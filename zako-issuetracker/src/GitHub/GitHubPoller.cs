using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using zako_issuetracker.Issue;

namespace zako_issuetracker.GitHub;

public static class GitHubPoller
{
    private static readonly HttpClient _http = CreateHttpClient();
    private static readonly Regex _repoPattern = new(@"^[a-zA-Z0-9\-_.]+/[a-zA-Z0-9\-_.]+$");
    private static string? _etag;
    private const int MaxBodyLength = 200;

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("zako-issuetracker", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        string? token = EnvLoader.GetGitHubToken();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public static async Task StartAsync()
    {
        string? repo = EnvLoader.GetGitHubRepo();
        if (string.IsNullOrEmpty(repo))
            return;

        if (!_repoPattern.IsMatch(repo))
        {
            Console.Error.WriteLine("[GitHub] Invalid GITHUB_REPO format. Expected 'owner/repo'.");
            return;
        }

        bool authenticated = !string.IsNullOrEmpty(EnvLoader.GetGitHubToken());
        int intervalMs = authenticated ? 20_000 : 120_000;

        Console.WriteLine($"[GitHub] Polling {repo} every {intervalMs / 1000}s (authenticated: {authenticated})");

        while (true)
        {
            try
            {
                await PollAsync(repo);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[GitHub] Poll error: {e}");
            }

            await Task.Delay(intervalMs);
        }
    }

    private static async Task PollAsync(string repo)
    {
        var issues = new List<IssueContent>();
        int page = 1;

        while (true)
        {
            string url = $"https://api.github.com/repos/{repo}/issues?state=open&per_page=100&sort=updated&direction=desc&page={page}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (_etag != null && page == 1)
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(_etag));

            using var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                Console.WriteLine("[GitHub] 304 Not Modified, skipping sync");
                return;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
                {
                    string? resetHeader = resetValues.FirstOrDefault();
                    if (resetHeader != null && long.TryParse(resetHeader, out long resetUnix))
                    {
                        var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
                        Console.Error.WriteLine($"[GitHub] Rate limited. Resets at {resetTime:HH:mm:ss} UTC");
                    }
                }
                return;
            }

            response.EnsureSuccessStatusCode();

            if (page == 1 && response.Headers.ETag != null)
                _etag = response.Headers.ETag.Tag;

            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
            {
                if (int.TryParse(remainingValues.FirstOrDefault(), out int rem) && rem < 10)
                    Console.WriteLine($"[GitHub] Rate limit remaining: {rem}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var items = await JsonSerializer.DeserializeAsync<JsonElement[]>(stream);
            if (items == null || items.Length == 0)
                break;

            foreach (var item in items)
            {
                bool isPr = item.TryGetProperty("pull_request", out var prObj);
                bool? merged = null;
                if (isPr && prObj.TryGetProperty("merged_at", out var mergedAt))
                    merged = mergedAt.ValueKind != JsonValueKind.Null;

                string state = item.GetProperty("state").GetString() ?? "open";

                string[]? labels = null;
                if (item.TryGetProperty("labels", out var labelsArr))
                {
                    labels = labelsArr.EnumerateArray()
                        .Select(l => l.GetProperty("name").GetString() ?? "")
                        .ToArray();
                }

                string title = item.GetProperty("title").GetString() ?? "";
                string body = item.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind == JsonValueKind.String
                    ? bodyEl.GetString() ?? ""
                    : "";
                if (body.Length > MaxBodyLength)
                    body = body[..MaxBodyLength] + "...";

                string author = "unknown";
                if (item.TryGetProperty("user", out var userObj)
                    && userObj.TryGetProperty("login", out var loginEl))
                {
                    author = loginEl.GetString() ?? "unknown";
                }

                int number = item.GetProperty("number").GetInt32();
                string htmlUrl = item.GetProperty("html_url").GetString() ?? "";

                issues.Add(new IssueContent
                {
                    Name = title,
                    Detail = body,
                    Tag = GitHubMapper.MapLabelsToTag(labels),
                    Status = GitHubMapper.MapState(state, isPr, merged),
                    UserId = author,
                    IsGitHub = true,
                    GitHubNumber = number,
                    IsPullRequest = isPr,
                    HtmlUrl = htmlUrl
                });
            }

            if (items.Length < 100)
                break;

            page++;
        }

        if (issues.Count == 0)
        {
            Console.WriteLine("[GitHub] No open issues returned, clearing github_issues table");
        }

        await IssueData.SyncGitHubIssuesAsync(issues);
        Console.WriteLine($"[GitHub] Synced {issues.Count} issues/PRs");
    }
}

using Discord;

namespace zako_issuetracker.commands;

public static class IssueListEmbed
{
    private static int PageSize = EnvLoader.GetPageSize();
    public static readonly Color GitHubColor = new(0x9B, 0x59, 0xB6);


    public static Embed[] BuildIssueListEmbed(Dictionary<int, Issue.IssueContent> dict, int page, IssueTag? tag = null, IssueStatus? status = null)
    {
        string sTag = tag?.ToString() ?? "All";
        string sStatus = status?.ToString() ?? "All";

        var appearedIssues = dict.OrderBy(kv => kv.Key).Skip((page -1) * PageSize).Take(PageSize);

        var embeds = new List<Embed>();

        foreach (var ctx in appearedIssues)
        {
            var color = ctx.Value.IsGitHub ? GitHubColor : Color.Blue;

            var eb = new EmbedBuilder()
                .WithTitle($"Issue List - Page {page}")
                .WithDescription($"Tag : {sTag} | Status : {sStatus}")
                .WithColor(color)
                .WithTimestamp(DateTimeOffset.Now);

            string idDisplay;
            if (ctx.Value.IsGitHub)
            {
                string typeLabel = ctx.Value.IsPullRequest ? "PR" : "Issue";
                idDisplay = $"[GH] #{ctx.Value.GitHubNumber} ({typeLabel})";
            }
            else
            {
                idDisplay = ctx.Key.ToString();
            }

            string userDisplay = ctx.Value.IsGitHub
                ? $"Author : @{ctx.Value.UserId}"
                : $"User : <@{ctx.Value.UserId}>";

            string fieldValue =
                $"Name : {ctx.Value.Name}\n" +
                $"Detail : {ctx.Value.Detail}\n" +
                $"Tag : {ctx.Value.Tag}\n" +
                $"Status : {ctx.Value.Status}\n" +
                userDisplay;

            if (ctx.Value.IsGitHub && !string.IsNullOrEmpty(ctx.Value.HtmlUrl))
                fieldValue += $"\nLink : {ctx.Value.HtmlUrl}";

            eb.AddField($"ID : {idDisplay}", fieldValue);
            eb.WithFooter($"Page {page} | Total Issues: {dict.Count}");

            embeds.Add(eb.Build());
        }

        return embeds.ToArray();
    }
}

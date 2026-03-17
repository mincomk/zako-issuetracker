using Microsoft.Data.Sqlite;

namespace zako_issuetracker.Issue;

public struct IssueContent
{
    public string Name;
    public string Detail;
    public IssueTag Tag;
    public IssueStatus Status;
    public string UserId;
    public bool IsGitHub;
    public int GitHubNumber;
    public bool IsPullRequest;
    public string HtmlUrl;
}

public class IssueJsonContent
{
    public int Id;
    public string Name;
    public string Detail;
    public IssueTag Tag;
    public IssueStatus Status;
    public string UserId;
}

public class IssueData
{
    public static async Task<int> StoreIssueAsync(string? name, string? detail, IssueTag? tag, string userId)
    {
        if (name == null || detail == null || tag == null)
            return -1;

        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO zako (name, detail, tag, status, discord) VALUES (@name, @detail, @tag, @status, @discord)";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@detail", detail);
            cmd.Parameters.AddWithValue("@tag", tag.ToString());
            cmd.Parameters.AddWithValue("@status", IssueStatus.Proposed);
            cmd.Parameters.AddWithValue("@discord", userId);

            await cmd.ExecuteNonQueryAsync();
            
            
            // Get stored issue id
            int id;
            await using var idCmd = con.CreateCommand();
            idCmd.CommandText = "SELECT id FROM zako WHERE name = @name AND discord=@discord AND tag=@tag ORDER BY id DESC LIMIT 1";
            idCmd.Parameters.AddWithValue("@name", name);
            idCmd.Parameters.AddWithValue("@discord", userId);
            idCmd.Parameters.AddWithValue("@tag", tag.ToString());

            await using var reader = await idCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                id = reader.GetInt32(0);
            }
            else
            {
                id = -1;
            }

            return id;
        }
        catch (Exception)
        {
            return -1;
        }
    }
    
    public static async Task<bool> UpdateIssueStatusAsync(int? issueId, IssueStatus? newStatus)
    {
        if(issueId==null || newStatus==null)
            return false;

        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE zako SET status = @status WHERE id = @id";
            cmd.Parameters.AddWithValue("@status", newStatus.ToString());
            cmd.Parameters.AddWithValue("@id", issueId);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<Dictionary<int, IssueContent>> ListOfIssueAsync(IssueTag? tag = null, IssueStatus? status = null)
    {
        string cTag = tag?.ToString() ?? "%";
        string cStatus = status?.ToString() ?? "%";
        var dict = new Dictionary<int, IssueContent>();

        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT id, name, detail, tag, status, discord, 0 as is_github, 0 as github_number, 0 as is_pr, '' as html_url FROM zako WHERE tag LIKE @tag AND status LIKE @status"
                + " UNION ALL SELECT id, name, detail, tag, status, author, 1 as is_github, github_number, is_pr, html_url FROM github_issues WHERE tag LIKE @tag2 AND status LIKE @status2"
                + " ORDER BY is_github ASC, id ASC";
            cmd.Parameters.AddWithValue("@tag", cTag);
            cmd.Parameters.AddWithValue("@status", cStatus);
            cmd.Parameters.AddWithValue("@tag2", cTag);
            cmd.Parameters.AddWithValue("@status2", cStatus);

            await using var reader = await cmd.ExecuteReaderAsync();
            int key = 0;
            while (await reader.ReadAsync())
            {
                dict.Add(key++, new IssueContent
                {
                    Name = reader.GetString(1),
                    Detail = reader.GetString(2),
                    Tag = Enum.Parse<IssueTag>(reader.GetString(3)),
                    Status = Enum.Parse<IssueStatus>(reader.GetString(4)),
                    UserId = reader.GetString(5),
                    IsGitHub = reader.GetInt32(6) == 1,
                    GitHubNumber = reader.GetInt32(7),
                    IsPullRequest = reader.GetInt32(8) == 1,
                    HtmlUrl = reader.GetString(9)
                });
            }
        }
        catch (Exception)
        {
            return dict;
        }

        return dict;
    }

    public static async Task<IssueContent?> GetIssueByIdAsync(int? issueId)
    {
        if (issueId == null)
            return null;

        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT name, detail, tag, status, discord  FROM zako WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", issueId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }
            
            IssueContent respond = new IssueContent
            {
                Name = reader.GetString(0),
                Detail = reader.GetString(1),
                Tag = Enum.Parse<IssueTag>(reader.GetString(2)),
                Status = Enum.Parse<IssueStatus>(reader.GetString(3)),
                UserId = reader.GetString(4)
            };

            return respond;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<bool> DeleteIssueAsync(int? issueId)
    {
        if (issueId == null)
            return false;

        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE zako SET status = @status, name = @name, detail = @detail WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", issueId);
            cmd.Parameters.AddWithValue("@status", IssueStatus.Deleted);
            cmd.Parameters.AddWithValue("@name", "Deleted Issue");
            cmd.Parameters.AddWithValue("@detail", "Deleted by admin.");
            
            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> UpdateIssueAsync(IssueJsonContent issueContent)
    {
        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE zako SET name = @name, detail = @detail, tag = @tag WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", issueContent.Id);
            cmd.Parameters.AddWithValue("@name", issueContent.Name);
            cmd.Parameters.AddWithValue("@detail", issueContent.Detail);
            cmd.Parameters.AddWithValue("@tag", issueContent.Tag.ToString());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }
    
    public static async Task<IssueContent?> GetGitHubIssueAsync(int githubNumber)
    {
        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT name, detail, tag, status, author, github_number, is_pr, html_url FROM github_issues WHERE github_number = @num";
            cmd.Parameters.AddWithValue("@num", githubNumber);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new IssueContent
            {
                Name = reader.GetString(0),
                Detail = reader.GetString(1),
                Tag = Enum.Parse<IssueTag>(reader.GetString(2)),
                Status = Enum.Parse<IssueStatus>(reader.GetString(3)),
                UserId = reader.GetString(4),
                IsGitHub = true,
                GitHubNumber = reader.GetInt32(5),
                IsPullRequest = reader.GetInt32(6) == 1,
                HtmlUrl = reader.GetString(7)
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task SyncGitHubIssuesAsync(List<IssueContent> issues)
    {
        try
        {
            await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
            await con.OpenAsync();
            await using var transaction = await con.BeginTransactionAsync();

            await using var delCmd = con.CreateCommand();
            delCmd.CommandText = "DELETE FROM github_issues";
            await delCmd.ExecuteNonQueryAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = @"INSERT INTO github_issues (github_number, is_pr, tag, status, name, detail, author, html_url)
                VALUES (@num, @is_pr, @tag, @status, @name, @detail, @author, @url)";
            var pNum = cmd.Parameters.Add("@num", SqliteType.Integer);
            var pIsPr = cmd.Parameters.Add("@is_pr", SqliteType.Integer);
            var pTag = cmd.Parameters.Add("@tag", SqliteType.Text);
            var pStatus = cmd.Parameters.Add("@status", SqliteType.Text);
            var pName = cmd.Parameters.Add("@name", SqliteType.Text);
            var pDetail = cmd.Parameters.Add("@detail", SqliteType.Text);
            var pAuthor = cmd.Parameters.Add("@author", SqliteType.Text);
            var pUrl = cmd.Parameters.Add("@url", SqliteType.Text);

            foreach (var issue in issues)
            {
                pNum.Value = issue.GitHubNumber;
                pIsPr.Value = issue.IsPullRequest ? 1 : 0;
                pTag.Value = issue.Tag.ToString();
                pStatus.Value = issue.Status.ToString();
                pName.Value = issue.Name;
                pDetail.Value = issue.Detail;
                pAuthor.Value = issue.UserId;
                pUrl.Value = issue.HtmlUrl;
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"GitHub sync failed: {e.Message}");
        }
    }

    #region ["Obsolete Sync Wrappers"]
    [Obsolete("Use StoreIssueAsync instead")]
    public static int StoreIssue(string? name, string? detail, IssueTag? tag, string userId)
        => StoreIssueAsync(name, detail, tag, userId).GetAwaiter().GetResult();
    
    [Obsolete("Use UpdateIssueStatusAsync instead")]
    public static bool UpdateIssueStatus(int? issueId, IssueStatus? newStatus)
        => UpdateIssueStatusAsync(issueId, newStatus).GetAwaiter().GetResult();
    
    [Obsolete("Use ListOfIssueAsync instead")]
    public static Dictionary<int, IssueContent> ListOfIssue(IssueTag? tag)
        => ListOfIssueAsync(tag, null).GetAwaiter().GetResult();
    
    [Obsolete("Use GetIssueByIdAsync instead")]
    public static IssueContent? GetIssueById(int? issueId)
        => GetIssueByIdAsync(issueId).GetAwaiter().GetResult();
    
    #endregion
}

internal static class DataBaseHelper
{
    public static string dbPath
    {
        get {
            return EnvLoader.GetSqlitePath() ?? throw new ArgumentNullException();
        } 
    } 
}

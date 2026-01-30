using Microsoft.Data.Sqlite;

namespace zako_issuetracker.Issue;

public struct IssueContent
{
    public string Name;
    public string Detail;
    public IssueTag Tag;
    public IssueStatus Status;
    public string UserId;
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
    public static async Task<bool> StoreIssueAsync(string? name, string? detail, IssueTag? tag, string userId)
    {
        if (name == null || detail == null || tag == null)
            return false;

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
            return true;
        }
        catch (Exception)
        {
            return false;
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
            cmd.CommandText = "UPDATE zako SET status = @status WHERE ROWID = @id";
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
            cmd.CommandText = "SELECT ROWID, name, detail, tag, status, discord FROM zako WHERE tag LIKE @tag AND status LIKE @status";
            cmd.Parameters.AddWithValue("@tag", cTag);
            cmd.Parameters.AddWithValue("@status", cStatus);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dict.Add(reader.GetInt32(0), new IssueContent
                {
                    Name = reader.GetString(1),
                    Detail = reader.GetString(2),
                    Tag = Enum.Parse<IssueTag>(reader.GetString(3)),
                    Status = Enum.Parse<IssueStatus>(reader.GetString(4)),
                    UserId = reader.GetString(5)
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
            cmd.CommandText = "SELECT name, detail, tag, status, discord  FROM zako WHERE ROWID = @id";
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
    
    #region ["Obsolete Sync Wrappers"]
    [Obsolete("Use StoreIssueAsync instead")]
    public static bool StoreIssue(string? name, string? detail, IssueTag? tag, string userId)
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

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

public class IssueData
{
    static Dictionary<int, IssueContent> _dict = new Dictionary<int, IssueContent>();
    
    public static bool StoreIssue(string? name, string? detail, IssueTag? tag, string userId)
    {
        if (name == null || detail == null || tag == null)
            return false;
        
        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO zako (name, detail, tag, status, discord) VALUES (@name, @detail, @tag, @status, @discord)";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@detail", detail);
        cmd.Parameters.AddWithValue("@tag", tag.ToString());
        cmd.Parameters.AddWithValue("@status", IssueStatus.Proposed);
        cmd.Parameters.AddWithValue("@discord", userId);
        
        cmd.ExecuteNonQuery();
        con.Close();

        return true;
    }
    
    public static bool UpdateIssueStatus(int? issueId, IssueStatus? newStatus)
    {
        if(issueId==null || newStatus==null)
            return false;
        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE zako SET status = @status WHERE ROWID = @id";
        cmd.Parameters.AddWithValue("@status", newStatus.ToString());
        cmd.Parameters.AddWithValue("@id", issueId);
        
        cmd.ExecuteNonQuery();
        con.Close();

        return true;
    }

    public static Dictionary<int, IssueContent> ListOfIssue(IssueTag? tag)
    {
        string cTag = tag?.ToString() ?? "%";
        
        var dict = new Dictionary<int, IssueContent>(); // Create new dictionary each time
        
        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT ROWID, name, detail, tag, status, discord FROM zako WHERE tag LIKE @tag";
        cmd.Parameters.AddWithValue("@tag", cTag);
        
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            dict.Add(reader.GetInt32(0), new IssueContent
            {
                Name = reader.GetString(1),
                Detail =  reader.GetString(2),
                Tag = Enum.Parse<IssueTag>(reader.GetString(3)),
                Status = Enum.Parse<IssueStatus>(reader.GetString(4)),
                UserId = reader.GetString(5)
            });
        }
        return _dict;
        con.Close();
        return dict;
    }

    public static IssueContent? GetIssueById(int? issueId)
    {
        if (issueId == null)
            return null;
        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT name, detail, tag, status, discord  FROM zako WHERE ROWID = @id";
        cmd.Parameters.AddWithValue("@id", issueId);
        var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            con.Close();
            return null;
        }
        IssueContent respond = new IssueContent();
        respond.Name = reader.GetString(0);
        respond.Detail = reader.GetString(1);
        respond.Tag = Enum.Parse<IssueTag>(reader.GetString(2));
        respond.Status = Enum.Parse<IssueStatus>(reader.GetString(3));
        respond.UserId = reader.GetString(4);
        
        con.Close();
        return respond;
    }
}

internal class DataBaseHelper
{
    public const string dbPath = "db.sqlite";
    
    
}

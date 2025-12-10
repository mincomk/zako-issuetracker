using Microsoft.Data.Sqlite;

namespace zako_issuetracker.Issue;



public class IssueData
{
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
        cmd.CommandText = "UPDATE zako SET status = @status WHERE id = @id";
        cmd.Parameters.AddWithValue("@status", newStatus.ToString());
        cmd.Parameters.AddWithValue("@id", issueId);
        
        var rowsAffected = cmd.ExecuteNonQuery();
        con.Close();

        return true;
    }
}

internal class DataBaseHelper
{
    public const string dbPath = "db.sqlite";
    
    static int IssueCount()
    {
        var con = new SqliteConnection("Data Source=" + dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM zako";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        con.Close();
        return count;
    }
}

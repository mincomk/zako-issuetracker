using System.IO;
using Microsoft.Data.Sqlite;
using zako_issuetracker.Issue;

namespace zako_issuetracker;

public static class Startup
{
    private static bool DbChk()
        => File.Exists(EnvLoader.GetSqlitePath());

    private static bool DbPathChk()
        => Directory.Exists(Path.GetDirectoryName(EnvLoader.GetSqlitePath()));

    public static void StartupCheck()
    {
        try
        {
            if (!DbPathChk())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EnvLoader.GetSqlitePath()));
            }

            if (!DbChk())
            {
                File.Create(EnvLoader.GetSqlitePath()).Close();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
        
        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS zako(id INTEGER PRIMARY KEY AUTOINCREMENT, tag INTEGER NOT NULL, status INTEGER NOT NULL, name TEXT NOT NULL, detail text NOT NULL, discord text NOT NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS zakonim(id TEXT PRIMARY KEY NOT NULL, description TEXT NOT NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS github_issues(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            github_number INTEGER NOT NULL,
            is_pr INTEGER NOT NULL DEFAULT 0,
            tag TEXT NOT NULL,
            status TEXT NOT NULL,
            name TEXT NOT NULL,
            detail TEXT NOT NULL,
            author TEXT NOT NULL,
            html_url TEXT NOT NULL,
            UNIQUE(github_number))";
        cmd.ExecuteNonQuery();
        con.Close();
    }
}
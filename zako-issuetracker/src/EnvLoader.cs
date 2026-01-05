using System;

namespace zako_issuetracker;

public static class EnvLoader
{
    private static Dictionary<string, string?> GetEnv()
    {
        Dictionary<string, string?> EnvDict = new Dictionary<string, string?>();
        EnvDict.Add("DISCORD_TOKEN", Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? null);
        EnvDict.Add("SQLITE_FILE", Environment.GetEnvironmentVariable("SQLITE_FILE") ?? null);
        EnvDict.Add("IMG_LINK", Environment.GetEnvironmentVariable("IMG_LINK") ?? null);

        return EnvDict;
    }

    public static string? GetToken()
    {
        return GetEnv()["DISCORD_TOKEN"] ?? null;
    }

    public static string? GetSqlitePath()
    {
        return GetEnv()["SQLITE_FILE"] ?? null;
    }

    public static string? GetImgLink()
    {
        return GetEnv()["IMG_LINK"] ??
               "https://cdn.discordapp.com/avatars/1365312197613453322/43da9fd0741f4657f2344deb2062c0ca.png";
    }

    public static string[] GetAdminIds()
    {
        string? ids = Environment.GetEnvironmentVariable("ADMIN_IDS");
        if(ids == null)
            return Array.Empty<string>();
        
        return ids.Split(",");
    }

    public static int GetPageSize()
    {
        string? v = Environment.GetEnvironmentVariable("EMBED_PAGE_SIZE");
        if (v == null)
            return 5;
        return int.Parse(v);
    }
}

public static class AdminTool
{
    public static bool IsAdmin(string userId)
    {
        var adminIds = EnvLoader.GetAdminIds();
        return adminIds.Contains(userId);
    }
}
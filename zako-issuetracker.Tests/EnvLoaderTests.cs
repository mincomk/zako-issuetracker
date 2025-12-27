namespace zako_issuetracker.Tests;

public class EnvLoaderTests
{
    [Fact]
    public void GetToken_WhenSet_ReturnsToken()
    {
        var expected = "test_token_123";
        Environment.SetEnvironmentVariable("DISCORD_TOKEN", expected);

        var result = EnvLoader.GetToken();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetToken_WhenNotSet_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("DISCORD_TOKEN", null);

        var result = EnvLoader.GetToken();

        Assert.Null(result);
    }

    [Fact]
    public void GetSqlitePath_WhenSet_ReturnsPath()
    {
        var expected = "./test.db";
        Environment.SetEnvironmentVariable("SQLITE_FILE", expected);

        var result = EnvLoader.GetSqlitePath();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSqlitePath_WhenNotSet_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("SQLITE_FILE", null);

        var result = EnvLoader.GetSqlitePath();

        Assert.Null(result);
    }

    [Fact]
    public void GetAdminIds_WhenSet_ReturnsArray()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", "123,456,789");

        var result = EnvLoader.GetAdminIds();

        Assert.Equal(3, result.Length);
        Assert.Contains("123", result);
        Assert.Contains("456", result);
        Assert.Contains("789", result);
    }

    [Fact]
    public void GetAdminIds_WhenNotSet_ReturnsEmptyArray()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", null);

        var result = EnvLoader.GetAdminIds();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAdminIds_SingleId_ReturnsArrayWithOneElement()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", "123");

        var result = EnvLoader.GetAdminIds();

        Assert.Single(result);
        Assert.Equal("123", result[0]);
    }
}

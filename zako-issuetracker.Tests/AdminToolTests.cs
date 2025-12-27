namespace zako_issuetracker.Tests;

public class AdminToolTests
{
    [Fact]
    public void IsAdmin_UserInAdminList_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", "123,456,789");

        var result = AdminTool.IsAdmin("456");

        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_UserNotInAdminList_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", "123,456,789");

        var result = AdminTool.IsAdmin("999");

        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_EmptyAdminList_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", "");

        var result = AdminTool.IsAdmin("123");

        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_NullAdminIds_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable("ADMIN_IDS", null);

        var result = AdminTool.IsAdmin("123");

        Assert.False(result);
    }
}

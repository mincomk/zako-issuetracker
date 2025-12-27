using Microsoft.Data.Sqlite;
using zako_issuetracker.Issue;

namespace zako_issuetracker.Tests;

[Collection("Sequential")]
public class IssueSystemTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _originalDbPath;

    public IssueSystemTests()
    {
        _testDbPath = $"test_issues_{Guid.NewGuid()}.db";
        _originalDbPath = Environment.GetEnvironmentVariable("SQLITE_FILE") ?? "";
        Environment.SetEnvironmentVariable("SQLITE_FILE", _testDbPath);
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var con = new SqliteConnection($"Data Source={_testDbPath}");
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS zako(tag int, status int, name text, detail text, discord text)";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("SQLITE_FILE", _originalDbPath);
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    [Fact]
    public void StoreIssue_ValidInput_ReturnsTrue()
    {
        var result = IssueData.StoreIssue("Test Issue", "Test Detail", IssueTag.Bug, "user123");

        Assert.True(result);
    }

    [Fact]
    public void StoreIssue_NullName_ReturnsFalse()
    {
        var result = IssueData.StoreIssue(null, "Test Detail", IssueTag.Bug, "user123");

        Assert.False(result);
    }

    [Fact]
    public void StoreIssue_NullDetail_ReturnsFalse()
    {
        var result = IssueData.StoreIssue("Test Issue", null, IssueTag.Bug, "user123");

        Assert.False(result);
    }

    [Fact]
    public void StoreIssue_NullTag_ReturnsFalse()
    {
        var result = IssueData.StoreIssue("Test Issue", "Test Detail", null, "user123");

        Assert.False(result);
    }

    [Fact]
    public void StoreIssue_ValidInput_StoresInDatabase()
    {
        IssueData.StoreIssue("Test Issue", "Test Detail", IssueTag.Feature, "user456");

        var issues = IssueData.ListOfIssue(null);
        
        Assert.NotEmpty(issues);
        Assert.Contains(issues.Values, i => i.Name == "Test Issue" && i.Detail == "Test Detail");
    }

    [Fact]
    public void UpdateIssueStatus_ValidInput_ReturnsTrue()
    {
        IssueData.StoreIssue("Test Issue", "Test Detail", IssueTag.Bug, "user123");
        var issues = IssueData.ListOfIssue(null);
        var issueId = issues.Keys.First();

        var result = IssueData.UpdateIssueStatus(issueId, IssueStatus.Approved);

        Assert.True(result);
    }

    [Fact]
    public void UpdateIssueStatus_NullId_ReturnsFalse()
    {
        var result = IssueData.UpdateIssueStatus(null, IssueStatus.Approved);

        Assert.False(result);
    }

    [Fact]
    public void UpdateIssueStatus_NullStatus_ReturnsFalse()
    {
        var result = IssueData.UpdateIssueStatus(1, null);

        Assert.False(result);
    }

    [Fact]
    public void UpdateIssueStatus_ValidInput_UpdatesDatabase()
    {
        IssueData.StoreIssue("Test Issue", "Test Detail", IssueTag.Bug, "user123");
        var issues = IssueData.ListOfIssue(null);
        var issueId = issues.Keys.First();

        IssueData.UpdateIssueStatus(issueId, IssueStatus.Approved);

        var updatedIssue = IssueData.GetIssueById(issueId);
        Assert.NotNull(updatedIssue);
        Assert.Equal(IssueStatus.Approved, updatedIssue.Value.Status);
    }

    [Fact]
    public void ListOfIssue_NoFilter_ReturnsAllIssues()
    {
        IssueData.StoreIssue("Bug Issue", "Bug Detail", IssueTag.Bug, "user1");
        IssueData.StoreIssue("Feature Issue", "Feature Detail", IssueTag.Feature, "user2");
        IssueData.StoreIssue("Enhancement Issue", "Enhancement Detail", IssueTag.Enhancement, "user3");

        var issues = IssueData.ListOfIssue(null);

        Assert.Equal(3, issues.Count);
    }

    [Fact]
    public void ListOfIssue_WithTagFilter_ReturnsFilteredIssues()
    {
        IssueData.StoreIssue("Bug Issue 1", "Bug Detail 1", IssueTag.Bug, "user1");
        IssueData.StoreIssue("Bug Issue 2", "Bug Detail 2", IssueTag.Bug, "user2");
        IssueData.StoreIssue("Feature Issue", "Feature Detail", IssueTag.Feature, "user3");

        var issues = IssueData.ListOfIssue(IssueTag.Bug);

        Assert.Equal(2, issues.Count);
        Assert.All(issues.Values, i => Assert.Equal(IssueTag.Bug, i.Tag));
    }

    [Fact]
    public void GetIssueById_ExistingId_ReturnsIssue()
    {
        IssueData.StoreIssue("Test Issue", "Test Detail", IssueTag.Bug, "user123");
        var issues = IssueData.ListOfIssue(null);
        var issueId = issues.Keys.First();

        var issue = IssueData.GetIssueById(issueId);

        Assert.NotNull(issue);
        Assert.Equal("Test Issue", issue.Value.Name);
        Assert.Equal("Test Detail", issue.Value.Detail);
        Assert.Equal(IssueTag.Bug, issue.Value.Tag);
        Assert.Equal(IssueStatus.Proposed, issue.Value.Status);
        Assert.Equal("user123", issue.Value.UserId);
    }

    [Fact]
    public void GetIssueById_NonExistingId_ReturnsNull()
    {
        var issue = IssueData.GetIssueById(99999);

        Assert.Null(issue);
    }

    [Fact]
    public void GetIssueById_NullId_ReturnsNull()
    {
        var issue = IssueData.GetIssueById(null);

        Assert.Null(issue);
    }

    [Fact]
    public void MultipleIssues_DifferentStatuses_WorkCorrectly()
    {
        IssueData.StoreIssue("Issue 1", "Detail 1", IssueTag.Bug, "user1");
        IssueData.StoreIssue("Issue 2", "Detail 2", IssueTag.Feature, "user2");
        
        var issues = IssueData.ListOfIssue(null);
        var id1 = issues.Keys.First();
        var id2 = issues.Keys.Last();
        
        IssueData.UpdateIssueStatus(id1, IssueStatus.Approved);
        IssueData.UpdateIssueStatus(id2, IssueStatus.Rejected);

        var issue1 = IssueData.GetIssueById(id1);
        var issue2 = IssueData.GetIssueById(id2);

        Assert.Equal(IssueStatus.Approved, issue1!.Value.Status);
        Assert.Equal(IssueStatus.Rejected, issue2!.Value.Status);
    }
}

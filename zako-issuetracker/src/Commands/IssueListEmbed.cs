using Discord;

namespace zako_issuetracker.commands;

public static class IssueListEmbed
{
    private const int PageSize = 10;
    
    
    public static EmbedBuilder BuildIssueListEmbed(Dictionary<int, Issue.IssueContent> dict, int page, IssueTag? tag, IssueStatus? status = null)
    {
        string sTag = tag?.ToString() ?? "All";
        string sStatus = status?.ToString() ?? "All";
        
        var eb = new EmbedBuilder();
        eb.WithTitle($"Issue List :: Page {page}");
        eb.WithDescription($"tag : {sTag} | status : {sStatus}");
        eb.WithColor(Color.Blue);
        eb.WithFooter($"Page {page} | Total Issues: {dict.Count}");
        eb.WithTimestamp(DateTimeOffset.Now);

        var AppearedIssues = dict.Skip((page -1) * PageSize).Take(PageSize);
        foreach (var ctx in AppearedIssues)
        {
            eb.AddField($"ID : {ctx.Key.ToString()}",
                $"Name : {ctx.Value.Name}\n" +
                $"Detail : {ctx.Value.Detail}\n"+
                $"Tag : {ctx.Value.Tag.ToString()}\n"+
                $"Status : {ctx.Value.Status.ToString()}\n"+
                $"User : <@{ctx.Value.UserId}>");
        }
        return eb;
    }
}
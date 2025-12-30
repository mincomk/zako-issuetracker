using Discord;

namespace zako_issuetracker.commands;

public static class IssueListEmbed
{
    private static int PageSize = EnvLoader.GetPageSize();
    
    
    public static Embed[] BuildIssueListEmbed(Dictionary<int, Issue.IssueContent> dict, int page, IssueTag? tag = null, IssueStatus? status = null)
    {
        string sTag = tag?.ToString() ?? "All";
        string sStatus = status?.ToString() ?? "All";
        
        
        
        
        var appearedIssues = dict.OrderBy(kv => kv.Key).Skip((page -1) * PageSize).Take(PageSize);

        var embeds = new List<Embed>();
        
        foreach (var ctx in appearedIssues)
        {
            var id = ctx.Key;
            var content = ctx.Value;
            
            
            var eb = new EmbedBuilder()
                .WithTitle($"Issue List - Page {page}")
                .WithDescription($"Tag : {sTag} | Status : {sStatus}")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now);
            
            
            eb.AddField($"ID : {ctx.Key.ToString()}",
                $"Name : {ctx.Value.Name}\n" +
                $"Detail : {ctx.Value.Detail}\n"+
                $"Tag : {ctx.Value.Tag.ToString()}\n"+
                $"Status : {ctx.Value.Status.ToString()}\n"+
                $"User : <@{ctx.Value.UserId}>");
            eb.WithFooter($"Page {page} | Total Issues: {dict.Count}");
            
            embeds.Add(eb.Build());
        }
        //eb[PageSize+1].WithFooter($"Page {page} | Total Issues: {dict.Count}");
        
        return embeds.ToArray();
    }
}
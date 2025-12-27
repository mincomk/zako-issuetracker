using Discord;

namespace zako_issuetracker.components;

public static class Pages
{
    public static ComponentBuilder Button()
    {
        var buttons = new ComponentBuilder()
            .WithButton(label: "이전", customId: "issue-previous", style: ButtonStyle.Primary, new Emoji("⬅️"))
            .WithButton(label: "다음", customId: "issue-next", style: ButtonStyle.Secondary, new Emoji("➡️"));

        return buttons;
    }
    
    
}
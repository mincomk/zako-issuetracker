using Discord;

namespace zako_issuetracker;

partial class Program
{
    private static async Task ReadyAsync()
    {
        Console.WriteLine($"{_client.CurrentUser} is connected!");

        _client.SetActivityAsync(new Game("이슈 수집 중..."));
        
        #region [SlashCommands]

        var issueTagChoices = new SlashCommandOptionBuilder()
            .WithName("tags")
            .WithDescription("이슈 태그")
            .WithType(ApplicationCommandOptionType.String);
        foreach (var tag in Enum.GetNames(typeof(IssueTag)))
        {
            issueTagChoices.AddChoice(tag, tag.ToLowerInvariant());
        }

        var issueStatusChoices = new SlashCommandOptionBuilder()
            .WithName("change-to")
            .WithDescription("이슈 상태 선택")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);
        
        var issueStatusFilterChoices = new SlashCommandOptionBuilder()
            .WithName("status")
            .WithDescription("이슈 상태 필터")
            .WithRequired(false)
            .WithType(ApplicationCommandOptionType.String);

        foreach (var status in Enum.GetNames(typeof(IssueStatus)))
        {
            issueStatusChoices.AddChoice(status, status.ToLowerInvariant());
            issueStatusFilterChoices.AddChoice(status, status.ToLowerInvariant());
        }

        var newIssue = new SlashCommandBuilder()
            .WithName("issue")
            .WithDescription("Root of issue commands")
            //.AddOptions(new SlashCommandOptionBuilder())
            .WithNameLocalizations(new Dictionary<string, string>
            {
                { "ko", "이슈" }
            })
            .WithDescriptionLocalizations(new Dictionary<string, string>
            {
                { "ko", "이슈 명령어 최상위 식별자(?)" }
            })
            //get
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("get")
                .WithDescription("ID로 이슈 조회")
                .AddOption(new SlashCommandOptionBuilder() // get (int issueId)
                    .WithName("id")
                    .WithDescription("이슈 ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithType(ApplicationCommandOptionType.SubCommand))
            //new
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("new")
                .WithDescription("새 이슈 생성")
                .WithType(ApplicationCommandOptionType.SubCommand))
            // status
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("set-status")
                .WithDescription("상태 설정")
                .AddOption(new SlashCommandOptionBuilder() // status(int issueId)
                    .WithName("id")
                    .WithDescription("이슈 ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .AddOption(issueStatusChoices)
                .WithType(ApplicationCommandOptionType.SubCommand))
            // list
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("이슈 목록")
                .AddOption(issueTagChoices) // list (<enum> Tag tag).WithType(ApplicationCommandOptionType.Boolean))
                .AddOption(issueStatusFilterChoices)
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("export")
                .WithDescription("이슈 내보내기")
                .AddOption(issueTagChoices.WithRequired(false))
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("delete")
                .WithDescription("이슈 삭제")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("id")
                    .WithDescription("이슈 ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("edit")
                .WithDescription("이슈 수정")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("id").WithDescription("이슈ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithType(ApplicationCommandOptionType.SubCommand))
            .WithContextTypes(new[] {InteractionContextType.PrivateChannel,InteractionContextType.BotDm,InteractionContextType.Guild}).Build();

        var ping = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("Pong!")
            .WithContextTypes(new[] {InteractionContextType.PrivateChannel,InteractionContextType.BotDm,InteractionContextType.Guild}).Build();

        var zakonim = new SlashCommandBuilder()
            .WithName("zakonim")
            .WithDescription("Hello zako!")
            .WithNameLocalizations(new Dictionary<string, string>{{"ko", "자코님"}})
            .WithDescriptionLocalizations(new Dictionary<string, string>{{"ko", "허접님"}})
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("who")
                .WithDescription("who is zako")
                .WithNameLocalizations(new Dictionary<string, string> {{"ko", "누가"}})
                .WithDescriptionLocalizations(new Dictionary<string, string>{{"ko", "누가 허접인가"}})
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.User))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("description")
                .WithDescription("zakonim!")
                .WithNameLocalizations(new Dictionary<string, string>{{"ko", "설명"}})
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String))
            .WithContextTypes(new[] {InteractionContextType.PrivateChannel,InteractionContextType.BotDm,InteractionContextType.Guild}).Build();
        
        await _client.CreateGlobalApplicationCommandAsync(newIssue);
        await _client.CreateGlobalApplicationCommandAsync(ping);
        await _client.CreateGlobalApplicationCommandAsync(zakonim);
        
        #endregion
    }
}
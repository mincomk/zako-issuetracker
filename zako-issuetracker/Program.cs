using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace zako_issuetracker;

public enum IssueTag
{
    Bug, Feature, Enhancement
}

public enum IssueStatus
{
    Proposed, Approved, Rejected, InProgress, Completed
}
class Program
{
    private static DiscordSocketClient _client;

    public static async Task Main(string[] args)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
            
        };
        
        _client = new DiscordSocketClient(config);
        
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreatedAsync;
     
        await _client.LoginAsync(TokenType.Bot, "MTQ0NzE2Nzc2MDU3MTMwNjAzNQ.GfwkL8.t0FgeFgQdAZEqaHyyU7tFaEHyYlOTHdiaBuSAU"); // token here
        
        await _client.StartAsync();
        
        await Task.Delay(Timeout.Infinite);
    }
    
    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
    
    private static async Task ReadyAsync()
    {
        Console.WriteLine($"{_client.CurrentUser} is connected!");

        var guild = _client.GetGuild(1429043368876314696);

        _client.SetActivityAsync(new Game("Gathering Issues..."));

        var issueTagChoices = new SlashCommandOptionBuilder()
            .WithName("tags")
            .WithDescription("Tags of issue")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);
        foreach (var tag in Enum.GetNames(typeof(IssueTag)))
        {
            issueTagChoices.AddChoice(tag, tag.ToLowerInvariant());
        }

        var issueStatusChoies = new SlashCommandOptionBuilder()
            .WithName("change-to")
            .WithDescription("Change issue status to")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);

        foreach (var status in Enum.GetNames(typeof(IssueStatus)))
        {
            issueStatusChoies.AddChoice(status, status.ToLowerInvariant());
        }
        
        var newIssue = new SlashCommandBuilder()
            .WithName("issue")
            .WithDescription("Root of issue commands")
            //.AddOptions(new SlashCommandOptionBuilder())
            .WithNameLocalizations(new Dictionary<string, string>
            {
                {"ko","이슈"}
            })
            .WithDescriptionLocalizations(new Dictionary<string, string>
            {
                {"ko","이슈 명령어 최상위 식별자(?)"}
            })
            //new
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("new")
                .WithDescription("Create new issue")
                .WithType(ApplicationCommandOptionType.SubCommand))
            // status
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("set status")
                .AddOption(new SlashCommandOptionBuilder() // status(int issueId)
                    .WithName("id")
                    .WithDescription("Issue ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .AddOption(issueStatusChoies)
                .WithType(ApplicationCommandOptionType.SubCommand))
            // list
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List of \"issues\"")
                .AddOption(issueTagChoices) // list (<enum> Tag tag)
                .AddOption(new SlashCommandOptionBuilder() // list (bool opened = true)
                    .WithName("opened")
                    .WithDescription("True = show opened issue only, false = show all")
                    //.WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Boolean))
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("export")
                .WithDescription("Export the issue")
                .AddOption(issueTagChoices)
                .WithType(ApplicationCommandOptionType.SubCommand))
            .Build();
        
        await guild.CreateApplicationCommandAsync(newIssue);
        //return Task.CompletedTask;
    }
    
    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client.CurrentUser.Id)
            return;


        if (message.Content == "!issue")
        {
            var cb = new ComponentBuilder()
                .WithButton("Click me!", "unique-id", ButtonStyle.Primary);

            // Send a message with content 'pong', including a button.
            // This button needs to be build by calling .Build() before being passed into the call.
            await message.Channel.SendMessageAsync("pong!", components: cb.Build());
        }
    }
    private static async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        // safety-casting is the best way to prevent something being cast from being null.
        // If this check does not pass, it could not be cast to said type.
        if (interaction is SocketMessageComponent component)
        {
            // Check for the ID created in the button mentioned above.
            if (component.Data.CustomId == "unique-id")
                await interaction.RespondAsync("Thank you for clicking my button!");

            else
                Console.WriteLine("An ID has been received that has no handler!");
        }

        if (interaction is SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                default:
                    await modal.RespondAsync("undfined command");
                    break;
            }
        }

        if (interaction is SocketSlashCommand slashCommand)
        {
            switch (slashCommand.Data.Name)
            {
                case "issue":
                    await slashCommand.RespondAsync("test", ephemeral: true);
                    break;
                default:
                    await slashCommand.RespondAsync("undfined command");
                    break;
            }
        }
    }
}
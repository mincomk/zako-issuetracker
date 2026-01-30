using Discord;
using Discord.WebSocket;


namespace zako_issuetracker;

public enum IssueTag
{
    Bug,
    Feature,
    Enhancement
}

public enum IssueStatus
{
    Proposed,
    Approved,
    Rejected,
    Deleted,
    InProgress,

    Completed
    // 0, 1, 2, 3, 4, 5
}

partial class Program
{
    private static DiscordSocketClient _client;

    public static async Task Main(string[] args)
    {
        string botToken = EnvLoader.GetToken() ?? throw new ArgumentNullException();
        string[] adminIds = EnvLoader.GetAdminIds();
        if (adminIds == Array.Empty<string>())
            throw new ArgumentNullException();

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        _client = new DiscordSocketClient(config);

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreatedAsync;

        Startup.StartupCheck();

        await _client.LoginAsync(TokenType.Bot, botToken); //or, read from console

        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }


    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client.CurrentUser.Id)
            return;
    }
}
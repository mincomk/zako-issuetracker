using System.Drawing;
using Discord;
//using Discord.Interactions.Builders;
using Discord.WebSocket;
using Color = Discord.Color;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using zako_issuetracker.Issue;

namespace zako_issuetracker;

public enum IssueTag
{
    Bug, Feature, Enhancement
}

public enum IssueStatus
{
    Proposed, Approved, Rejected
    // 0, 1, 2,
}
class Program
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

        var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
        con.Open();
        var cmd = con.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS zako(id INTEGER PRIMARY KEY AUTOINCREMENT, tag INTEGER NOT NULL, status INTEGER NOT NULL, name TEXT NOT NULL, detail text NOT NULL, discord text NOT NULL)";
        cmd.ExecuteNonQuery();
        con.Close();
        
        await _client.LoginAsync(TokenType.Bot, botToken); //or, read from console
        
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

        _client.SetActivityAsync(new Game("이슈 수집 중..."));

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
            .WithDescription("이슈 상태 변경")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);

        foreach (var status in Enum.GetNames(typeof(IssueStatus)))
        {
            issueStatusChoices.AddChoice(status, status.ToLowerInvariant());
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
                .WithName("status")
                .WithDescription("상태 설정")
                .AddOption(new SlashCommandOptionBuilder() // status(int issueId)
                    .WithName("id")
                    .WithDescription("이슈 ID")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .AddOption(issueStatusChoices)
                .WithType(ApplicationCommandOptionType.SubCommand)
            )
            // list
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("이슈 목록")
                .AddOption(issueTagChoices) // list (<enum> Tag tag).WithType(ApplicationCommandOptionType.Boolean))
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("export")
                .WithDescription("이슈 내보내기")
                .AddOption(issueTagChoices)
                .WithType(ApplicationCommandOptionType.SubCommand))
            .Build();

        await _client.CreateGlobalApplicationCommandAsync(newIssue);
    }
    
    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client.CurrentUser.Id)
            return;
    }
    private static async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        // safety-casting is the best way to prevent something being cast from being null.
        // If this check does not pass, it could not be cast to said type.
        if (interaction is SocketMessageComponent component)
        {
            // Check for the ID created in the button mentioned above.
            if (component.Data.CustomId == "unique-id")
                await interaction.RespondAsync("버튼을 클릭해주셔서 감사합니다!");
            else if (component.Data.CustomId == "issue-previous")
            {
                int currentPage = int.Parse(component.Message.Embeds.First().Title.Split().Last());
                IssueTag? tag;
                if (component.Message.Embeds.First().Description.Split().Last() != "All")
                    tag = Enum.Parse<IssueTag>(component.Message.Embeds.First().Description.Split().Last(), true);
                else
                    tag = null;
                
                if (currentPage <= 1)
                {
                    await component.RespondAsync("첫 페이지입니다!", ephemeral: false);
                    return;
                }
                
                Dictionary<int, Issue.IssueContent> dict = await Issue.IssueData.ListOfIssueAsync(tag);
                await component.UpdateAsync(msg =>
                    { 
                        msg.Embed = commands.IssueListEmbed.BuildIssueListEmbed(dict,--currentPage, tag).Build();
                    });

            }else if (component.Data.CustomId == "issue-next")
            {
                int currentPage = int.Parse(component.Message.Embeds.First().Title.Split().Last());

                IssueTag? tag;
                if (component.Message.Embeds.First().Description.Split().Last() != "All")
                    tag = Enum.Parse<IssueTag>(component.Message.Embeds.First().Description.Split().Last(), true);
                else
                    tag = null;
                
                Dictionary<int, Issue.IssueContent> dict = await Issue.IssueData.ListOfIssueAsync(tag);
                int maxPage = (int)Math.Ceiling((double)dict.Count / 10);
                if (currentPage >= maxPage)
                {
                    await component.RespondAsync("마지막 페이지입니다!", ephemeral: false);
                    return;
                }

                await component.UpdateAsync(msg =>
                {
                    msg.Embed = commands.IssueListEmbed.BuildIssueListEmbed(dict, ++currentPage, tag).Build();
                });
            }else
                Console.WriteLine("An ID has been received that has no handler!");
        }

        if (interaction is SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                case "ISSUE_MODAL":
                {
                    var c = modal.Data.Components.ToArray();
                    object[] values = new object[c.Length];
                    for (int i = 0; i < c.Length; i++)
                        values[i] = c[i].Value ?? "NULL";

                    values[1] = c[1].Values.First();
                    //Console.WriteLine($"values[1] = {values[1]}");

                    string userId = modal.User.Id.ToString();
                    
                    var embed = new EmbedBuilder().WithTitle("이슈를 DB에 등록했습니다.")
                        .AddField("이슈 이름", values[0])
                        .AddField("이슈 태그", values[1])
                        .AddField("이슈 설명", values[2])
                        .WithColor(Color.Blue)
                        .WithCurrentTimestamp()
                        .Build();
                    bool result;
                    try
                    {
                         result = await Issue.IssueData.StoreIssueAsync(values[0].ToString(), values[2].ToString(),
                            Enum.Parse<IssueTag>(values[1].ToString(), true), userId);
                    }
                    catch (Exception e)
                    {
                        result = false;
                        Console.WriteLine(e.Message);
                    }
                    
                    if (!result)
                    {
                        var errorEmbed = new EmbedBuilder().WithTitle("이슈 등록에 실패했습니다.")
                            .WithColor(Color.Red)
                            .AddField("이슈 이름", values[0])
                            .AddField("이슈 태그", values[1])
                            .AddField("이슈 설명", values[2])
                            .WithCurrentTimestamp()
                            .Build();
                        await modal.RespondAsync(embed: errorEmbed, ephemeral: false);
                    }
                    else
                    {
                        await modal.RespondAsync(embed: embed, ephemeral: false);
                    }
                }   
                    break;
                default:
                    //await modal.RespondAsync("undfined command");
                    break;
            }
        }

        if (interaction is SocketSlashCommand slashCommand)
        {
            switch (slashCommand.Data.Name)
            {
                case "issue":
                    var subCommand = slashCommand.Data.Options.First().Name;
                    switch (subCommand)
                    {
                        case "new":
                            var inModal = new ModalBuilder()
                                .WithTitle("새 이슈")
                                .WithCustomId("ISSUE_MODAL")
                                .AddTextInput("이슈 이름", "issue_title", placeholder:"이슈 이름을 입력하세요", required:true)
                                .AddSelectMenu("이슈 태그", "issue_tag", options:new List<SelectMenuOptionBuilder>
                                {
                                    new SelectMenuOptionBuilder().WithLabel("Bug").WithValue("bug").WithDescription("오류가 발생했어요!")
                                    ,new SelectMenuOptionBuilder().WithLabel("Feature").WithValue("feature").WithDescription("새로운 기능을 제안해요!")
                                    ,new SelectMenuOptionBuilder().WithLabel("Enhancement").WithValue("enhancement").WithDescription("기존 기능을 개선해요!")
                                }, required:true)
                                .AddTextInput("이슈 설명", "issue_detail", placeholder:"이슈 설명을 입력하세요", required:true, style: TextInputStyle.Paragraph);
                                
                            await slashCommand.RespondWithModalAsync(inModal.Build());
                            break;
                        case "status":
                        {
                            string[] adminIds = EnvLoader.GetAdminIds();
                            if (!AdminTool.IsAdmin(slashCommand.User.Id.ToString()))
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("안돼")
                                    .WithDescription("넌 안돼.")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: true);
                                break;
                            }
                            
                            var issueId = (long)slashCommand.Data.Options.First().Options
                                .First(o => o.Name == "id").Value;
                            var newStatusStr = (string)slashCommand.Data.Options.First().Options
                                .First(o => o.Name == "change-to").Value;
                            var newStatus = Enum.Parse<IssueStatus>(newStatusStr, true);
                            bool result;
                            try
                            {
                                result = await Issue.IssueData.UpdateIssueStatusAsync((int)issueId, newStatus);
                            }
                            catch (Exception e)
                            {
                                result = false;
                                Console.WriteLine(e.Message);
                            }

                            if (!result)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("상태 변경에 실패했습니다")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                            else
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("성공했습니다")
                                    .WithDescription("상태 변경 성공")
                                    .AddField("Issue ID", issueId.ToString())
                                    .AddField("New Status", newStatus.ToString())
                                    .WithColor(Color.Green);

                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                            
                        }
                            break;
                        case "list":
                        {
                            string? tagStr = slashCommand.Data.Options.First().Options.FirstOrDefault()?.Value?.ToString();
                            IssueTag? tag = null;
                            if (!string.IsNullOrEmpty(tagStr))
                                tag = Enum.Parse<IssueTag>(tagStr, true);
                            
                            Dictionary<int, Issue.IssueContent> dict = await Issue.IssueData.ListOfIssueAsync(tag);
                            
                            await slashCommand.RespondAsync
                                (embed: commands.IssueListEmbed.BuildIssueListEmbed(dict,1 , tag).Build(),
                                    components: components.Pages.Button().Build(), ephemeral: false);
                        }
                            break;
                        case "export":
                        {
                            string? tagStr = slashCommand.Data.Options.First().Options.FirstOrDefault()?.Value?.ToString();
                            IssueTag? tag = null;
                            if (!string.IsNullOrEmpty(tagStr))
                                tag = Enum.Parse<IssueTag>(tagStr, true);
                            var dict = await Issue.IssueData.ListOfIssueAsync(tag);

                            var jsonList = new List<Issue.IssueJsonContent>(dict.Count);
                            foreach (var ctx in dict)
                            {
                                jsonList.Add(new IssueJsonContent()
                                {
                                    Id = ctx.Key,
                                    Name = ctx.Value.Name,
                                    Detail = ctx.Value.Detail,
                                    Tag = ctx.Value.Tag,
                                    Status = ctx.Value.Status,
                                    UserId = ctx.Value.UserId
                                });
                            }

                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true,IncludeFields = true,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                Converters = { new JsonStringEnumConverter() }
                            };
                            string finalJson = JsonSerializer.Serialize(jsonList, options);
                            
                            string msg = "```json\n" + finalJson + "\n```";
                            
                            await slashCommand.RespondAsync(msg, ephemeral: true);
                        }
                            break;
                        case "get":
                        {
                            var issueId = (long)slashCommand.Data.Options.First().Options
                                .First(o => o.Name == "id").Value;
                            
                            var ctx = await Issue.IssueData.GetIssueByIdAsync((int)issueId);
                            if (ctx == null)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("해당 이슈를 찾을 수 없습니다")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                            else
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("이슈")
                                    .WithDescription("이슈 정보를 불러왔습니다")
                                    .AddField("Name", ctx.Value.Name)
                                    .AddField("Issue ID", issueId.ToString(), true)
                                    .AddField("Detail", ctx.Value.Detail, true)
                                    .AddField("Tag", ctx.Value.Tag.ToString(),true)
                                    .AddField("Status", ctx.Value.Status.ToString(),true)
                                    .AddField("User", $"<@{ctx.Value.UserId}>", true)
                                    .WithColor(Color.Blue)
                                    .WithCurrentTimestamp();
                                
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                        }
                            break;
                        default:
                            //await slashCommand.RespondAsync("Unknown command");
                            break;
                    }
                    break;
                default:
                    await slashCommand.RespondAsync("Unkown command");
                    break;
            }
        }
    }
}

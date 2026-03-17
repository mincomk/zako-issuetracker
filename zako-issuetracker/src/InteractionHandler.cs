using Discord;
using Discord.WebSocket;
using Color = Discord.Color;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using zako_issuetracker.Issue;

namespace zako_issuetracker;

partial class Program
{
    private static async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        // safety-casting is the best way to prevent something being cast from being null.
        // If this check does not pass, it could not be cast to said type.
        if (interaction is SocketMessageComponent component)
        {
            // Check for the ID created in the button mentioned above.
            if (component.Data.CustomId == "issue-previous")
            {
                int currentPage = int.Parse(component.Message.Embeds.First().Title.Split().Last());
                IssueTag? tag;
                if (component.Message.Embeds.First().Description.Split().Last() != "All")
                    tag = Enum.Parse<IssueTag>(component.Message.Embeds.First().Description.Split().Last(), true);
                else
                    tag = null;

                if (currentPage <= 1)
                {
                    await component.RespondAsync("첫 페이지입니다!", ephemeral: true);
                    return;
                }

                Dictionary<int, IssueContent> dict = await IssueData.ListOfIssueAsync(tag);
                await component.UpdateAsync(msg =>
                {
                    msg.Embeds = commands.IssueListEmbed.BuildIssueListEmbed(dict, --currentPage, tag);
                });
            }
            else if (component.Data.CustomId == "issue-next")
            {
                int currentPage = int.Parse(component.Message.Embeds.First().Title.Split().Last());

                IssueTag? tag;
                if (component.Message.Embeds.First().Description.Split().Last() != "All")
                    tag = Enum.Parse<IssueTag>(component.Message.Embeds.First().Description.Split().Last(), true);
                else
                    tag = null;

                Dictionary<int, IssueContent> dict = await IssueData.ListOfIssueAsync(tag);
                int maxPage = (int)Math.Ceiling((double)dict.Count / EnvLoader.GetPageSize());
                if (currentPage >= maxPage)
                {
                    await component.RespondAsync("마지막 페이지입니다!", ephemeral: true);
                    return;
                }

                await component.UpdateAsync(msg =>
                {
                    msg.Embeds = commands.IssueListEmbed.BuildIssueListEmbed(dict, ++currentPage, tag);
                });
            }
            else
                Console.WriteLine("An ID has been received that has no handler!");
        }

        if (interaction is SocketModal modal)
        {
            // WithCustomId("ISSUE_MODAL_EDIT__" + inputId)

            if (modal.Data.CustomId.StartsWith("ISSUE_MODAL_EDIT__"))
            {
                int id = int.Parse(modal.Data.CustomId.Substring("ISSUE_MODAL_EDIT__".Length));
                //check user permission
                {
                    var issue = await IssueData.GetIssueByIdAsync(id);

                    // modification permission check
                    if (issue == null || issue.Value.Status == IssueStatus.Deleted)
                    {
                        var eb = new EmbedBuilder()
                            .WithTitle("오류가 발생했습니다")
                            .WithDescription("해당 이슈가 없던지 지워졌던지")
                            .WithColor(Color.Red)
                            .WithCurrentTimestamp();
                        await modal.RespondAsync(embed: eb.Build(), ephemeral: false);
                    }

                    if (issue.Value.UserId.ToString() != modal.User.Id.ToString()
                        && !AdminTool.IsAdmin(modal.User.Id.ToString()))
                    {
                        var eb = new EmbedBuilder()
                            .WithTitle("오류가 발생했습니다")
                            .WithDescription("남의 것을 탐하지 마라")
                            .WithColor(Color.Red)
                            .WithCurrentTimestamp();
                        await modal.RespondAsync(embed: eb.Build(), ephemeral: false);
                    }
                }
                
                var c = modal.Data.Components.ToArray();
                object[] values = new object[c.Length];
                for (int i = 0; i < c.Length; i++)
                    values[i] = c[i].Value ?? "NULL";

                values[1] = c[1].Values.First();

                var embed = new EmbedBuilder().WithTitle("수정된 이슈를 DB에 등록했습니다.")
                    .AddField("이슈 이름", values[0])
                    .AddField("이슈 태그", values[1])
                    .AddField("이슈 설명", values[2])
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build();
                bool result;

                IssueJsonContent Content = new IssueJsonContent();
                IssueContent origin = await IssueData.GetIssueByIdAsync(id) ?? throw new ArgumentNullException(); // ??
                try
                {
                    Content.Id = id;
                    Content.Name = values[0].ToString()!;
                    Content.Detail = values[2].ToString()!;
                    Content.Tag = Enum.Parse<IssueTag>(values[1].ToString()!, true);
                    Content.Status = origin.Status;
                    Content.UserId = origin.UserId;

                    result = await IssueData.UpdateIssueAsync(Content);
                }
                catch (Exception e)
                {
                    result = false;
                    Console.Error.WriteLine(e);
                }

                if (!result)
                {
                    var errorEmbed = new EmbedBuilder().WithTitle("이슈 수정을 실패했습니다.")
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

                    
                    int result;
                    try
                    {
                        result = await Issue.IssueData.StoreIssueAsync(values[0].ToString(), values[2].ToString(),
                            Enum.Parse<IssueTag>(values[1].ToString(), true), userId);
                    }
                    catch (Exception e)
                    {
                        result = -1;
                        Console.Error.WriteLine(e.Message);
                    }
                    
                    var embed = new EmbedBuilder().WithTitle("이슈를 DB에 등록했습니다.")
                        .WithDescription($"이슈 ID : {result}")
                        .AddField("이슈 이름", values[0])
                        .AddField("이슈 태그", values[1])
                        .AddField("이슈 설명", values[2])
                        .WithColor(Color.Blue)
                        .WithCurrentTimestamp()
                        .Build();

                    if (result == -1)
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
                    if (modal.Data.CustomId.StartsWith("ISSUE_MODAL_EDIT__"))
                        break;
                    await modal.RespondAsync("Undefined command", ephemeral: true);
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
                                .AddTextInput("이슈 이름", "issue_title", placeholder: "이슈 이름을 입력하세요", required: true)
                                .AddSelectMenu("이슈 태그", "issue_tag", options: new List<SelectMenuOptionBuilder>
                                {
                                    new SelectMenuOptionBuilder().WithLabel("Bug").WithValue("bug")
                                        .WithDescription("오류가 발생했어요!"),
                                    new SelectMenuOptionBuilder().WithLabel("Feature").WithValue("feature")
                                        .WithDescription("새로운 기능을 제안해요!"),
                                    new SelectMenuOptionBuilder().WithLabel("Enhancement").WithValue("enhancement")
                                        .WithDescription("기존 기능을 개선해요!")
                                }, required: true)
                                .AddTextInput("이슈 설명", "issue_detail", placeholder: "이슈 설명을 입력하세요", required: true,
                                    style: TextInputStyle.Paragraph);

                            await slashCommand.RespondWithModalAsync(inModal.Build());
                            break;
                        case "set-status":
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
                                result = await IssueData.UpdateIssueStatusAsync((int)issueId, newStatus);
                            }
                            catch (Exception e)
                            {
                                result = false;
                                await Console.Error.WriteLineAsync(e.Message);
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
                            string? tagStr = slashCommand.Data.Options.First().Options.FirstOrDefault()?.Value
                                ?.ToString();
                            IssueTag? tag = null;
                            if (!string.IsNullOrEmpty(tagStr))
                                tag = Enum.Parse<IssueTag>(tagStr, true);

                            string? statusStr = slashCommand.Data.Options.First().Options
                                .FirstOrDefault(o => o.Name == "status")?.Value?.ToString();
                            IssueStatus? status = null;
                            if (!string.IsNullOrEmpty(statusStr))
                                status = Enum.Parse<IssueStatus>(statusStr, true);

                            Dictionary<int, IssueContent> dict = await IssueData.ListOfIssueAsync(tag);

                            if (dict.Count == 0)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("없어요 없어요")
                                    .WithDescription("이슈가 없어요")
                                    .WithImageUrl(EnvLoader.GetImgLink())
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: true);
                                break;
                            }

                            await slashCommand.RespondAsync
                            (embeds: commands.IssueListEmbed.BuildIssueListEmbed(dict, 1, tag, status),
                                components: components.Pages.Button().Build(), ephemeral: false);
                        }
                            break;
                        case "export":
                        {
                            string? tagStr = slashCommand.Data.Options.First().Options.FirstOrDefault()?.Value
                                ?.ToString();
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
                                WriteIndented = true, IncludeFields = true,
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
                            var sourceStr = slashCommand.Data.Options.First().Options
                                .FirstOrDefault(o => o.Name == "source")?.Value?.ToString() ?? "local";

                            if (sourceStr == "github")
                            {
                                var ghIssue = await IssueData.GetGitHubIssueAsync((int)issueId);
                                if (ghIssue == null)
                                {
                                    var eb = new EmbedBuilder()
                                        .WithTitle("오류가 발생했습니다")
                                        .WithDescription("해당 GitHub 이슈를 찾을 수 없습니다")
                                        .WithColor(Color.Red)
                                        .WithCurrentTimestamp();
                                    await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                }
                                else
                                {
                                    string typeLabel = ghIssue.Value.IsPullRequest ? "Pull Request" : "Issue";
                                    var eb = new EmbedBuilder()
                                        .WithTitle($"이슈 [GitHub]")
                                        .WithDescription("GitHub 이슈 정보를 불러왔습니다")
                                        .AddField("Name", ghIssue.Value.Name)
                                        .AddField("Issue ID", $"[GH] #{ghIssue.Value.GitHubNumber}", true)
                                        .AddField("Type", typeLabel, true)
                                        .AddField("Detail", ghIssue.Value.Detail, true)
                                        .AddField("Tag", ghIssue.Value.Tag.ToString(), true)
                                        .AddField("Status", ghIssue.Value.Status.ToString(), true)
                                        .AddField("Author", $"@{ghIssue.Value.UserId}", true)
                                        .AddField("Link", ghIssue.Value.HtmlUrl)
                                        .WithColor(commands.IssueListEmbed.GitHubColor)
                                        .WithCurrentTimestamp();

                                    await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                }
                            }
                            else
                            {
                                var ctx = await IssueData.GetIssueByIdAsync((int)issueId);
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
                                        .AddField("Tag", ctx.Value.Tag.ToString(), true)
                                        .AddField("Status", ctx.Value.Status.ToString(), true)
                                        .AddField("User", $"<@{ctx.Value.UserId}>", true)
                                        .WithColor(Color.Blue)
                                        .WithCurrentTimestamp();

                                    await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                }
                            }
                        }
                            break;
                        case "delete":
                        {
                            int Id = (int)(long)slashCommand.Data.Options.First()
                                .Options.First(o => o.Name == "id").Value;

                            // user permission check
                            
                            var issue = await IssueData.GetIssueByIdAsync(Id);
                            if (issue == null || issue.Value.Status == IssueStatus.Deleted)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("해당 이슈가 없던지 지워졌던지")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                break;
                            }
                            if (!AdminTool.IsAdmin(slashCommand.User.Id.ToString()))
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("감히 관리자를 무시하다니")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                break;
                            }
                            
                            // action
                            bool result;
                            try
                            {
                                result = await IssueData.DeleteIssueAsync(Id);
                            }
                            catch (Exception e)
                            {
                                result = false;
                            }

                            if (!result)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("이슈 삭제에 실패했습니다")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                            else
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("이슈 삭제 성공")
                                    .WithDescription($"ID {Id} 이슈를 삭제했습니다.")
                                    .WithColor(Color.Green)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                            }
                        }
                            break;
                        case "edit":
                        {
                            int inputId = (int)slashCommand.Data.Options.First(o => o.Name == "id").Value;
                            var result = await IssueData.GetIssueByIdAsync(inputId);

                            // modification permission check
                            if (result == null || result.Value.Status == IssueStatus.Deleted)
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("해당 이슈가 없던지 지워졌던지")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                break;
                            }

                            if (result.Value.UserId.ToString() != slashCommand.User.Id.ToString()
                                && !AdminTool.IsAdmin(slashCommand.User.Id.ToString()))
                            {
                                var eb = new EmbedBuilder()
                                    .WithTitle("오류가 발생했습니다")
                                    .WithDescription("남의 것을 탐하지 마라")
                                    .WithColor(Color.Red)
                                    .WithCurrentTimestamp();
                                await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                                break;
                            }

                            string defaultTag = result.Value.Tag switch
                            {
                                IssueTag.Bug => "bug",
                                IssueTag.Feature => "feature",
                                IssueTag.Enhancement => "enhancement"
                            };

                            var eModal = new ModalBuilder()
                                .WithTitle("이슈 수정 #" + inputId)
                                .WithCustomId("ISSUE_MODAL_EDIT__" + inputId)
                                .AddTextInput("이슈 이름", "issue_title", value: result.Value.Name,
                                    placeholder: "이슈 이름을 입력하세요", required: true)
                                //.AddTextInput("이슈 Id(수정금지)", "issue_id", value: inputId.ToString(), required: true)
                                .AddSelectMenu("이슈 태그", "issue_tag", options: new List<SelectMenuOptionBuilder>
                                {
                                    new SelectMenuOptionBuilder().WithLabel("Bug").WithValue("bug")
                                        .WithDescription("오류가 발생했어요!").WithDefault(defaultTag == "bug"),
                                    new SelectMenuOptionBuilder().WithLabel("Feature").WithValue("feature")
                                        .WithDescription("새로운 기능을 제안해요!").WithDefault(defaultTag == "feature"),
                                    new SelectMenuOptionBuilder().WithLabel("Enhancement").WithValue("enhancement")
                                        .WithDescription("기존 기능을 개선해요!").WithDefault(defaultTag == "enhancement")
                                }, required: true)
                                .AddTextInput("이슈 설명", "issue_detail", value: result.Value.Detail,
                                    placeholder: "이슈 설명을 입력하세요", required: true,
                                    style: TextInputStyle.Paragraph);

                            await slashCommand.RespondWithModalAsync(eModal.Build());
                            break;
                        }
                    }

                    break;
                case "ping":
                    await slashCommand.RespondAsync($"Pong! {_client.Latency}");
                    break;
                case "zakonim":
                {
                    SocketUser who = (SocketUser)slashCommand.Data.Options.First(o => o.Name == "who").Value;
                    string description =
                        slashCommand.Data.Options.First(o => o.Name == "description").Value.ToString() ??
                        "No description";

                    await using var con = new SqliteConnection("Data Source=" + DataBaseHelper.dbPath);
                    con.Open();
                    var cmd = con.CreateCommand();
                    try
                    {
                        cmd.CommandText = "INSERT INTO zakonim (id, description) VALUES (@id, @description)";
                        cmd.Parameters.AddWithValue("@id", who.Id.ToString());
                        cmd.Parameters.AddWithValue("@description", description);
                        await cmd.ExecuteNonQueryAsync();

                        cmd.CommandText = "SELECT COUNT(*) FROM zakonim";
                        await using var reader = await cmd.ExecuteReaderAsync();

                        int count = 0;
                        if (await reader.ReadAsync())
                        {
                            count = reader.GetInt32(0);
                        }

                        var eb = new EmbedBuilder()
                            .WithTitle("자코는 봇이에요!")
                            .WithDescription($"{who.Mention}님은 {count}번째로 자코를 사람으로 오해하셨습니다.")
                            .AddField("멘트", description);
                        await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                    }
                    catch (SqliteException e)
                    {
                        int code = e.SqliteErrorCode;

                        var eb = new EmbedBuilder();
                        if (code == 19) // UNIQUE constraint failed
                        {
                            eb = new EmbedBuilder()
                                .WithTitle("이미 허접입니다")
                                .WithDescription($"{who.Mention}님은 이미 자코님 DB에 등록되어 있습니다.")
                                .WithColor(Color.Orange)
                                .WithCurrentTimestamp();
                            await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: true);
                            break;
                        }
                        else
                        {
                            eb = new EmbedBuilder()
                                .WithTitle("??")
                                .WithDescription("오류남")
                                .WithColor(Color.DarkRed)
                                .WithCurrentTimestamp();
                            await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: false);
                        }

                        Console.Error.WriteLine(e);
                    }
                    catch (Exception e)
                    {
                        var eb = new EmbedBuilder()
                            .WithTitle("???")
                            .WithDescription("오류남")
                            .WithColor(Color.Red)
                            .WithCurrentTimestamp();

                        await slashCommand.RespondAsync(embed: eb.Build(), ephemeral: true);

                        Console.Error.WriteLine(e);
                    }
                    finally
                    {
                        con.Close();
                    }
                }
                    break;
                default:
                    await slashCommand.RespondAsync("Unknown command");
                    break;
            }
        }
    }
}
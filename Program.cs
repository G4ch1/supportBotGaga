using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace supportBotGaga
{
    internal class Program
    {
        // setup our fields we assign later
        private readonly IConfiguration _config;
        private DiscordSocketClient _client;
        private InteractionService _commands;


        public Program()
        {
            // create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");

            // build the configuration and assign to _config          
            _config = _builder.Build();
        }

        static Task Main(string[] args) => new Program().MainAsync(args);

        public async Task MainAsync(string[] args)
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMembers |
                                 GatewayIntents.GuildBans |
                                 GatewayIntents.GuildPresences |
                                 GatewayIntents.GuildIntegrations |
                                 GatewayIntents.GuildVoiceStates |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.GuildMessageTyping |
                                 GatewayIntents.MessageContent |
                                 GatewayIntents.DirectMessages
            };
            _client = new DiscordSocketClient(config);
            _commands = new InteractionService(_client.Rest);

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.ChannelCreated += OnChannelCreated;
            _client.UserLeft += OnUserLeft;
            _client.ChannelDestroyed += OnChannelDestroyed;
            _client.LeftGuild += OnLeftGuild;
            _client.RoleDeleted += OnRoleDeleted;
            _client.UserBanned += OnUserBanned;
            _client.UserJoined += OnUserJoined;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            _client.MessageReceived += HandleMessage;

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);

        }

        private Dictionary<ulong, DateTime> _userLastMessageTimes = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, int> _userMessageCount = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> _userActivityPoints = new Dictionary<ulong, int>();


        private async Task HandleMessage(SocketMessage message)
        {
            if (!(message.Author is SocketGuildUser user))
                return;

            // Проверяем, если пользователь уже в муте, не начисляем баллы
            if (user.Roles.Any(r => r.Id == ulong.Parse(_config["muteRoles"])))
                return;

            // Проверяем, есть ли пользователь в словаре
            if (!_userLastMessageTimes.ContainsKey(user.Id))
            {
                _userLastMessageTimes.Add(user.Id, DateTime.UtcNow);
                _userMessageCount.Add(user.Id, 1);
                return;
            }

            var currentTime = DateTime.UtcNow;
            var lastMessageTime = _userLastMessageTimes[user.Id];
            var messageCount = _userMessageCount[user.Id];

            // Проверяем, если пользователь пишет в канале для флуда, не начисляем баллы
            if (message.Channel.Id != ulong.Parse(_config["logChanel"]))
            {
                // Проверяем, если пользователь отправляет больше 3 сообщений подряд, не начисляем баллы
                var userActivityPoints = _userActivityPoints.ContainsKey(user.Id) ? _userActivityPoints[user.Id] : 0;
                if (userActivityPoints >= 3)
                    return;
                else
                {
                    // Начисляем баллы активности пользователю
                    var pointsToAdd = 1; // Количество баллов, которое будет начислено за каждое сообщение
                    _userActivityPoints[user.Id] = userActivityPoints + pointsToAdd;
                    RequestHandler.ExecuteWrite($"UPDATE Users SET  Currency = Currency + 1 WHERE DiscordId = '{user.Id}'");
                }
            }

            // Проверяем, если прошло более 30 секунд, сбрасываем счетчик
            if ((currentTime - lastMessageTime).TotalSeconds > 30)
            {
                _userLastMessageTimes[user.Id] = currentTime;
                _userMessageCount[user.Id] = 1;
                return;
            }

            // Увеличиваем счетчик сообщений пользователя
            _userMessageCount[user.Id]++;

            // Если пользователь превысил лимит сообщений, выдаем роль "мут" на 10 минут
            if (_userMessageCount[user.Id] > 10)
            {
                var muteRole = user.Guild.Roles.FirstOrDefault(r => r.Id == ulong.Parse(_config["muteRoles"]));
                if (muteRole != null)
                {
                    await user.AddRoleAsync(muteRole);
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    await user.RemoveRoleAsync(muteRole);
                }

                // Сбрасываем счетчик и время последнего сообщения пользователя
                _userLastMessageTimes[user.Id] = currentTime;
                _userMessageCount[user.Id] = 1;
            }
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            await TimeVoice.OnVoiceStateUpdatedAsync(user, before, after);

            if (before.VoiceChannel == null && after.VoiceChannel != null)
            {
                ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
                var EmbedBuilderLog = new EmbedBuilder()
                    .WithAuthor(user.Username,user.GetAvatarUrl())
                    .WithDescription($"{user.Mention} присоединился к голосовому чату {after.VoiceChannel.Name}.")
                    .WithFooter(footer =>
                    {
                        footer
                        .WithIconUrl(channel.Guild.BannerUrl)
                        .WithText(channel.Guild.Name);
                    }).WithCurrentTimestamp();
                Embed embedLog = EmbedBuilderLog.Build();
                await channel.SendMessageAsync(embed: embedLog);
            }
            if (before.VoiceChannel != null && after.VoiceChannel == null)
            {
                ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
                var EmbedBuilderLog = new EmbedBuilder()
                    .WithAuthor(user.Username, user.GetAvatarUrl())
                    .WithDescription($"{user.Mention} отключился от голосового канала {before.VoiceChannel.Name}.")
                    .WithFooter(footer =>
                    {
                        footer
                        .WithIconUrl(channel.Guild.BannerUrl)
                        .WithText(channel.Guild.Name);
                    }).WithCurrentTimestamp();
                Embed embedLog = EmbedBuilderLog.Build();
                await channel.SendMessageAsync(embed: embedLog);
            }
            if (before.VoiceChannel != null)
            {
                if (before.IsMuted == false && after.IsMuted == true)
                {
                    ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
                    var EmbedBuilderLog = new EmbedBuilder()
                        .WithAuthor(user.Username, user.GetAvatarUrl())
                        .WithDescription($"{user.Mention} получил мут.")
                        .WithFooter(footer =>
                        {
                            footer
                            .WithIconUrl(channel.Guild.BannerUrl)
                            .WithText(channel.Guild.Name);
                        }).WithCurrentTimestamp();
                    Embed embedLog = EmbedBuilderLog.Build();
                    await channel.SendMessageAsync(embed: embedLog);
                }
                if (before.IsMuted == true && after.IsMuted == false)
                {
                    ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
                    var EmbedBuilderLog = new EmbedBuilder()
                        .WithAuthor(user.Username, user.GetAvatarUrl())
                        .WithDescription($"{user.Mention} мут снят.")
                        .WithFooter(footer =>
                        {
                            footer
                            .WithIconUrl(channel.Guild.BannerUrl)
                            .WithText(channel.Guild.Name);
                        }).WithCurrentTimestamp();
                    Embed embedLog = EmbedBuilderLog.Build();
                    await channel.SendMessageAsync(embed: embedLog);
                }    
            }
        }

        private async Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
            var EmbedBuilderLog = new EmbedBuilder()
                .WithDescription($"{user.Mention} был забанен на сервере {guild.Name}.")
                .WithFooter(footer =>
                {
                    footer
                    .WithText("User ban log")
                    .WithIconUrl(user.GetAvatarUrl());
                });
            Embed embedLog = EmbedBuilderLog.Build();
            await channel.SendMessageAsync(embed: embedLog);
        }

        private async Task OnRoleDeleted(SocketRole role)
        {
            ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
            var EmbedBuilderLog = new EmbedBuilder()
                .WithDescription($"Роль {role.Name} была удалена.")
                .WithFooter(footer =>
                {
                    footer
                    .WithText("Role delete log")
                    .WithIconUrl(_client.CurrentUser.GetAvatarUrl());
                });
            Embed embedLog = EmbedBuilderLog.Build();
            await channel.SendMessageAsync(embed: embedLog);
        }

        private async Task OnChannelDestroyed(SocketChannel channel)
        {
            if (channel is not SocketGuildChannel and ISocketMessageChannel)
                await Task.CompletedTask;
            _ = ((ISocketMessageChannel)channel).SendMessageAsync(embed: new EmbedBuilder()
                .WithColor((Discord.Color)System.Drawing.Color.Green)
                .WithTitle("Info")
                .AddField("Channel detected:", "Channel deleted.")
                .WithFooter("GagaBot moderation")
                .WithCurrentTimestamp().Build());
            await Task.CompletedTask;
        }

        private async Task OnLeftGuild(SocketGuild guild)
        {
            _ = guild.DeleteApplicationCommandsAsync();
            /*_ = Task.Run(() =>
            {
                using var db = new Database();
                var dbGuild = db.GetGuild(guild.Id);
                db.DeleteGuild(dbGuild);
            });*/
            await Task.CompletedTask;
        }

        private async Task OnChannelCreated(SocketChannel channel)
        {
            if (channel is not SocketGuildChannel and ISocketMessageChannel)
                await Task.CompletedTask;
            _ = ((ISocketMessageChannel)channel).SendMessageAsync(embed: new EmbedBuilder()
                .WithColor((Discord.Color)System.Drawing.Color.Green)
                .WithTitle("Info")
                .AddField("New channel detected:", "Channel added in database with default settings.")
                .WithFooter("GagaBot moderation")
                .WithCurrentTimestamp().Build());
            await Task.CompletedTask;
        }

        private async Task OnUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User joined.");

            var query = RequestHandler.ExecuteReader($"SELECT * FROM Users WHERE DiscordId = '{user.Id}'");

            if(!query.HasRows) // построчно считываем данные
            {
                RequestHandler.ExecuteWrite($"INSERT INTO Users (DiscordId, MessagesCount, Currency, LikesCount, LastActivity, VoiceTime)" +
                $"\r\n VALUES ('{user.Id}', 0, 0, 0, GETDATE(), '00:00:00')");

                Console.WriteLine("User added to database.");
            }

            // получаем роль, которую необходимо выдать
            var role = user.Guild.GetRole(ulong.Parse(_config["newUserRole"]));

            // выдаем роль пользователю
            await user.AddRoleAsync(role);

            ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
            var EmbedBuilderLog = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription($"{user.Mention} присоединился к серверу.")
                .WithFooter(footer =>
                {
                    footer
                    .WithText("User join log")
                    .WithIconUrl(user.GetAvatarUrl());
                });
            Embed embedLog = EmbedBuilderLog.Build();
            await channel.SendMessageAsync(embed: embedLog);
        }

        private async Task OnUserLeft(SocketGuild guild, SocketUser user)
        {
            //await new RequestHandler().ExecuteWriteAsync($"DELETE FROM Users WHERE DiscordId = '{user.Id}'");

            await guild.SystemChannel.SendMessageAsync($"User {user.Username} has left the server.");

            ITextChannel? channel = _client.GetChannel(ulong.Parse(_config["logChanel"])) as ITextChannel;
            var EmbedBuilderLog = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription($"{user.Mention} вышел с сервера.")
                .WithFooter(footer =>
                {
                    footer
                    .WithText("User left log")
                    .WithIconUrl(user.GetAvatarUrl());
                });
            Embed embedLog = EmbedBuilderLog.Build();
            await channel.SendMessageAsync(embed: embedLog);

            Console.WriteLine("User left");
        }

        private async Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            await Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            await _client.SetStatusAsync(UserStatus.Online);
            Console.WriteLine($"Connected as -> [{_client.CurrentUser}] :)");

            if (IsDebug())
            {
                // здесь вы указываете id тестовой гильдии дискорда
                Console.WriteLine($"In debug mode, adding commands to {ulong.Parse(_config["serverId"])}...");
                await _commands.RegisterCommandsToGuildAsync(ulong.Parse(_config["serverId"]));
            }
            else
            {
                // this method will add commands globally, but can take around an hour
                await _commands.RegisterCommandsGloballyAsync(true);
            }
        }

        static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
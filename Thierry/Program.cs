using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Thierry
{
    public class Program
    {
        public static readonly DiscordSocketClient Client =
            new DiscordSocketClient(new DiscordSocketConfig {LogLevel = LogSeverity.Verbose});

        public static Guild Guild;
        private static IConfigurationRoot _config;
        private static bool _idmode;
        private static bool _ready;
        private static string _token;
        private CommandServiceConfig _commandConfig;
        private CommandService _commands;
        private IServiceProvider _services;

        public static void Main(string[] args)
        {
            new Program().MainAsync(args).GetAwaiter().GetResult();
        }

        public void CheckHat()
        {
            if (!Guild.HatRole.Members.Any()) return;
            Guild.LastHat = Guild.HatRole.Members.Last();
            Guild.HatRole.Members.AsParallel().ForAll(
                async x =>
                {
                    if (x == Guild.LastHat) return;
                    await Log(string.Format("User id: {0}, Username: {1}", x.Id, x.Username));
                    await x.RemoveRoleAsync(Guild.HatRole);
                });
        }

        public async void GetIDs()
        {
            foreach (var guild in Client.Guilds)
            {
                await Log(string.Format("Guild id: {0}, Guild name: {1}", guild.Id, guild.Name));

                await Log("Roles:");
                foreach (var role in guild.Roles.OrderBy(role => role.Id))
                    await Log(string.Format("Role id: {0}, Role name: {1}", role.Id, role.Name));

                await Log("Text Channels:");
                foreach (var channel in guild.TextChannels.OrderBy(channel => channel.Id))
                    await Log(string.Format("Channel id: {0}, Channel name: {1}", channel.Id, channel.Name));

                await Log("Voice Channels:");
                foreach (var channel in guild.VoiceChannels.OrderBy(channel => channel.Id))
                    await Log(string.Format("Channel id: {0}, Channel name: {1}", channel.Id, channel.Name));
            }
        }

        public async Task GiveHat(SocketGuildUser user)
        {
            Guild.HatBeingMoved = true;
            if (Guild.LastHat != null)
            {
                await Guild.LastHat.RemoveRoleAsync(Guild.HatRole);
                UpdateVoiceChannel();
            }

            Guild.LastHat = Guild.SocketGuild.GetUser(user.Id);
            await Guild.LastHat.AddRoleAsync(Guild.HatRole);
            UpdateVoiceChannel();
        }

        public async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (!_ready) return;
            if (Guild.HatBeingMoved) return;

            if (before.Id == Guild.LastHat?.Id && !after.Roles.Contains(Guild.HatRole))
            {
                await Log(string.Format("Hat illegally removed from {0}. Reassigning hat to {0}.", after.Username));
                await after.AddRoleAsync(Guild.HatRole);
            }

            if (after.Roles.Contains(Guild.HatRole) && after.Id != Guild.LastHat?.Id)
            {
                await Log(string.Format("Hat illegally given to {0}. Removing hat from {0}.", after.Username));
                await after.RemoveRoleAsync(Guild.HatRole);
            }

            Guild.HatBeingMoved = false;
        }

        public async Task InstallCommands()
        {
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task Log(LogMessage msg)
        {
            await Log("[API] [" + msg.Severity + "] [" + msg.Source + "] " + msg.Message);
        }

        public async Task Log(string msg)
        {
            await Task.Run(() => { Console.WriteLine("[" + DateTime.Now + "]" + " " + msg); });
        }

        public async Task MainAsync(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true);
            _config = builder.Build();

            _commandConfig = new CommandServiceConfig
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Verbose
            };
            _commands = new CommandService(_commandConfig);

            _services = new ServiceCollection().BuildServiceProvider();

            await InstallCommands();

            if (args.Length != 0 && args[0] == "-id") _idmode = true;

            // Set up events.
            Client.Log += Log;
            Client.Ready += Ready;
            Client.MessageReceived += MessageReceived;
            Client.GuildMemberUpdated += GuildMemberUpdated;

            _token = _config?["token"];
            await Client.StartAsync();
            await Client.LoginAsync(TokenType.Bot, _token);

            await Task.Delay(-1);
        }

        public async Task MessageReceived(SocketMessage msg)
        {
            // Don't process the command if it was a System Message
            if (!(msg is SocketUserMessage message)) return;
            // Log the message
            await Log(
                $"Channel id: {msg.Channel.Id}, Channel name: {message.Channel.Name}, Author id: {message.Author.Id}, Author: {message.Author.Username}, Message: {message.Content}");
            // Check if user is allowed to talk
            if ((message.Author as SocketGuildUser).Roles.Contains(Guild.MutedRole))
            {
                await msg.Channel.SendMessageAsync("Zwijgen trut!");
                return;
            }

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                if (result.Error == CommandError.UnknownCommand)
                    return;

            await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async Task Ready()
        {
            await Log("Ready for action!");

            if (_idmode)
            {
                GetIDs();
                await Log("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            Guild = new Guild(
                ulong.Parse(_config?["guildId"]),
                ulong.Parse(_config?["mutedRoleId"]),
                ulong.Parse(_config?["hatRoleId"]),
                ulong.Parse(_config?["hatminRoleId"]),
                ulong.Parse(_config?["afkChannelId"])
            );

            CheckHat();

            _ready = true;
        }

        public void RemoveHat()
        {
            Guild.LastHat.RemoveRoleAsync(Guild.HatRole);
            Guild.LastHat = null;
        }

        // Because Discord does not automatically unmute someone after they've been given speaking rights.
        public async void UpdateVoiceChannel()
        {
            if (!Guild.LastHat.Roles.Contains(Guild.MutedRole)) return;
            var channel = Guild.LastHat.VoiceChannel;
            if (channel == null) return;
            await Guild.LastHat.ModifyAsync(x => x.Channel = Guild.AfkVoiceChannel);
            await Guild.LastHat.ModifyAsync(x => x.Channel = channel);
        }
    }
}
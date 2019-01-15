using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Thierry
{
    public class Program
    {
        private static bool _idMode;
        private static bool _ready;

        private static readonly DiscordSocketClient Client =
            new DiscordSocketClient(new DiscordSocketConfig {LogLevel = LogSeverity.Verbose});

        private CommandServiceConfig _commandConfig;
        private CommandService _commands;
        private IServiceProvider _services;

        // Checks to see if more than 1 person has the hat, and removes extra hats if necessary
        private void CheckHat(SocketGuild guild)
        {
            if (guild == null) return;
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == guild.Id);
            if (g.HatRoleId == 0) return;
            g.LastHatId = guild.GetRole(g.HatRoleId).Members.Last().Id;
            guild.GetRole(g.HatRoleId).Members.AsParallel().ForAll(
                async x =>
                {
                    if (x.Id == g.LastHatId) return;
                    await Log($"User id: {x.Id}, Username: {x.Username}");
                    await x.RemoveRoleAsync(guild.GetRole(g.HatRoleId));
                });
        }

        private async void GetIDs()
        {
            foreach (var guild in Client.Guilds)
            {
                await Log($"Guild id: {guild.Id}, Guild name: {guild.Name}");

                await Log("Roles:");
                foreach (var role in guild.Roles.OrderBy(role => role.Id))
                    await Log($"Role id: {role.Id}, Role name: {role.Name}");

                await Log("Text Channels:");
                foreach (var channel in guild.TextChannels.OrderBy(channel => channel.Id))
                    await Log($"Channel id: {channel.Id}, Channel name: {channel.Name}");

                await Log("Voice Channels:");
                foreach (var channel in guild.VoiceChannels.OrderBy(channel => channel.Id))
                    await Log($"Channel id: {channel.Id}, Channel name: {channel.Name}");
            }
        }

        public static async Task GiveHat(SocketGuildUser user)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == user.Guild.Id);
            g.HatBeingMoved = true;
            if (g.LastHatId != 0)
            {
                await user.Guild.GetRole(g.HatRoleId).Members.First().RemoveRoleAsync(user.Guild.GetRole(g.HatRoleId));
                UpdateVoiceChannel(user.Guild);
            }

            g.LastHatId = user.Id;
            await user.AddRoleAsync(user.Guild.GetRole(g.HatRoleId));
            UpdateVoiceChannel(user.Guild);
        }

        private async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (!_ready) return;
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == before.Guild.Id);

            if (g.HatBeingMoved) return;

            if (before.Id == g.LastHatId && !after.Roles.Contains(after.Guild.GetRole(g.HatRoleId)))
            {
                await Log($"Hat illegally removed from {after.Username}. Reassigning hat to {after.Username}.");
                await after.AddRoleAsync(after.Guild.GetRole(g.HatRoleId));
            }

            if (after.Roles.Contains(after.Guild.GetRole(g.HatRoleId)) && after.Id != g.LastHatId)
            {
                await Log($"Hat illegally given to {after.Username}. Removing hat from {after.Username}.");
                await after.RemoveRoleAsync(after.Guild.GetRole(g.HatRoleId));
            }

            g.HatBeingMoved = false;
        }

        private async Task InstallCommands()
        {
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task Log(LogMessage msg)
        {
            await Log("[API] [" + msg.Severity + "] [" + msg.Source + "] " + msg.Message);
        }

        private async Task Log(string msg)
        {
            await Task.Run(() => { Console.WriteLine("[" + DateTime.Now + "]" + " " + msg); });
        }

        public static void Main(string[] args)
        {
            new Program().MainAsync(args).GetAwaiter().GetResult();
        }

        private async Task MainAsync(string[] args)
        {
            Configuration.LoadConfig();

            _commandConfig = new CommandServiceConfig
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Verbose
            };
            _commands = new CommandService(_commandConfig);

            _services = new ServiceCollection().BuildServiceProvider();

            await InstallCommands();

            if (args.Length != 0 && args[0] == "-id") _idMode = true;

            // Set up events.
            Client.Log += Log;
            Client.Ready += Ready;
            Client.MessageReceived += MessageReceived;
            Client.GuildMemberUpdated += GuildMemberUpdated;

            await Client.StartAsync();
            await Client.LoginAsync(TokenType.Bot, Configuration.Config.Token);

            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            // Don't process the command if it was a System Message
            if (!(msg is SocketUserMessage message) || message.Author.IsBot) return;

            var sgUser = (SocketGuildUser) message.Author;
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == sgUser.Guild.Id);
            // Log the message
            await Log(
                $"Channel id: {msg.Channel.Id}, Channel name: {message.Channel.Name}, Author id: {message.Author.Id}, Author: {message.Author.Username}, Message: {message.Content}");
            // check bad words
            if (g.BadWordsEnabled && g.BadWords.Any(x => message.Content.Contains(x)))
            {
                await message.DeleteAsync();
                return;
            }

            // Check if user is allowed to talk
            if (sgUser.Roles.Contains(sgUser.Guild.GetRole(g.MutedRoleId)))
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
            var context = new SocketCommandContext(Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
            {
                if (result.Error == CommandError.UnknownCommand)
                    return;
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async Task Ready()
        {
            await Log("Ready for action!");

            if (_idMode)
            {
                GetIDs();
                await Log("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            foreach (var guild in Client.Guilds) CheckHat(guild);

            _ready = true;
        }

        public static void RemoveHat(SocketGuildUser user)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == user.Guild.Id);

            user.RemoveRoleAsync(user.Guild.GetRole(g.HatRoleId));
            g.LastHatId = 0;
        }

        // Because Discord does not automatically unmute someone after they've been given speaking rights.
        private static async void UpdateVoiceChannel(SocketGuild guild)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == guild.Id);

            if (!guild.Users.First(x => x.Id == g.LastHatId).Roles.Contains(guild.GetRole(g.MutedRoleId))) return;
            var channel = guild.Users.First(x => x.Id == g.LastHatId).VoiceChannel;
            if (channel == null) return;

            await guild.Users.First(x => x.Id == g.LastHatId)
                .ModifyAsync(x => x.Channel = guild.GetVoiceChannel(g.AfkVoiceChannelId));
            await guild.Users.First(x => x.Id == g.LastHatId).ModifyAsync(x => x.Channel = channel);
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
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
        private static string _token;
        public static Guild _guild;
        private static bool _ready;
        private static bool _idmode;
        private static IConfigurationRoot _config;
        private CommandServiceConfig commandConfig;
        private CommandService commands;
        private IServiceProvider services;

        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true);
            _config = builder.Build();

            commandConfig = new CommandServiceConfig() { CaseSensitiveCommands = true, DefaultRunMode = RunMode.Async, LogLevel = LogSeverity.Verbose };
            commands = new CommandService(commandConfig);

            services = new ServiceCollection().BuildServiceProvider();

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

        public async Task InstallCommands()
        {
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
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

            _guild = new Guild(
                ulong.Parse(_config?["guildId"]),
                ulong.Parse(_config?["mutedRoleId"]),
                ulong.Parse(_config?["hatRoleId"]),
                ulong.Parse(_config?["hatminRoleId"]),
                ulong.Parse(_config?["afkChannelId"])
            );

            CheckHat();

            _ready = true;
        }

        public async Task Log(LogMessage msg)
        {
            await Log("[API] [" + msg.Severity + "] [" + msg.Source + "] " + msg.Message);
        }

        public async Task Log(string msg)
        {
            await Task.Run(() => { Console.WriteLine("[" + DateTime.Now + "]" + " " + msg); });
        }

        /*
        private async Task MessageReceived(SocketMessage msg)
        {
            // TODO: Add multi-guild support.

            // var guild = (msg.Channel as SocketGuildChannel)?.Guild;

            await Log(
                $"Channel id: {msg.Channel.Id}, Channel name: {msg.Channel.Name}, Author id: {msg.Author.Id}, Author: {msg.Author.Username}, Message: {msg.Content}");

            if (!_ready)
            {
                await msg.Channel.SendMessageAsync("I am performing initial setup. Ignoring command.");
                return;
            }
        }
        */

        public async Task MessageReceived(SocketMessage msg)
        {
            // Don't process the command if it was a System Message
            var message = msg as SocketUserMessage;
            if (message == null) return;
            //
            if ((message.Author as SocketGuildUser).Roles.Contains(_guild.MutedRole))
            {
                await msg.Channel.SendMessageAsync("Zwijgen trut!");
                return;
            }
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                if (result.Error == CommandError.UnknownCommand)
                {
                    return;
                }
            await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        public async void GiveHat(SocketUser user)
        {
            _guild.HatBeingMoved = true;
            if (_guild.LastHat != null)
            {
                await _guild.LastHat.RemoveRoleAsync(_guild.HatRole);
                UpdateVoiceChannel();
            }

            _guild.LastHat = _guild.SocketGuild.GetUser(user.Id);
            await _guild.LastHat.AddRoleAsync(_guild.HatRole);
            UpdateVoiceChannel();
        }

        public void RemoveHat()
        {
            _guild.LastHat.RemoveRoleAsync(_guild.HatRole);
            _guild.LastHat = null;
        }

        public async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (!_ready) return;
            if (_guild.HatBeingMoved) return;

            if (before.Id == _guild.LastHat?.Id && !after.Roles.Contains(_guild.HatRole))
            {
                await Log(string.Format("Hat illegally removed from {0}. Reassigning hat to {0}.", after.Username));
                await after.AddRoleAsync(_guild.HatRole);
            }

            if (after.Roles.Contains(_guild.HatRole) && after.Id != _guild.LastHat?.Id)
            {
                await Log(string.Format("Hat illegally given to {0}. Removing hat from {0}.", after.Username));
                await after.RemoveRoleAsync(_guild.HatRole);
            }

            _guild.HatBeingMoved = false;
        }

        // Because Discord does not automatically unmute someone after they've been given speaking rights.
        public async void UpdateVoiceChannel()
        {
            if (!_guild.LastHat.Roles.Contains(_guild.MutedRole)) return;
            var channel = _guild.LastHat.VoiceChannel;
            if (channel == null) return;
            await _guild.LastHat.ModifyAsync(x => x.Channel = _guild.AfkVoiceChannel);
            await _guild.LastHat.ModifyAsync(x => x.Channel = channel);
        }

        public async Task SendLindsey(string path)
        {
            // Get the audio channel
            var channel = _guild.LastHat.VoiceChannel;
            if (channel == null) return;
            var audioClient = await channel.ConnectAsync();

            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
        }

        public Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
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

        public void CheckHat()
        {
            if (!_guild.HatRole.Members.Any()) return;
            _guild.LastHat = _guild.HatRole.Members.Last();
            _guild.HatRole.Members.AsParallel().ForAll(
                async x =>
                {
                    if (x == _guild.LastHat) return;
                    await Log(string.Format("User id: {0}, Username: {1}", x.Id, x.Username));
                    await x.RemoveRoleAsync(_guild.HatRole);
                });
        }
    }
}
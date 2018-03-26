using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Thierry
{
    internal static class Program
    {
        public static readonly DiscordSocketClient Client = new DiscordSocketClient();
        private static string _token;
        private static Guild _guild;
        private static bool _setupDone;
        private static bool _idmode;
        private static IConfigurationRoot _config;

        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true);
            _config = builder.Build();

            if (args.Length != 0 && args[0] == "-id") _idmode = true;

            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            // Set up events.
            Client.Ready += Ready;
            Client.MessageReceived += MessageReceived;
            Client.GuildMemberUpdated += GuildMemberUpdated;

            _token = _config?["token"];
            await Client.StartAsync();
            await Client.LoginAsync(TokenType.Bot, _token);

            await Task.Delay(-1);
        }

        private static async Task Ready()
        {
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

            _setupDone = true;
        }

        private static async Task Log(string msg)
        {
            await Task.Run(() => { Console.WriteLine("[" + DateTime.Now + "]" + " " + msg); });
        }

        private static async Task MessageReceived(SocketMessage msg)
        {
            await Log(
                $"Channel id: {msg.Channel.Id}, Channel name: {msg.Channel.Name}, Author id: {msg.Author.Id}, Author: {msg.Author.Username}, Message: {msg.Content}");

            var user = _guild.GuildObject.GetUser(msg.Author.Id);

            if (user.Roles.Contains(_guild.HatminRole) && msg.Content.StartsWith("!givehat") &&
                msg.MentionedUsers.Count == 1)
            {
                if (!_setupDone)
                {
                    await msg.Channel.SendMessageAsync("I am performing initial setup. Ignoring command.");
                    var t = (msg.Channel as SocketGuildChannel)?.Guild;
                    return;
                }

                _guild.HatBeingMoved = true;
                if (_guild.LastHat != null)
                {
                    await _guild.LastHat.RemoveRoleAsync(_guild.HatRole);
                    UpdateVoiceChannel();
                }

                _guild.LastHat = _guild.GuildObject.GetUser(msg.MentionedUsers.First().Id);
                await _guild.LastHat.AddRoleAsync(_guild.HatRole);
                UpdateVoiceChannel();
            }
        }

        private static async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (!_setupDone) return;
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
        private static async void UpdateVoiceChannel()
        {
            if (!_guild.LastHat.Roles.Contains(_guild.MutedRole)) return;
            var channel = _guild.LastHat.VoiceChannel;
            if (channel == null) return;
            await _guild.LastHat.ModifyAsync(x => x.Channel = _guild.AfkVoiceChannel);
            await _guild.LastHat.ModifyAsync(x => x.Channel = channel);
        }

        private static async Task SendLindsay(string path)
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

        private static Process CreateStream(string path)
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

        private static async void GetIDs()
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

        private static void CheckHat()
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
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
        private static bool _ready;
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

        private static async Task Log(string msg)
        {
            await Task.Run(() => { Console.WriteLine("[" + DateTime.Now + "]" + " " + msg); });
        }

        private static async Task MessageReceived(SocketMessage msg)
        {
            // TODO: Add multi-guild support.

            // var guild = (msg.Channel as SocketGuildChannel)?.Guild;

            await Log(
                $"Channel id: {msg.Channel.Id}, Channel name: {msg.Channel.Name}, Author id: {msg.Author.Id}, Author: {msg.Author.Username}, Message: {msg.Content}");

            if (!msg.Content.StartsWith("!")) return;
            if (!_ready)
            {
                await msg.Channel.SendMessageAsync("I am performing initial setup. Ignoring command.");
                return;
            }

            var user = _guild.SocketGuild.GetUser(msg.Author.Id);
            var messageSplit = msg.Content.Split(' ');

            switch (messageSplit[0])
            {
                case "!givehat":
                {
                    if (!user.Roles.Contains(_guild.HatminRole))
                    {
                        Punish(msg.Author, msg.Channel);
                        break;
                    }

                    if (msg.MentionedUsers.Count == 1)
                        GiveHat(msg.MentionedUsers.First());
                    break;
                }
                case "!takehat":
                {
                    if (!user.Roles.Contains(_guild.HatminRole))
                    {
                        Punish(msg.Author, msg.Channel);
                        break;
                    }

                    if (_guild.SocketGuild.GetRole(_guild.HatRole.Id).Members.Any())
                    {
                        await PrintChannelMessage(msg.Channel,
                            string.Format("{0} The Lord giveth, and the Lord taketh away.", _guild.LastHat.Mention));
                        RemoveHat();
                    }

                    break;
                }
                case "!help":
                {
                    //print helptext
                    //json help file maken
                    const string message = "Ze heeft weeral gemorst, film het maar.";
                    await PrintChannelMessage(msg.Channel, message);
                    break;
                }
                case "!beepboop":
                {
                    const string message = "Ik ben een robot!";
                    await PrintChannelMessage(msg.Channel, message);
                    break;
                }
                case "!TRUT":
                {
                    const string message =
                        "Als er al spanningen zijn geweest, dan zijn die nu allemaal weg, daarmee hebben we die prijs gewonnen eh.";
                    await PrintChannelMessage(msg.Channel, message);
                    break;
                }
                case "!Thierry":
                {
                    if (msg.MentionedUsers.Count == 1)
                    {
                        var mention = msg.MentionedUsers.First();
                        await PrintChannelMessage(msg.Channel, string.Format("{0}! {0}! {0}!", mention.Mention));
                    }
                            break;
                }
                default:
                {
                    if (!user.Roles.Contains(_guild.HatminRole))
                        Punish(msg.Author, msg.Channel);
                    break;
                }
            }
        }

        private static async void Punish(SocketUser user, ISocketMessageChannel channel)
        {
            var message = string.Format("Maar allee {0}, wat doet gij nu? Precies ons Lindsey die bezig is!", user.Mention);
            await PrintChannelMessage(channel, message);
        }

        private static async void GiveHat(SocketUser user)
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

        private static void RemoveHat()
        {
            _guild.LastHat.RemoveRoleAsync(_guild.HatRole);
            _guild.LastHat = null;
        }

        private static async Task PrintChannelMessage(ISocketMessageChannel channel, string message)
        {
            await channel.SendMessageAsync(message);
        }

        private static async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
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
        private static async void UpdateVoiceChannel()
        {
            if (!_guild.LastHat.Roles.Contains(_guild.MutedRole)) return;
            var channel = _guild.LastHat.VoiceChannel;
            if (channel == null) return;
            await _guild.LastHat.ModifyAsync(x => x.Channel = _guild.AfkVoiceChannel);
            await _guild.LastHat.ModifyAsync(x => x.Channel = channel);
        }

        private static async Task SendLindsey(string path)
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
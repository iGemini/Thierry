using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Thierry.Preconditions;

namespace Thierry.Commands
{
    [Group("badwords")]
    public class BadWords : ModuleBase
    {
        [Command("add")]
        [RequireHatminRole]
        public async Task AddWord(string word)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == Context.Guild.Id);
            var words = g.BadWords;
            if (words.Contains(word)) return;
            words.Add(word);
            g.BadWords = words;
        }

        [Command("list")]
        [RequireHatminRole]
        public async Task List()
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == Context.Guild.Id);
            var embed = new EmbedBuilder {Title = $"Badwords list for {Context.Guild.Name}"};
            var pos = 1;

            var sb = new StringBuilder();
            foreach (var badWord in g.BadWords)
            {
                sb.Append($"{pos}: {badWord}");
                sb.Append(Environment.NewLine);
                pos++;
            }

            var list = sb.ToString();
            embed.AddField("List", list);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("remove")]
        [RequireHatminRole]
        public async Task RemoveWord(int entry)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == Context.Guild.Id);
            var words = g.BadWords;
            words.RemoveAt(entry - 1);
            g.BadWords = words;
        }

/*
        [Command("remove")]
        [RequireHatminRole]
        public async Task RemoveWords(int[] entries)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == Context.Guild.Id);
            var words = g.BadWords;

            foreach (var entry in entries) words.RemoveAt(entry - 1);

            g.BadWords = words;
        }
*/
        [Command("toggle")]
        [RequireHatminRole]
        public async Task Toggle()
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == Context.Guild.Id);
            g.BadWordsEnabled = !g.BadWordsEnabled;
        }
    }
}
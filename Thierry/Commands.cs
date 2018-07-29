using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Thierry
{
    public class Commands : ModuleBase
    {
        Program prog = new Program();

        [Command("TRUT")]
        public async Task Trut()
        {
            await ReplyAsync("Als er al spanningen zijn geweest, dan zijn die nu allemaal weg, daarmee hebben we die prijs gewonnen eh.");
        }

        [Command("beepboop")]
        public async Task Beepboop()
        {
            await ReplyAsync("Ik ben een robot!");
        }

        [Command("Thierry")]
        public async Task Thierry(SocketUser user)
        {
            var msg = Context.Message as SocketMessage;

            if (msg.MentionedUsers.Count == 1)
            {
                var mention = msg.MentionedUsers.First();
                await ReplyAsync(String.Format("{0}! {0}! {0}!", mention.Mention));
            }
        }

        [Command("givehat")]
        [RequireHatminRole()]
        public async Task GiveHat(SocketUser user)
        {
            var msg = Context.Message as SocketMessage;

            if (msg.MentionedUsers.Count == 1)
            {
                prog.GiveHat(msg.MentionedUsers.First());
            }
        }

        [Command("takehat")]
        [Alias("removehat")]
        [RequireHatminRole()]
        public async Task RemoveHat()
        {
            if (Program._guild.SocketGuild.GetRole(Program._guild.HatRole.Id).Members.Any())
            {
                await ReplyAsync(String.Format("{0} The Lord giveth, and the Lord taketh away.", Program._guild.LastHat.Mention));
                prog.RemoveHat();
            }
        }

        [Command("help")]
        public async Task Help()
        {
            //print helptext
            //json help file maken
            await ReplyAsync("Ze heeft weeral gemorst, film het maar.");
        }
    }
}

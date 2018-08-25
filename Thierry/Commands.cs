using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Thierry
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private static readonly List<HoekUser> Hoek = new List<HoekUser>();

        [Command("beepboop")]
        public async Task Beepboop()
        {
            await ReplyAsync("Ik ben een robot!");
        }

        [Command("givehat")]
        [RequireHatminRole]
        public async Task GiveHat(SocketGuildUser user)
        {
            await Program.Prog.GiveHat(user);
        }

        [Command("help")]
        public async Task Help()
        {
            //print helptext
            //json help file maken
            await ReplyAsync("Ze heeft weeral gemorst, film het maar.");
        }

        [Command("indenhoek")]
        [RequireHatminRole]
        public async Task InDenHoek(SocketGuildUser user)
        {
            foreach (var hoekUser in Hoek)
                if (hoekUser == user)
                {
                    await ReplyAsync($"{user.Mention} staat al in den hoek.");
                    return;
                }

            await user.AddRoleAsync(Program.Guild.MutedRole);
            Hoek.Add(new HoekUser(user));
            await ReplyAsync($"{user.Mention} staat nu in den hoek.");
        }

        [Command("lindsey")]
        public async Task Lindsey()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=v39Xh4hk8Jw");
        }

        [Command("takehat")]
        [Alias("removehat")]
        [RequireHatminRole]
        public async Task RemoveHat()
        {
            if (Program.Guild.SocketGuild.GetRole(Program.Guild.HatRole.Id).Members.Any())
            {
                await ReplyAsync($"{Program.Guild.LastHat.Mention} The Lord giveth, and the Lord taketh away.");
                Program.Prog.RemoveHat();
            }
        }

        [Command("Thierry")]
        public async Task Thierry(SocketGuildUser user)
        {
            await ReplyAsync($"{user.Mention}! {user.Mention}! {user.Mention}!");
        }

        [Command("TRUT")]
        public async Task Trut()
        {
            await ReplyAsync(
                "Als er al spanningen zijn geweest, dan zijn die nu allemaal weg, daarmee hebben we die prijs gewonnen eh.");
        }

        [Command("uitdenhoek")]
        [RequireHatminRole]
        public async Task UitDenHoek(SocketGuildUser user)
        {
            HoekUser temp = null;

            foreach (var hoekUser in Hoek)
                if (hoekUser == user)
                    temp = hoekUser;

            if (temp == null) return;
            temp.Votes++;

            if (temp.Votes >= 2)
            {
                await user.RemoveRoleAsync(Program.Guild.MutedRole);
                Hoek.Remove(temp);
                await ReplyAsync($"{temp.User.Mention} has been released.");
                return;
            }

            await ReplyAsync($"{temp.User.Mention} currently has {temp.Votes} votes to be released.");
        }
    }
}
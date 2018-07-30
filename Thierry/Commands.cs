using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Thierry
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly Program _prog = new Program();

        [Command("beepboop")]
        public async Task Beepboop()
        {
            await ReplyAsync("Ik ben een robot!");
        }

        [Command("givehat")]
        [RequireHatminRole]
        public async Task GiveHat(SocketGuildUser user)
        {
            await _prog.GiveHat(user);
        }

        [Command("help")]
        public async Task Help()
        {
            //print helptext
            //json help file maken
            await ReplyAsync("Ze heeft weeral gemorst, film het maar.");
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
                _prog.RemoveHat();
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
    }
}
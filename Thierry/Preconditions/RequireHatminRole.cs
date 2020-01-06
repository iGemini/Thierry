using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Thierry.Preconditions
{
    public class RequireHatminRole : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var g = Configuration.Config.Guilds.First(x => x.SocketGuildId == context.Guild.Id);
            var sender = ((SocketGuild) context.Guild).Users.First(x => x.Id == context.User.Id);

            if (sender.Roles.Contains(context.Guild.GetRole(g.HatminRoleId)))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError(
                $"Maar allee {sender.Mention}, wat doet gij nu? Precies ons Lindsey die bezig is!");
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Thierry
{
    public class RequireHatminRole : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var sender = Program.Guild.SocketGuild.GetUser(context.Message.Author.Id);
            if (sender.Roles.Contains(Program.Guild.HatminRole))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError(
                $"Maar allee {sender.Mention}, wat doet gij nu? Precies ons Lindsey die bezig is!");
        }
    }
}
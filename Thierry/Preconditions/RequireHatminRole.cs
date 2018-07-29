using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Linq;

namespace Thierry
{
    public class RequireHatminRole : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var sender = Program._guild.SocketGuild.GetUser(context.Message.Author.Id);
            if ((sender as SocketGuildUser).Roles.Contains(Program._guild.HatminRole))
            {
                return PreconditionResult.FromSuccess();
            }
            else
                return PreconditionResult.FromError(String.Format("Maar allee {0}, wat doet gij nu? Precies ons Lindsey die bezig is!", sender.Mention));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace Thierry
{
    public class Guild
    {
        public Guild(ulong guildId, ulong mutedRoleId, ulong hatRoleId, ulong hatminRoleId, ulong afkChannelId)
        {
            SocketGuild = Program.Client.GetGuild(guildId);
            MutedRole = SocketGuild.GetRole(mutedRoleId);
            HatRole = SocketGuild.GetRole(hatRoleId);
            HatminRole = SocketGuild.GetRole(hatminRoleId);
            AfkVoiceChannel = SocketGuild.GetVoiceChannel(afkChannelId);
        }

        public SocketGuild SocketGuild { get; }

        public SocketRole HatRole { get; }

        public SocketRole MutedRole { get; }

        public SocketRole HatminRole { get; }

        public SocketVoiceChannel AfkVoiceChannel { get; }

        public SocketGuildUser LastHat { get; set; }

        public bool HatBeingMoved { get; set; }
    }
}

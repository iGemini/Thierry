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
            GuildObject = Program.Client.GetGuild(guildId);
            MutedRole = GuildObject.GetRole(mutedRoleId);
            HatRole = GuildObject.GetRole(hatRoleId);
            HatminRole = GuildObject.GetRole(hatminRoleId);
            AfkVoiceChannel = GuildObject.GetVoiceChannel(afkChannelId);
        }

        public SocketGuild GuildObject { get; }

        public SocketRole HatRole { get; }

        public SocketRole MutedRole { get; }

        public SocketRole HatminRole { get; }

        public SocketVoiceChannel AfkVoiceChannel { get; }

        public SocketGuildUser LastHat { get; set; }

        public bool HatBeingMoved { get; set; }
    }
}

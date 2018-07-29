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

        public SocketVoiceChannel AfkVoiceChannel { get; }

        public bool HatBeingMoved { get; set; }

        public SocketRole HatminRole { get; }

        public SocketRole HatRole { get; }

        public SocketGuildUser LastHat { get; set; }

        public SocketRole MutedRole { get; }

        public SocketGuild SocketGuild { get; }
    }
}
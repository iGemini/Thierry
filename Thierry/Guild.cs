using System.Collections.Generic;

namespace Thierry
{
    public class Guild
    {
        public ulong AfkVoiceChannelId { get; set; }

        public List<string> BadWords { get; set; }

        public bool BadWordsEnabled { get; set; }

        public bool HatBeingMoved { get; set; }

        public ulong HatminRoleId { get; set; }

        public ulong HatRoleId { get; set; }

        public ulong LastHatId { get; set; }

        public ulong MutedRoleId { get; set; }

        public ulong SocketGuildId { get; set; }
    }
}
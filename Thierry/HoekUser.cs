using Discord.WebSocket;

namespace Thierry
{
    public class HoekUser
    {
        public HoekUser(SocketGuildUser user)
        {
            User = user;
            Votes = 0;
        }

        public SocketGuildUser User { get; set; }
        public int Votes { get; set; }

        public static bool operator ==(HoekUser hoekUser, SocketGuildUser user)
        {
            return hoekUser.User == user;
        }

        public static bool operator !=(HoekUser hoekUser, SocketGuildUser user)
        {
            return hoekUser.User != user;
        }

        public override bool Equals(object obj)
        {
            var temp = obj as SocketGuildUser;
            return User == temp;
        }
    }
}
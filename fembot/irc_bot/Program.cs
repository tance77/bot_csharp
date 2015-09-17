using System;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //var irc = new IrcClient("irc.twitch.tv", 6667, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            var irc = new IrcClient("192.16.64.152", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //var irc = new IrcClient("192.16.64.155", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //var irc = new IrcClient("192.16.64.51", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //"chat_servers": 
            //  "192.16.64.51:443",
            //  "192.16.64.155:443",
            //  "192.16.64.37:443",
            //  "192.16.64.144:443",
            //  "199.9.248.236:443",
            //  "192.16.64.146:443",
            //  "192.16.64.11:443",
            //  "192.16.70.169:443",
            //  "192.16.64.152:443",
            //  "192.16.64.45:443",
            //  "199.9.251.168:443"
            irc.JoinChannelStartup();
            //irc.JoinChannel("blackmarmalade");
            //irc.JoinChannel("liveegg");


            while (true)
            {
                string message = irc.ReadMessage();

                if (string.IsNullOrEmpty(message)) continue;
                irc.MessageHandler(message);
                Console.Write(message + "\r\n");
            }
        }
    }
}
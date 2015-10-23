using System;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var irc = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            var whisper_server = new IrcClient("192.16.64.212", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //var irc = new IrcClient("192.16.64.152", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
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
            //irc.JoinChannel("whitemarmalade");
            //irc.JoinChannel("liveegg");






            while (true)
            {
                var message = irc.ReadMessage();
                if (string.IsNullOrEmpty(message)) continue;

                var chatEvent = new TwitchChatEvent();
                var command = chatEvent.MessageHandler(message);

                if (command != null && command == "PRIVMSG")
                {
                    var chatHandler = new TwitchChatEventHandler(chatEvent, irc, whisper_server);
                    if (chatHandler.CheckSpam()) continue;
                    chatHandler.CheckCommands();
                }
            }
        }
    }
}
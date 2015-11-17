using System;
using System.IO;
using System.Threading;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ircServer = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle",false);
            var whisperServer = new IrcClient("192.16.64.212", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle", true);
            whisperServer.JoinChannel("blackmarmalade");
            ircServer.JoinChannelStartup();

            new Thread(whisperServer.WhisperReadMessage).Start();
            while (true)
            {
                var data = ircServer.ReadMessage();
                if (data == null)
                {
                    ircServer = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle",false);
                    ircServer.JoinChannelStartup();
                    //ircServer.JoinChannel("blackmarmalade");
                }

                if (string.IsNullOrEmpty(data)) continue;

                var twitchMessage = new TwitchMessage(data);
                var commandHandler = new IrcCommandHandler(twitchMessage, ircServer, whisperServer);
                commandHandler.Run();

            }
        }
    }
}

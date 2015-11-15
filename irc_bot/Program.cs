using System;
using System.IO;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ircServer = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            var whisperServer = new IrcClient("199.9.253.59", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            //ircServer.JoinChannel("whitemarmalade");
            ircServer.JoinChannelStartup();

            while (true)
            {
                var data = ircServer.ReadMessage();
                if (data == null)
                {
                    ircServer = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    whisperServer = new IrcClient("199.9.253.59", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    ircServer.JoinChannelStartup();
                }

                if (string.IsNullOrEmpty(data)) continue;

                var twitchMessage = new TwitchMessage(data);
                var commandHandler = new IrcCommandHandler(twitchMessage, ircServer, whisperServer);
                commandHandler.Run();




                // if (command != null && command == "PRIVMSG")
                // {
                // var handledMessage = new TwitchMessageHandler(twitchMessage, irc, whisper_server);
                // if (CheckSpam()) continue;
                // handledMessage.CheckCommands();
                // }
                // else if (command != null && command == "CLEARCHAT")
                // {
                //     var path = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Bad Phrases.txt";
                //     var goodPath = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Good Phrases.txt";
                //
                //     var a = irc.ChannelHistory;
                //     foreach(var person in a){
                //         Console.Write(twitchMessage.Msg.Split(':')[1] + "\r\n");
                //         if (person.GetSender() == twitchMessage.Msg.Split(':')[1])
                //         {
                //             string toBeWritten = ":" + person.GetSender();
                //             foreach (var dict in person.GetMessagesWithTimes())
                //             {
                //                 using (StreamWriter sw = File.AppendText(path))
                //                 {
                //                     sw.WriteLine(toBeWritten + " :" + dict.Value + " :" + dict.Key);
                //                 }
                //             }
                //         }
                //         else
                //         {
                //             string toBeWritten = ":" + person.GetSender();
                //             foreach (var dict in person.GetMessagesWithTimes())
                //             {
                //                 using (StreamWriter sw = File.AppendText(goodPath))
                //                 {
                //                     sw.WriteLine(toBeWritten + " :" + dict.Value + " :" + dict.Key);
                //                 }
                //             }
                //         }
                //     }
                //     irc.ChannelHistory.Clear();
                // }
            }
        }
    }
}

using System;
using System.IO;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var irc = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            var whisper_server = new IrcClient("192.16.64.212", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
            irc.JoinChannel("whitemarmalade");
            //irc.JoinChannelStartup();

            while (true)
            {
                var message = irc.ReadMessage();
                if (message == null)
                {
                    irc = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    whisper_server = new IrcClient("192.16.64.212", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    irc.JoinChannelStartup();
                }
                if (string.IsNullOrEmpty(message)) continue;

                var TwitchMessage = new TwitchMessage();
                var command = TwitchMessage.MessageHandler(message);

                if (command != null && command == "PRIVMSG")
                {
                    var HandledMessage = new TwitchMessageHandler(TwitchMessage, irc, whisper_server);
                    if (CheckSpam()) continue;
                    Message.CheckCommands();
                }
                else if (command != null && command == "CLEARCHAT")
                {
                    var path = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Bad Phrases.txt";
                    var goodPath = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Good Phrases.txt";

                    var a = irc.ChannelHistory;
                    foreach(var person in a){
                        Console.Write(chatEvent.Msg.Split(':')[1] + "\r\n");
                        if (person.GetSender() == chatEvent.Msg.Split(':')[1])
                        {
                            string toBeWritten = ":" + person.GetSender();
                            foreach (var dict in person.GetMessagesWithTimes())
                            {
                                using (StreamWriter sw = File.AppendText(path))
                                {
                                    sw.WriteLine(toBeWritten + " :" + dict.Value + " :" + dict.Key);
                                }
                            }
                        }
                        else
                        {
                            string toBeWritten = ":" + person.GetSender();
                            foreach (var dict in person.GetMessagesWithTimes())
                            {
                                using (StreamWriter sw = File.AppendText(goodPath))
                                {
                                    sw.WriteLine(toBeWritten + " :" + dict.Value + " :" + dict.Key);
                                }
                            }
                        }
                    }
                    irc.ChannelHistory.Clear();
                }
            }
        }
    }
}

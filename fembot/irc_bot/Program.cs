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
                if (message == null)
                {
                    irc = new IrcClient("irc.twitch.tv", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    whisper_server = new IrcClient("192.16.64.212", 443, "chinnbot", "oauth:88bwsy5w33ue5ogyj5g90m8qkpmvle");
                    irc.JoinChannelStartup();
                }
                if (string.IsNullOrEmpty(message)) continue;

                var chatEvent = new TwitchChatEvent();
                var command = chatEvent.MessageHandler(message);

                if (command != null && command == "PRIVMSG")
                {
                    var chatHandler = new TwitchChatEventHandler(chatEvent, irc, whisper_server);
                    if (chatHandler.CheckSpam()) continue;
                    chatHandler.CheckCommands();
                }
                else if (command != null && command == "CLEARCHAT")
                {
                    var path = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Bad Phrases.txt";
                    var goodPath = "C:\\Users\\Lance\\Documents\\GitHub\\bot_csharp\\fembot\\irc_bot\\Bad Phrases\\Good Phrases.txt";

                    var a = irc._ChannelHistory;
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
                    irc._ChannelHistory.Clear();
                }
            }
        }
    }
}
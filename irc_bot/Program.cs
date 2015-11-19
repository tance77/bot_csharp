using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
			ircServer.JoinChannel("blackmarmalade");
//            ircServer.JoinChannelStartup();
            var BlockingMessageQueue = new BlockingCollection<string>();
            var BlockingWhisperQueue = new BlockingCollection<string>();

<<<<<<< Updated upstream
			var whisperThread = new Thread (() => whisperServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue));
			whisperThread.Start ();

			ircServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue);
        
=======
<<<<<<< HEAD
            var whisperThread = new Thread (() => whisperServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue));
            whisperThread.Start ();

			Thread.Sleep (1000);

			ircServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue);

<<<<<<< HEAD
=======
=======
>>>>>>> parent of 581e2cf... Removed some comments in main



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
>>>>>>> master
>>>>>>> Stashed changes
        }
    }
}

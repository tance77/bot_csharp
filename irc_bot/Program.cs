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

            var whisperThread = new Thread (() => whisperServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue));
            whisperThread.Start ();

			Thread.Sleep (1000);

			ircServer.ReadMessage (ref BlockingMessageQueue, ref BlockingWhisperQueue);

        }
    }
}

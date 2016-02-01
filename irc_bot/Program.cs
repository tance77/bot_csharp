﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class Program
    {
        private static void Main (string[] args)
        {
            var jObj = JObject.Parse (File.ReadAllText (@"login.json"));
            var username = jObj.SelectToken ("bot_username").ToString ();
            var oAuth = jObj.SelectToken ("oauth").ToString ();

            const bool debug = true;
            var q = new BlockingCollection<string> ();
            var wq = new BlockingCollection<string> ();
            var ircServer = new IrcClient ("irc.twitch.tv", 443, username, oAuth, ref q, ref wq, false, debug);
            var whisperServer = new IrcClient ("192.16.64.212", 443, username, oAuth, ref q, ref wq, true, debug);
            if (debug) {
                ircServer.JoinChannel ("blackmarmalade");
            } else {
                ircServer.JoinChannelStartup ();
            }

            var whisperThread = new Thread (() => whisperServer.ReadMessage ());
            whisperThread.Start ();

            while (!whisperThread.IsAlive) {
                //spin for a bit till the thread starts
            }

            var ircThread = new Thread (() => ircServer.ReadMessage ());
            ircThread.Start ();

            while (!ircThread.IsAlive) {
                //spin for a bit till the thread starts
            }

            while (true) {
                if (!ircThread.IsAlive) {
                    ircServer = new IrcClient ("irc.twitch.tv", 443, username, oAuth, ref q, ref wq, false, debug);
                    if (debug) {
                        ircServer.JoinChannel ("blackmarmalade");
                    } else {
                        ircServer.JoinChannelStartup ();
                    }
                    ircThread = new Thread (() => ircServer.ReadMessage ());
                    ircThread.Start ();
                    while (!ircThread.IsAlive) {
                        Thread.Sleep (1000);
                        //spin for a bit till the thread starts
                    }
                }
                if (!whisperThread.IsAlive) {
                    whisperServer = new IrcClient ("192.16.64.212", 443, username, oAuth, ref q, ref wq, true, debug);
                    whisperThread = new Thread (() => whisperServer.ReadMessage ());
                    whisperThread.Start ();

                    while (!whisperThread.IsAlive) {
                        Thread.Sleep (1000);
                        //spin for a bit till the thread starts
                    }
                }
            }

        }
    }
}

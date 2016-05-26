using System;
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
            var youTubeApiKey = jObj.SelectToken ("youtube_api_key").ToString ();


            var debug = jObj.SelectToken ("debug").ToString () == "True";



            var q = new BlockingCollection<string> ();
            var wq = new BlockingCollection<string> ();
            var ircServer = new IrcClient ("irc.twitch.tv", 80, username, oAuth, ref q, ref wq, debug, youTubeApiKey);
            if (debug) {
                ircServer.JoinChannel ("blackmarmalade");
            } else {
                ircServer.JoinChannelStartup ();
            }

//            var ircThread = new Thread (() => ircServer.ReadMessage (/*PARAMS*/));
//            ircThread.Start ();

//            while (!ircThread.IsAlive) {
//                spin for a bit till the thread starts
//                Thread.Sleep (1000);
//            }

            while (true) {
//                if (!ircThread.IsAlive) {
//                    ircServer = new IrcClient ("irc.twitch.tv", 80, username, oAuth, ref q, ref wq, false, debug, youTubeApiKey);
//                if (debug) {
//                    ircServer.JoinChannel ("blackmarmalade");
//                } else {
//                    ircServer.JoinChannelStartup ();
//                }
                if (ircServer.ReadMessage () == "restart") {
                    ircServer = new IrcClient ("irc.twitch.tv", 80, username, oAuth, ref q, ref wq, debug, youTubeApiKey); 
                }
//                    ircThread = new Thread (() => ircServer.ReadMessage ());
//                    ircThread.Start ();
//                    while (!ircThread.IsAlive) {
//                        Thread.Sleep (1000);
                //spin for a bit till the thread starts
//                    }
            }
        }
    }
}


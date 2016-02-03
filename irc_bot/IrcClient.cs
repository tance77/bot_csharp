using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace twitch_irc_bot
{
    internal class IrcClient : WebFunctions
    {
        public string BotUserName{ get; set; }

        private StreamReader _inputStream;
        private StreamWriter _outputStream;
        private List<string> _listOfActiveChannels;
        private readonly CommandHelpers _commandHelpers = new CommandHelpers ();
        private readonly DatabaseFunctions _db = new DatabaseFunctions ();
        private readonly TwitchApi _twitchApi = new TwitchApi ();


        public List<MessageHistory> ChannelHistory { get; set; }

        public List<string> EmoteList { get; set; }

        public BlockingCollection<string> BlockingMessageQueue{ get; set; }

        public BlockingCollection<string> BlockingWhisperQueue{ get; set; }

        public int RateLimit { get; set; }

        public bool Running{ get; set; }

        public bool Debug { get; set; }

        public bool WhisperServer { get; set; }

       

        #region Constructors

        public IrcClient (string ip, int port, string userName, string oAuth, ref BlockingCollection<string> q, ref BlockingCollection<string> wq, bool whisperServer, bool debug)
        {
            RateLimit = 0;
            Debug = debug;

            WhisperServer = whisperServer;
            Running = true;
            EmoteList = GetGlobalEmotes ();
            _listOfActiveChannels = new List<string> ();
            BotUserName = userName;
            var tcpClient = new TcpClient (ip, port);
            _inputStream = new StreamReader (tcpClient.GetStream ());
            _outputStream = new StreamWriter (tcpClient.GetStream ()) { AutoFlush = true };

            _outputStream.WriteLine ("PASS " + oAuth);
            _outputStream.WriteLine ("NICK " + userName);
            _outputStream.WriteLine ("CAP REQ :twitch.tv/membership");
            _outputStream.WriteLine ("CAP REQ :twitch.tv/tags");
            _outputStream.WriteLine ("CAP REQ :twitch.tv/commands");
            ChannelHistory = new List<MessageHistory> ();
            BlockingMessageQueue = q;
            BlockingWhisperQueue = wq;

            GetGlobalEmotes ();
            


            #region Timers

            if (!WhisperServer) {
                if (!Debug) {
                    var channelUpdate = new Timer { Interval = 30000 };
                    //check if someone requested chinnbot to join channel every 30 secodns or leave
                    channelUpdate.Elapsed += UpdateActiveChannels;
                    channelUpdate.AutoReset = true;
                    channelUpdate.Enabled = true;

                }
                var followerTimer = new Timer { Interval = 30000 };
                followerTimer.Elapsed += AnnounceFollowers;
                followerTimer.AutoReset = true;
                followerTimer.Enabled = true;

                //var pointsTenTimer = new Timer { Interval = 600000 }; //1 coin every 10 minutes
                //pointsTenTimer.Elapsed += AddPointsTen;
                //pointsTenTimer.AutoReset = true;
                //pointsTenTimer.Enabled = true;
                
                //Timers for Timed Messages every 30,25,20,15,10, 5 minutes

                var advertiseThirtyTimer = new Timer { Interval = 1800000 };
                advertiseThirtyTimer.Elapsed += AdvertiseThirty;
                advertiseThirtyTimer.AutoReset = true;
                advertiseThirtyTimer.Enabled = true;


                var advertiseTwentyFive = new Timer { Interval = 1500000 };
                advertiseTwentyFive.Elapsed += AdvertiseTwentyFive;
                advertiseTwentyFive.AutoReset = true;
                advertiseTwentyFive.Enabled = true;

                var advertiseTwenty = new Timer { Interval = 1200000 };
                advertiseTwenty.Elapsed += AdvertiseTwenty;
                advertiseTwenty.AutoReset = true;
                advertiseTwenty.Enabled = true;

                var advertiseFifteen = new Timer { Interval = 900000 };
                advertiseFifteen.Elapsed += AdvertiseFifteen;
                advertiseFifteen.AutoReset = true;
                advertiseFifteen.Enabled = true;

                var advertiseTen = new Timer { Interval = 600000 };
                advertiseTen.Elapsed += AdvertiseTen;
                advertiseTen.AutoReset = true;
                advertiseTen.Enabled = true;

                var advertiseFive = new Timer { Interval = 300000 };
                advertiseFive.Elapsed += AdvertiseFive;
                advertiseFive.AutoReset = true;
                advertiseFive.Enabled = true;


            }

            var rateCheckTimer = new Timer { Interval = 1000 };
            rateCheckTimer.Elapsed += CheckRateAndSend;
            rateCheckTimer.AutoReset = true;
            rateCheckTimer.Enabled = true;

            var rateLimitTimer = new Timer { Interval = 30000 }; //100 messages every 30 seconds
            rateLimitTimer.Elapsed += ResetRateLimit;
            rateLimitTimer.AutoReset = true;
            rateLimitTimer.Enabled = true;

            #endregion

        }

        #endregion


        #region Timer_Inizialization

        public void CheckRateAndSend (Object source, ElapsedEventArgs e)
        {
            if (WhisperServer) {
                while (RateLimit < 100 && BlockingWhisperQueue.Count > 0) {
                    SendIrcMessage (BlockingWhisperQueue.Take ());
                    Thread.Sleep (400);
                }
            } else {
                while (RateLimit < 100 && BlockingMessageQueue.Count > 0) {
                    SendIrcMessage (BlockingMessageQueue.Take ());
                    Thread.Sleep (400);
                }
            }
        }

        private void AdvertiseThirty (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (30);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void AdvertiseTwentyFive (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (25);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void AdvertiseTwenty (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (20);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void AdvertiseFifteen (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (15);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void AdvertiseTen (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (10);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void AdvertiseFive (Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (5);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    if (!WhisperServer) {
                        AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                    }
                }
            }
        }

        private void ResetRateLimit (Object source, ElapsedEventArgs e)
        {
            RateLimit = 0;
        }


        public void AnnounceFollowers (Object source, ElapsedEventArgs e)
        {
            List<string> channelList = _db.GetListOfChannels ();
            if (channelList == null)
                return;
            foreach (string channel in channelList) {
                if (_db.GetAnnounceFollowerStatus (channel)) { //if announce followers is set to true.
                    string message = _commandHelpers.AssembleFollowerList (channel, _db, _twitchApi);
                    if (message != null) {
                        if (!WhisperServer) {
                            AddPrivMsgToQueue (message, channel);
                        }
                    }
                }
            }
        }

        public void AddPointsTen (Object source, ElapsedEventArgs e)
        {
            List<string> channelList = _db.GetListOfChannels ();
            if (channelList == null)
                return;
            foreach (string channel in channelList) {
                string response = _twitchApi.GetStreamUptime (channel);
                if (response == "Stream is offline." || response == "Could not reach Twitch API")
                    continue; //continue the loop
                List<string> userList = _twitchApi.GetActiveUsers (channel);
                _db.AddCoins (1, channel, userList);
            }
        }




        public void UpdateActiveChannels (Object source, ElapsedEventArgs e)
        {
            List<string> listOfDbChannels = _db.GetListOfActiveChannels ();
            if (listOfDbChannels == null)
                return;
            var listOfChannelsToRemove = new List<string> ();
            foreach (var channel in listOfDbChannels) {
                //If our active channels doesn't contain the channels in our DB we need to join that channel
                if (!_listOfActiveChannels.Contains (channel)) {
                    _commandHelpers.GetFollowers (channel, _db, _twitchApi);
                    JoinChannel (channel);
                }
            }
            foreach (var channel in _listOfActiveChannels) {
                //If our database doesn't contain a channel in the active channels list we need to part that channel
                if (!listOfDbChannels.Contains (channel)) {

                    if (!WhisperServer) {
                        AddPrivMsgToQueue ("Goodbye cruel world.", channel);
                        _outputStream.WriteLine ("PART #" + channel);
                        listOfChannelsToRemove.Add (channel);
                    }
                }
            }
            //Finally we need to remove the channels that we parted
            foreach (var channel in listOfChannelsToRemove) {
                _listOfActiveChannels.Remove (channel);
            }
        }

        #endregion

        #region Methods



        public void AddPrivMsgToQueue (string message, string fromChannel)
        {
            if (message == null) {
                return;
            }
            var colorList = new List<string>()
                {
                    "Blue",
                    "BlueViolet",
                    "CadetBlue",
                    "Chocolate",
                    "Coral",
                    "DodgerBlue",
                    "Firebrick",
                    "GoldenRod",
                    "Green",
                    "HotPink",
                    "OrangeRed",
                    "Red",
                    "SeaGreen",
                    "SpringGreen",
                    "YellowGreen"
                };
            var randRange = new Random((int)DateTime.Now.Ticks & (0x0000FFFF));
            int randOne = randRange.Next(1, colorList.Count);
            BlockingMessageQueue.Add(":" + BotUserName + "!" + BotUserName + "@"
            + BotUserName + ".tmi.twitch.tv PRIVMSG #" + "chinnbot" + " :" + "/color " + colorList[randOne]);
            BlockingMessageQueue.Add (":" + BotUserName + "!" + BotUserName + "@"
            + BotUserName + ".tmi.twitch.tv PRIVMSG #" + fromChannel + " :" + message);
        }



        public void AddLobbyPrivMsgToQueue (string message)
        {
            if (message == null) {
                return;
            }
            BlockingMessageQueue.Add (":" + BotUserName + "!" + BotUserName + "@"
            + BotUserName + ".tmi.twitch.tv PRIVMSG #chinnbot :" + message);
        }

        public void AddWhisperToQueue (string message, string messageSender)
        {
            if (message == null) {
                return;
            }
            BlockingWhisperQueue.Add ("PRIVMSG #jtv :/w " + messageSender + " " + message);
        }


        public void JoinChannel (string channel)
        {
            _outputStream.WriteLine ("JOIN #" + channel);
            _listOfActiveChannels.Add (channel);
        }


        public void JoinChannelStartup ()
        {
            Console.Write (
                "-------------------------------- Loading Channels to Join ------------------------------- \r\n");
            List<string> channelsToJoin = _db.JoinChannels ();
            foreach (string channel in channelsToJoin) {
                JoinChannel (channel);
                Console.Write ("Joining Channel " + channel + "\r\n");
            }
            Console.Write (
                "-------------------------------- Finished Loading Channels ------------------------------- \r\n");
        }

        public void PartChannel (string channel)
        {
            _outputStream.WriteLine ("PART #" + channel);
            _listOfActiveChannels.Remove (channel);
        }

        private void SendIrcMessage (string message)
        {
            RateLimit += 2;
            try {
                if (WhisperServer) {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                } else {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }

                Console.WriteLine (message);
                _outputStream.WriteLine (message);
                Console.ForegroundColor = ConsoleColor.White;
            } catch (IOException e) {
                Running = false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine (e);
                Console.ForegroundColor = ConsoleColor.White;
                BlockingMessageQueue.Add (message);
            }
        }






        public string ReadMessage ()
        {
            var q = BlockingMessageQueue;
            var wq = BlockingWhisperQueue;
            while (Running) {
                try {
                    var buf = _inputStream.ReadLine ();
                    if (buf == null)
                        continue;
                    if (!buf.StartsWith ("PING ")) { //If its not ping lets treat it as another message
                        if (WhisperServer) {
                            //Console.Write (buf + "\r\n");
                            continue;
                        }
                        Console.Write (buf + "\r\n");
                        var twitchMessage = new TwitchMessage (buf);
                        var handler = new IrcCommandHandler (twitchMessage, ref q, ref wq, this);
                        var restartStatus = handler.Run ();
                        if (restartStatus) {
                            Running = false;
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine ("****** FORCED RESTART ******");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        continue;

                    }
                    if (WhisperServer) {
                        Console.WriteLine ("Whisper Server");
                    }
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write (buf + "\r\n");
                    _outputStream.Write (buf.Replace ("PING", "PONG") + "\r\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write (buf.Replace ("PING", "PONG") + "\r\n");
                    _outputStream.Flush ();

                } catch (Exception e) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine (e);
                    Running = false;
                    Console.WriteLine ("************ Lost connection trying to reconnect. ************");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            return "restart";
        }


        #endregion

    }

}

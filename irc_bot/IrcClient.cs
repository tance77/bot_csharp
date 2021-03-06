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
        public string BotUserName { get; set; }

        private StreamReader _inputStream;
        private StreamWriter _outputStream;
        private List<string> _listOfActiveChannels;
        private readonly CommandHelpers _commandHelpers = new CommandHelpers ();
        private readonly DatabaseFunctions _db = new DatabaseFunctions ();
        private readonly TwitchApi _twitchApi = new TwitchApi ();


        public List<MessageHistory> ChannelHistory { get; set; }

        public List<string> EmoteList { get; set; }

        public List<string> PornLinks { get; set; }

        public BlockingCollection<string> BlockingMessageQueue { get; set; }

        public BlockingCollection<string> BlockingWhisperQueue { get; set; }

        public int RateLimit { get; set; }

        public bool Running { get; set; }

        public bool Debug { get; set; }
        public string YoutubeApiKey { get; set; }



        #region Constructors

        public IrcClient (string ip, int port, string userName, string oAuth, ref BlockingCollection<string> q, ref BlockingCollection<string> wq, bool debug, string youtubeApiKey)
        {
            RateLimit = 0;
            Debug = debug;

            YoutubeApiKey = youtubeApiKey;
            Running = true;
            EmoteList = GetGlobalEmotes ();
            PornLinks = new List<string> ();
            const string f = "pornlinks.txt";
            using (StreamReader r = new StreamReader (f)) {
                string line;
                while ((line = r.ReadLine ()) != null) {
                    PornLinks.Add (line);
                }
            }
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


            if (!Debug) {

                //var channelUpdateTimer = new System.Threading.Timer ((e) => {
                //    UpdateActiveChannels ();
                //}, null, 1000 * 30, Timeout.Infinite);
                //channelUpdateTimer.InitializeLifetimeService ();

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

            //var followerTimer = new System.Threading.Timer ((e) => {
            //    AnnounceFollowers ();
            //}, null, 1000 * 15, Timeout.Infinite);

            var pointsTenTimer = new Timer { Interval = 600000 }; //1 coin every 10 minutes
            pointsTenTimer.Elapsed += AddPointsTen;
            pointsTenTimer.AutoReset = true;
            pointsTenTimer.Enabled = true;

            //Timers for Timed Messages every 30,25,20,15,10, 5 minutes

            //var advertiseThirty = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (30);
            //}, null, 1000 * 60 * 30, Timeout.Infinite);

            //var advertiseTwentiyFive = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (25);
            //}, null, 1000 * 60 * 25, Timeout.Infinite);

            //var advertiseTwenty = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (20);
            //}, null, 1000 * 60 * 20, Timeout.Infinite);

            //var advertiseFifteen = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (15);
            //}, null, 1000 * 60 * 15, Timeout.Infinite);

            //var advertiseTen = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (10);
            //}, null, 1000 * 60 * 10, Timeout.Infinite);

            //var advertiseFive = new System.Threading.Timer ((e) => {
            //    AdvertiseTimer (5);
            //}, null, 1000 * 60 * 5, Timeout.Infinite);




            var advertiseThirtyTimer = new Timer { Interval = 1000 * 60 * 30 };
            advertiseThirtyTimer.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 30);
            advertiseThirtyTimer.AutoReset = true;
            advertiseThirtyTimer.Enabled = true;


            var advertiseTwentyFive = new Timer { Interval = 1000 * 60 * 25 };
            advertiseTwentyFive.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 25);
            advertiseTwentyFive.AutoReset = true;
            advertiseTwentyFive.Enabled = true;

            var advertiseTwenty = new Timer { Interval = 1000 * 60 * 20 };
            advertiseTwenty.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 20);
            advertiseTwenty.AutoReset = true;
            advertiseTwenty.Enabled = true;

            var advertiseFifteen = new Timer { Interval = 1000 * 60 * 15 };
            advertiseFifteen.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 15);
            advertiseFifteen.AutoReset = true;
            advertiseFifteen.Enabled = true;

            var advertiseTen = new Timer { Interval = 1000 * 60 * 10 };
            advertiseTen.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 10);
            advertiseTen.AutoReset = true;
            advertiseTen.Enabled = true;

            var advertiseFive = new Timer { Interval = 1000 * 60 * 5 };
            advertiseFive.Elapsed += (sender, e) => AdvertiseTimer (sender, e, 5);
            advertiseFive.AutoReset = true;
            advertiseFive.Enabled = true;


            //var rateCheckTimer = new System.Threading.Timer ((e) => {
            //kj`CheckRateAndSend ();
            //}, null, 0, 400);

            var rateCheckTimer = new Timer { Interval = 400 };
            rateCheckTimer.Elapsed += CheckRateAndSend;
            rateCheckTimer.AutoReset = true;
            rateCheckTimer.Enabled = true;

            //var rateLimitTimer = new System.Threading.Timer ((e) => {
            //    ResetRateLimit ();
            //}, null, 0, 30000);


            //followerTimer.InitializeLifetimeService ();
            //advertiseThirty.InitializeLifetimeService ();
            //advertiseTwentiyFive.InitializeLifetimeService ();
            //advertiseTwenty.InitializeLifetimeService ();
            //advertiseFifteen.InitializeLifetimeService ();
            //advertiseTen.InitializeLifetimeService ();
            //advertiseFive.InitializeLifetimeService ();
            //rateCheckTimer.InitializeLifetimeService ();
            //rateLimitTimer.InitializeLifetimeService ();

            var rateLimitTimer = new Timer { Interval = 30000 }; //100 messages every 30 seconds
            rateLimitTimer.Elapsed += ResetRateLimit;
            rateLimitTimer.AutoReset = true;
            rateLimitTimer.Enabled = true;

            #endregion

        }

        #endregion


        #region Timer_Inizialization

        public void CheckRateAndSend (Object source, ElapsedEventArgs e)
        //public void CheckRateAndSend ()
        {
            while (RateLimit < 18 && BlockingMessageQueue.Count > 0) {
                SendIrcMessage (BlockingMessageQueue.Take (), 0);
            }
            while (BlockingWhisperQueue.Count > 0) {
                SendIrcMessage (BlockingWhisperQueue.Take (), 1);
            }
        }

        private void AdvertiseTimer (Object source, ElapsedEventArgs e, int time)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimers (time);
            if (timmedMessagesDict == null)
                return;
            foreach (var item in timmedMessagesDict) {
                var r = new Random ();
                int randomMsg = r.Next (0, item.Value.Count);
                if (_twitchApi.StreamStatus (item.Key)) {
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
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
                    AddPrivMsgToQueue (item.Value [randomMsg], item.Key);
                }
            }
        }

        private void ResetRateLimit (Object source, ElapsedEventArgs e)
        //private void ResetRateLimit ()
        {
            RateLimit = 0;
        }


        public void AnnounceFollowers (Object source, ElapsedEventArgs e)
        //public void AnnounceFollowers ()
        {
            List<string> channelList = _db.GetListOfChannels ();
            if (channelList == null)
                return;
            foreach (string channel in channelList) {
                if (_db.GetAnnounceFollowerStatus (channel)) { //if announce followers is set to true.
                    string message = _commandHelpers.AssembleFollowerList (channel, _db, _twitchApi);
                    if (message != null) {
                        AddPrivMsgToQueue (message, channel);
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
        //public void UpdateActiveChannels ()
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

                    AddPrivMsgToQueue ("Goodbye cruel world.", channel);
                    _outputStream.WriteLine ("PART #" + channel);
                    listOfChannelsToRemove.Add (channel);
                }
            }
            _listOfActiveChannels = _db.GetListOfActiveChannels ();
        }

        #endregion

        #region Methods



        public void AddPrivMsgToQueue (string message, string fromChannel)
        {
            if (message == null) {
                return;
            }
            BlockingMessageQueue.Add (":" + BotUserName + "!" + BotUserName + "@"
            + BotUserName + ".tmi.twitch.tv PRIVMSG #" + fromChannel + " :" + message + "\r\n");
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

        private void SendIrcMessage (string message, int serverType)
        {
            Console.WriteLine (RateLimit);
            if (serverType == 0) {
                RateLimit += 1;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            } else {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            try {
                _outputStream.WriteLine (message);
                Console.WriteLine (message);
                Console.ForegroundColor = ConsoleColor.White;
                Thread.Sleep (400);
            } catch (IOException e) {
                WriteError (e);
                if (serverType == 1) {
                    BlockingWhisperQueue.Add (message);
                } else {
                    BlockingMessageQueue.Add (message);
                }
                //Running = false;
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
                        var twitchMessage = new TwitchMessage (buf);
                        if (twitchMessage.Command == "PRIVMSG") {
                            DateTime now = DateTime.Now;
                            Console.WriteLine ("[" + now + "] <" + twitchMessage.FromChannel + "> [" + twitchMessage.DisplayName + "]: " + twitchMessage.Msg);

                        } else if (twitchMessage.Command == "JOINERWITHMSG") {
                            AddPrivMsgToQueue (twitchMessage.Msg, twitchMessage.FromChannel);
                        } else {
                            //show everything if debug is on
                            if (Debug) {
                                Console.WriteLine (buf);
                            }
                        }
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
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine (buf);
                    _outputStream.WriteLine (buf.Replace ("PING", "PONG") + "\r\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine (buf.Replace ("PING", "PONG") + "\r\n");

                } catch (Exception e) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine (e);
                    Running = false;
                    Console.WriteLine ("************ Failed to Recieve Messages. Possible connection loss attempting to reconnect. ************");
                    Console.ForegroundColor = ConsoleColor.White;
                    return "restart";
                }
            }
            return "restart";
        }


        #endregion

    }

}

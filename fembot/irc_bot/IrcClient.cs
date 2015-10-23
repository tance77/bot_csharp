using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace twitch_irc_bot
{
    internal class IrcClient
    {
        private readonly string _botUserName;
        private readonly StreamReader _inputStream;
        private readonly StreamWriter _outputStream;
        private List<string> _listOfActiveChannels;
        private readonly List<Messages> _channelHistory;
        private readonly CommandFunctions _commandFunctions = new CommandFunctions();
        private readonly DatabaseFunctions _db = new DatabaseFunctions();
        private readonly RiotApi _riotApi;
        private readonly TwitchApi _twitchApi = new TwitchApi();
        private bool _debug = false;
        //private int _rateLimit;

        public List<Messages> _ChannelHistory { get; set; }

        #region Constructors

        public IrcClient(string ip, int port, string userName, string oAuth)
        {
            //_rateLimit = 0;
            //_riotApi = new RiotApi(_db);
            _listOfActiveChannels = new List<string>();
            _botUserName = userName;
            //_channelHistory = new List<Messages>();
            var tcpClient = new TcpClient(ip, port);
            _inputStream = new StreamReader(tcpClient.GetStream());
            _outputStream = new StreamWriter(tcpClient.GetStream());

            _outputStream.WriteLine("PASS " + oAuth);
            _outputStream.WriteLine("NICK " + userName);
            _outputStream.WriteLine("CAP REQ :twitch.tv/membership");
            _outputStream.WriteLine("CAP REQ :twitch.tv/tags");
            _outputStream.WriteLine("CAP REQ :twitch.tv/commands");
            //_rateLimit = 5;
            _outputStream.Flush();
            _ChannelHistory = new List<Messages>();


            #region Timers

            var channelUpdate = new Timer { Interval = 30000 }; //check if someone requested chinnbot to join channel every 30 secodns or leave
            channelUpdate.Elapsed += UpdateActiveChannels;
            channelUpdate.AutoReset = true;
            channelUpdate.Enabled = true;

            var followerTimer = new Timer { Interval = 30000 };
            followerTimer.Elapsed += AnnounceFollowers;
            followerTimer.AutoReset = true;
            followerTimer.Enabled = true;

            var pointsTenTimer = new Timer { Interval = 600000 }; //1 coin every 10 minutes
            pointsTenTimer.Elapsed += AddPointsTen;
            pointsTenTimer.AutoReset = true;
            pointsTenTimer.Enabled = true;

            //var rateLimitTimer = new Timer { Interval = 20000 }; //20 seconds for mod
            //rateLimitTimer.Elapsed += ResetRateLimit;
            //rateLimitTimer.AutoReset = false;
            //rateLimitTimer.Enabled = false;

            var advertiseTimer = new Timer { Interval = 900000 }; //900000 advertise timers in channels every 15 minutes
            advertiseTimer.Elapsed += Advertise;
            advertiseTimer.AutoReset = true;
            advertiseTimer.Enabled = true;
            #endregion
        }

        #endregion

        private void Advertise(Object source, ElapsedEventArgs e)
        {
            Dictionary<string, List<string>> timmedMessagesDict = _db.GetTimmedMessages();
            if (timmedMessagesDict == null) return;
            foreach (var item in timmedMessagesDict)
            {
                var r = new Random();
                int randomMsg = r.Next(0, item.Value.Count);
                if (_twitchApi.StreamStatus(item.Key))
                {
                    SendChatMessage(item.Value[randomMsg], item.Key);
                }
            }
        }

        //private void ResetRateLimit(Object source, ElapsedEventArgs e)
        //{
        //    _rateLimit = 0;
        //}

        public void AnnounceFollowers(Object source, ElapsedEventArgs e)
        {
            List<string> channelList = _db.GetListOfChannels();
            if (channelList == null) return;
            foreach (string channel in channelList)
            {
                string message = _commandFunctions.AssembleFollowerList(channel, _db, _twitchApi);
                if (message != null)
                {
                    SendChatMessage(message, channel);
                    Thread.Sleep(1000);
                }
            }
        }

        public void AddPointsTen(Object source, ElapsedEventArgs e)
        {
            List<string> channelList = _db.GetListOfChannels();
            if (channelList == null) return;
            foreach (string channel in channelList)
            {
                string response = _twitchApi.GetStreamUptime(channel);
                if (response == "Stream is offline." || response == "Could not reach Twitch API")
                    continue; //continue the loop
                List<string> userList = _twitchApi.GetActiveUsers(channel);
                _db.AddCoins(1, channel, userList);
            }
        }

        #region Methods

        public void UpdateActiveChannels(Object source, ElapsedEventArgs e)
        {
            List<string> listOfDbChannels = _db.GetListOfActiveChannels();
            if (listOfDbChannels == null) return;
            var listOfChannelsToRemove = new List<string>();
            foreach (var channel in listOfDbChannels)
            {
                //If our active channels doesn't contain the channels in our DB we need to join that channel
                if (!_listOfActiveChannels.Contains(channel))
                {
                    JoinChannel(channel);
                }
            }
            foreach (var channel in _listOfActiveChannels)
            {
                //If our database doesn't contain a channel in the active channels list we need to part that channel
                if (!listOfDbChannels.Contains(channel))
                {
                    SendChatMessage("Goodbye cruel world.", channel);
                    _outputStream.WriteLine("PART #" + channel);
                    _outputStream.Flush();
                    listOfChannelsToRemove.Add(channel);
                }
            }
            //Finally we need to remove the channels that we parted
            foreach (var channel in listOfChannelsToRemove)
            {
                _listOfActiveChannels.Remove(channel);
            }
        }

        public void JoinChannel(string channel)
        {
            _outputStream.WriteLine("JOIN #" + channel);
            _outputStream.Flush();
            _listOfActiveChannels.Add(channel);
        }


        public void JoinChannelStartup()
        {
            Console.Write(
                "-------------------------------- Loading Channels to Join ------------------------------- \r\n");
            List<string> channelsToJoin = _db.JoinChannels();
            foreach (string channel in channelsToJoin)
            {
                JoinChannel(channel);
                Console.Write("Joining Channel " + channel + "\r\n");
            }
            Console.Write(
                "-------------------------------- Finished Loading Channels ------------------------------- \r\n");
        }

        public void PartChannel(string channel)
        {
            _outputStream.WriteLine("PART #" + channel);
            _outputStream.Flush();
            _listOfActiveChannels.Remove(channel);
        }

        private void SendIrcMessage(string message)
        {
            _outputStream.WriteLine(message);
            _outputStream.Flush();
            //_rateLimit += 1;
        }

        public void SendChatMessageLobby(string message)
        {
            SendIrcMessage(":" + _botUserName + "!" + _botUserName + "@"
                           + _botUserName + ".tmi.twitch.tv PRIVMSG #chinnbot :" + message);
        }

        public void SendWhisper(string message, string channelName, string msgSender)
        {
            //SendIrcMessage(":" + _botUserName + "!" + _botUserName + "@"
            //+ _botUserName + ".tmi.twitch.tv WHISPER #" + _botUserName + " :" + message);
            SendIrcMessage("PRIVMSG #jtv :/w " + msgSender + " " + message);
        }

        public void SendChatMessage(string message, string channelName)
        {
            SendIrcMessage(":" + _botUserName + "!" + _botUserName + "@"
                           + _botUserName + ".tmi.twitch.tv PRIVMSG #" + channelName + " :" + message);
        }

        public string ReadMessage()
        {
            var buf = _inputStream.ReadLine();
            if (buf == null) return "";
            if (!buf.StartsWith("PING "))
            {
                _outputStream.Flush();
                Console.Write(buf + "\r\n");
                return buf;
            }
            Console.Write(buf + "\r\n");
            _outputStream.Write(buf.Replace("PING", "PONG") + "\r\n");
            Console.Write(buf.Replace("PING", "PONG") + "\r\n");
            _outputStream.Flush();
            return buf;
        }
    }
        #endregion
}
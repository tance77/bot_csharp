using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
		public string BotUserName{ get; set; }
		private readonly StreamReader _inputStream;
		private readonly StreamWriter _outputStream;
		private List<string> _listOfActiveChannels;
		private readonly CommandHelpers _commandHelpers = new CommandHelpers();
		private readonly DatabaseFunctions _db = new DatabaseFunctions();
		//        private readonly RiotApi _riotApi;
		private readonly TwitchApi _twitchApi = new TwitchApi();
		private bool _debug = true;
		public int RateLimit { get; set; }

		public List<MessageHistory> ChannelHistory { get; set; }


		public BlockingCollection<string> BlockingMessageQueue{ get; set; }
		public BlockingCollection<string> BlockingWhisperQueue{ get; set; }

		public bool WhisperServer { get; set; }

		#region Constructors

		public IrcClient(string ip, int port, string userName, string oAuth, bool a)
		{
			RateLimit = 0;

			WhisperServer = a;
			_listOfActiveChannels = new List<string>();
			BotUserName = userName;
			var tcpClient = new TcpClient(ip, port);
			_inputStream = new StreamReader(tcpClient.GetStream());
			_outputStream = new StreamWriter(tcpClient.GetStream()) {AutoFlush = true};

			_outputStream.WriteLine("PASS " + oAuth);
			_outputStream.WriteLine("NICK " + userName);
			_outputStream.WriteLine("CAP REQ :twitch.tv/membership");
			_outputStream.WriteLine("CAP REQ :twitch.tv/tags");
			_outputStream.WriteLine("CAP REQ :twitch.tv/commands");
			ChannelHistory = new List<MessageHistory>();
			BlockingMessageQueue = new BlockingCollection<string> ();
			BlockingWhisperQueue = new BlockingCollection<string> ();


			#region Timers

			if (!WhisperServer)
			{
				if (!_debug)
				{
					var channelUpdate = new Timer {Interval = 30000};
					//check if someone requested chinnbot to join channel every 30 secodns or leave
					channelUpdate.Elapsed += UpdateActiveChannels;
					channelUpdate.AutoReset = true;
					channelUpdate.Enabled = true;

				}
				var followerTimer = new Timer {Interval = 30000};
				followerTimer.Elapsed += AnnounceFollowers;
				followerTimer.AutoReset = true;
				followerTimer.Enabled = true;

				var pointsTenTimer = new Timer {Interval = 600000}; //1 coin every 10 minutes
				//var pointsTenTimer = new Timer { Interval = 20 }; //1 coin every 10 minutes
				pointsTenTimer.Elapsed += AddPointsTen;
				pointsTenTimer.AutoReset = true;
				pointsTenTimer.Enabled = true;

				var advertiseTimer = new Timer {Interval = 900000};
				//900000 advertise timers in channels every 15 minutes
				advertiseTimer.Elapsed += Advertise;
				advertiseTimer.AutoReset = true;
				advertiseTimer.Enabled = true;

			}

			var rateCheckTimer = new Timer { Interval = 1000 };
			rateCheckTimer.Elapsed += CheckRateAndSend;
			rateCheckTimer.AutoReset = true;
			rateCheckTimer.Enabled = true;

			var rateLimitTimer = new Timer { Interval = 30000 }; //20 messages every 30 seconds
			rateLimitTimer.Elapsed += ResetRateLimit;
			rateLimitTimer.AutoReset = true;
			rateLimitTimer.Enabled = true;

			#endregion
		}

		#endregion

		public void CheckRateAndSend(Object source, ElapsedEventArgs e)
		{
			//Console.Write("Rate Limit = " + RateLimit + " *********** \r\n");
			//Console.Write("Message Queue Size = " + MessageQueue.Count + " ~~~~~~ \r\n");
			if (WhisperServer) {
				while (RateLimit < 20 && BlockingWhisperQueue.Count > 0) {
						SendIrcMessage (BlockingWhisperQueue.Take ());
						Thread.Sleep (1000);
				}
			}
				else{
			while (RateLimit < 20 && BlockingMessageQueue.Count > 0)
			{
				SendIrcMessage(BlockingMessageQueue.Take());
				Thread.Sleep(1000);
			}
				}
		}

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
					if (!WhisperServer)
					{
						AddPrivMsgToQueue(item.Value[randomMsg], item.Key);
					}
				}
			}
		}

		private void ResetRateLimit(Object source, ElapsedEventArgs e)
		{
			RateLimit = 0;
		}


		public void AnnounceFollowers(Object source, ElapsedEventArgs e)
		{
			List<string> channelList = _db.GetListOfChannels();
			if (channelList == null) return;
			foreach (string channel in channelList)
			{
				string message = _commandHelpers.AssembleFollowerList(channel, _db, _twitchApi);
				if (message != null)
				{
					if (!WhisperServer)
					{

						AddPrivMsgToQueue(message, channel);
					}
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

					if (!WhisperServer)
					{
						AddPrivMsgToQueue("Goodbye cruel world.", channel);
						_outputStream.WriteLine("PART #" + channel);
						listOfChannelsToRemove.Add(channel);
					}
				}
			}
			//Finally we need to remove the channels that we parted
			foreach (var channel in listOfChannelsToRemove)
			{
				_listOfActiveChannels.Remove(channel);
			}
		}
		#region Methods

		public void AddPrivMsgToQueue(string message, string fromChannel)
		{
			if (message == null)
			{
				return;
			}
			BlockingMessageQueue.Add(":" + BotUserName + "!" + BotUserName + "@"
				+ BotUserName + ".tmi.twitch.tv PRIVMSG #" + fromChannel + " :" + message);
		}



		public void AddLobbyPrivMsgToQueue(string message)
		{
			if (message == null)
			{
				return;
			}
			BlockingMessageQueue.Add(":" + BotUserName + "!" + BotUserName + "@"
				+ BotUserName + ".tmi.twitch.tv PRIVMSG #chinnbot :" + message);
		}

		public void AddWhisperToQueue(string message, string messageSender)
		{
			if (message == null)
			{
				return;
			}
			BlockingWhisperQueue.Add("PRIVMSG #jtv :/w " + messageSender + " " + message);
		}


		public void JoinChannel(string channel)
		{
			_outputStream.WriteLine("JOIN #" + channel);
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
			_listOfActiveChannels.Remove(channel);
		}

		private void SendIrcMessage(string message)
		{
			RateLimit += 1;
			try
			{
				if(WhisperServer)
				{
					Console.ForegroundColor = ConsoleColor.Magenta;
				}
				else{
					Console.ForegroundColor = ConsoleColor.DarkYellow;
				}
				Console.WriteLine(message);
				_outputStream.WriteLine(message);
					Console.ForegroundColor = ConsoleColor.White;
			}
			catch (IOException e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e);
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("**************");
				BlockingMessageQueue.Add(message);
			}
		}






		public string ReadMessage(ref BlockingCollection<string> q, ref BlockingCollection<string> wq)
		{
			BlockingMessageQueue = q;
			BlockingWhisperQueue = wq;
			while (true) {
				try {
					var buf = _inputStream.ReadLine ();
					if (buf == null)
						continue;
					if (!buf.StartsWith ("PING ")) { //If its not ping lets treat it as another message
						if (WhisperServer) {
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.Write (buf + "\r\n");
						Console.ForegroundColor = ConsoleColor.White;
							continue;
						} else {
							Console.ForegroundColor = ConsoleColor.DarkBlue;
						Console.Write (buf + "\r\n");
							Console.ForegroundColor = ConsoleColor.White;
							var twitchMessage = new TwitchMessage (buf);
							new IrcCommandHandler (twitchMessage, ref q, ref wq, this);
							continue;

						}
					}
					if(WhisperServer){
						Console.WriteLine("Whisper Server");
					}
					Console.Write (buf + "\r\n");
					_outputStream.Write (buf.Replace ("PING", "PONG") + "\r\n");
					Console.Write (buf.Replace ("PING", "PONG") + "\r\n");
					_outputStream.Flush ();

				} catch (Exception e) {
					Console.WriteLine (e);

				}
			}
		}


		#endregion

	}

}

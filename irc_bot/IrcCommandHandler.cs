using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace twitch_irc_bot
{
    internal class IrcCommandHandler
    {
        private CommandHelpers _commandHelpers = new CommandHelpers ();
        private DatabaseFunctions _db = new DatabaseFunctions ();
        private RiotApi _riotApi;
        private TwitchApi _twitchApi = new TwitchApi ();

        #region Constructors

        public IrcCommandHandler (TwitchMessage m, ref BlockingCollection<string> q, ref BlockingCollection<string> wq, IrcClient c)
        {
            BlockingMessageQueue = q;
            BlockingWhisperQueue = wq;
            Irc = c;
            Message = m;
            _riotApi = new RiotApi (_db);
        }

        #endregion

        #region Getters/Setters

        public BlockingCollection<string> BlockingMessageQueue{ get; set; }

        public BlockingCollection<string> BlockingWhisperQueue{ get; set; }

        public TwitchMessage Message { get; set; }

        public IrcClient Irc { get; set; }


        #endregion

        #region Methods

        public bool Run ()
        {
            if (Message.Msg == null)
                return false;
            if (Message.Command == "PRIVMSG") {
                if (!CheckSpam ()) {
                    //If not spam
                    var response = CheckCommands ();
                    if (response == "restart") {
                        return true;
                    }
                }
            }
            if (Message.Command == "JOIN") {
                AddPrivMsgToQueue (Message.Msg, Message.FromChannel);
            }
            return false;
        }

        private void kill_user (string fromChannel, string msgSender, string userType)
        {
            if (userType == "mod")
                return;
            AddPrivMsgToQueue ("/timeout " + msgSender + " 4", fromChannel);
            AddPrivMsgToQueue ("/timeout " + msgSender + " 3", fromChannel);
            AddPrivMsgToQueue ("/timeout " + msgSender + " 2", fromChannel);
            AddPrivMsgToQueue ("/timeout " + msgSender + " 1", fromChannel);
            AddWhisperToQueue (
                " Your chat has been purged in " + fromChannel +
                "'s channel. Please keep your dirty secrets to yourself.", msgSender);
        }


        public bool MatchCommand (string message, string fromChannel, string sender)
        {
            if (!message.StartsWith ("!"))
                return true;
            Tuple<string, bool> commandFound = _db.MatchCommand (message, fromChannel);
            if (commandFound == null)
                return false;
            if (!commandFound.Item2) {
                AddPrivMsgToQueue (commandFound.Item1, fromChannel);
                return true;
            }
            AddPrivMsgToQueue (sender + ", " + commandFound.Item1, fromChannel);
            return true;
        }

        #region Spam Filters

        private bool CheckSpam (TwitchMessage msg)
        {
            if (Regex.Match (msg.Msg, @".*?I just got championship riven skin from here.*?").Success ||
                Regex.Match (msg.Msg, @".*?I just got championship riven skin code.*?").Success ||
                Regex.Match (msg.Msg, @".*?OMG I just won an Iphone 6.*?").Success ||
                Regex.Match (msg.Msg, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match (msg.Msg, @".*?[Rr]\.*[Aa]\.*[Ff]\.*[2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match (msg.Msg, @".*?v=IacCuPMkdXk.*?").Success ||
                Regex.Match (msg.Msg, @".*?articles4daily\.com.*?").Success ||
                Regex.Match (msg.Msg, @".*?com.*?.php\?.*?id.*?id.*?umk.*?").Success ||
                Regex.Match (msg.Msg,
                    @".*?[Gg][Rr][Ee][Yy].*?[Ww][Aa][Rr][Ww][Ii][Cc][Kk].*?[Mm][Ee][Dd][Ii][Ee][Vv][Aa][Ll].*?[Tt][Ww][Ii][Tt][Cc][Hh].*?[Aa][Nn][Dd].*?\d*.*?\d*.*?[Ii][]Pp].*?")
				.Success ||
                Regex.Match (msg.Msg, @"\$50 prepaid riot points from here").Success ||
                Regex.Match (msg.Msg,
                    @"I just got \$50 prepaid riot points from here its legit xD!!! http:\/\/getriotpointscodes\.com\/")
				.Success ||
                Regex.Match (msg.Msg, @"http:\/\/bit\.ly\/").Success ||
                Regex.Match (msg.Msg, @".*?ddns.*?").Success ||
                Regex.Match (msg.Msg, @".*?testmuk.*?").Success ||
                Regex.Match (msg.Msg, @".*?traffic\.php.*?").Success ||
                Regex.Match (msg.Msg, @".*?\/ow\.ly\/.*?").Success ||
                Regex.Match (msg.Msg, @".*?testmuk.*?").Success ||
                Regex.Match (msg.Msg, @".*?myvnc.*?").Success ||
                Regex.Match (msg.Msg, @".*?ulirate.*?").Success ||
                Regex.Match (msg.Msg, @".*?uslada\..*?").Success ||
                Regex.Match (msg.Msg, @".*?bounceme\..*?").Success ||
                Regex.Match (msg.Msg, @".*?serveblog\..*?").Success ||
                Regex.Match (msg.Msg, @".*?oeptmf\..*?").Success ||
                Regex.Match (msg.Msg, @".*?servebeer\..*?").Success) {
                if (msg.UserType == "mod")
                    return false; //your a mod no timeout
                AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                AddWhisperToQueue ("You have been banned from chatting in " +
                "'s channel. If you think you have been wrongly banned whisper a mod or message the channel owner.",
                    msg.MsgSender);
                AddPrivMsgToQueue ("/ban " + msg.MsgSender, msg.FromChannel);
                return true;
            }
            return false; //no spam in message
        }

        public bool CheckEmotes (TwitchMessage msg, IrcClient ircServer)
        {
            if (msg.UserType == "mod") {
                //Console.Write("I'm a mod**************************");
                return false;
            }
            if (_db.EmoteStatus (msg.FromChannel) || _db.RegularExist (msg.FromChannel, msg.MsgSender)) {
                return false;
            }
            var count = 0;
            var matchedEmoted = new Dictionary<string, int> ();
            var msgArr = msg.Msg.Split (' ');
            foreach (var m in msgArr) {
                if (ircServer.EmoteList.Contains (m)) {
                    count++;
                }
            }
            if (count > _db.CheckEmoteCount (msg.FromChannel)) {
                AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
                AddWhisperToQueue ("You are using to many emotes in your messages. [WARNING]", msg.MsgSender);
                AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
                return true;
            }
            return false;
        }

        public bool CheckAscii (TwitchMessage msg)
        {
            //Return True = Timeout
            if (msg.UserType == "mod") {
                //Console.Write("I'm a mod**************************");
                return false;
            }
            if (_db.AsciiStatus (msg.FromChannel)) {
                return false;
            }
            var count = 0;
            foreach (char c in msg.Msg) {
                if (c > 127) {
                    count++;
                }
            }
            //If the ascii count in this message was greater than the channel count time that user out
            if (count > _db.CheckAsciiCount (Message.FromChannel)) {
                AddPrivMsgToQueue (msg.MsgSender + ", [Warning]", msg.FromChannel);
                AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                AddWhisperToQueue ("You have been timed out for 2 minutes for excessive use of ascii symbols.", msg.MsgSender);
                return true;
            }
            return false;
        }

        public bool CheckUrls (TwitchMessage msg)
        {
            if (_db.UrlStatus (msg.FromChannel) || _db.RegularExist (msg.FromChannel, msg.MsgSender))
                return false;
            var msgParts = msg.Msg.Split (' ');
            var linkDetected = false;
            foreach (var a in msgParts) {
                if (Regex.IsMatch (a,
                        @"^(?!.{256})(?:[a-z0-9][a-z0-9-]{0,61}[a-z0-9]\.|[a-z0-9]\.)+(?:[a-z]{2}|AERO|ARPA|ASIA|BIZ|CAT|COM|COOP|EDU|GOV|INFO|INT|JOBS|MIL|MOBI|MUSEUM|NAME|NET|ORG|POST|PRO|TEL|TRAVEL|XXX)$",
                        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) {
                    linkDetected = true;
                    break;
                }
            }
            if (!linkDetected || msg.UserType == "mod")
                return false;
            if (_db.PermitExist (msg.FromChannel, msg.MsgSender) && _db.CheckPermitStatus (msg.FromChannel, msg.MsgSender)) {				//if they exist in permit and if permit has not expired//if it got here that means it was a url and they were permitted
                _db.RemovePermit (msg.FromChannel, msg.MsgSender);
                return false;
            }
            //Otherwise your not a mod or you are posting a link
            Thread.Sleep (400);
            AddPrivMsgToQueue (msg.MsgSender + ", you need permission before posting a link. [Warning]", msg.FromChannel);
            AddWhisperToQueue (
                "You can't post links in " + msg.FromChannel +
                "'s channel. You have been timed out for 10 seconds.", msg.MsgSender);
            AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            return true;
        }

        public bool CheckAccountCreation (TwitchMessage msg)
        {
            var result = _twitchApi.CheckAccountCreation (msg.MsgSender);
            //New Account less than a day old
            if (!result) {
                //if they exist in permit and if permit has not expired
                //if it got here that means it was a url and they were permitted
                if (_db.PermitExist (msg.FromChannel, msg.MsgSender) && _db.CheckPermitStatus (msg.FromChannel, msg.MsgSender)) {
                    _db.RemovePermit (msg.FromChannel, msg.MsgSender);
                    return true;
                }
                //match url
                var msgParts = msg.Msg.Split (' ');
                var linkDetected = false;
                foreach (var a in msgParts) {
                    if (Regex.IsMatch (a,
                            @"^(?!.{256})(?:[a-z0-9][a-z0-9-]{0,61}[a-z0-9]\.|[a-z0-9]\.)+(?:[a-z]{2}|AERO|ARPA|ASIA|BIZ|CAT|COM|COOP|EDU|GOV|INFO|INT|JOBS|MIL|MOBI|MUSEUM|NAME|NET|ORG|POST|PRO|TEL|TRAVEL|XXX)$",
                            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) {
                        linkDetected = true;
                        break;
                    }
                }
                if (linkDetected) {
                    AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    AddPrivMsgToQueue ("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    AddPrivMsgToQueue (msg.MsgSender + " You're account is to new to be posting links.",
                        msg.FromChannel);
                    return true;
                }
            }
            //yolo they are old enough
            return false;
        }

        #endregion


        public void AddPrivMsgToQueue (string message, string fromChannel)
        {
            if (message == null) {
                return;
            }
            BlockingMessageQueue.Add (":" + Irc.BotUserName + "!" + Irc.BotUserName + "@"
            + Irc.BotUserName + ".tmi.twitch.tv PRIVMSG #" + fromChannel + " :" + message);
        }

        public void AddLobbyPrivMsgToQueue (string message)
        {
            if (message == null) {
                return;
            }
            BlockingMessageQueue.Add (":" + Irc.BotUserName + "!" + Irc.BotUserName + "@"
            + Irc.BotUserName + ".tmi.twitch.tv PRIVMSG #chinnbot :" + message);
        }

        public void AddWhisperToQueue (string message, string messageSender)
        {
            if (message == null) {
                return;
            }
            BlockingWhisperQueue.Add ("PRIVMSG #jtv :/w " + messageSender + " " + message);
        }


        public void AddUserMessageHistory (string fromChannel, string msgSender, string msg)
        {
            var toBeDeleted = new List<MessageHistory> ();
            bool empty = true;

            foreach (MessageHistory msgContainer in Irc.ChannelHistory) {
                if (msgContainer.GetChannel () == fromChannel && msgContainer.GetSender () == msgSender &&
                    msgContainer.Count () < 20) {
                    msgContainer.AddMessage (msg);
                    empty = false;
                } else if (msgContainer.GetChannel () == fromChannel && msgContainer.GetSender () == msgSender &&
                           msgContainer.Count () >= 20) {
                    msgContainer.RemoveFirst ();
                    msgContainer.AddMessage (msg);
                    empty = false;
                }
                if (msgContainer.LastMessageTime ()) {
                    toBeDeleted.Add (msgContainer);
                }
            }

            if (empty) {
                Irc.ChannelHistory.Add (new MessageHistory (fromChannel, msgSender, msg));
            }

            foreach (MessageHistory item in toBeDeleted) {
                Irc.ChannelHistory.Remove (item);
            }
        }

        public bool CheckSpam ()
        {
            //Adds message to user message history
            AddUserMessageHistory (Message.FromChannel, Message.MsgSender, Message.Msg);

            if (CheckAscii (Message)) {
                return true;
            }
            if (CheckEmotes (Message, Irc)) {
                return true;
            }
            if (CheckAccountCreation (Message)) {
                return true;
            }
            if (CheckUrls (Message)) {
                return true;
            }
            if (CheckSpam (Message)) {
                return true;
            }
            return false;
        }

        public string CheckCommands ()
        {
            #region Chinnbot Only Commands

            if (Message.FromChannel == "chinnbot") {
                if (Regex.Match (Message.Msg, @"!join").Success) {
                    if (_db.AddToChannels (Message.MsgSender)) {
                        _commandHelpers.JoinAssembleFollowerList (Message.FromChannel, _db, _twitchApi);
                        AddLobbyPrivMsgToQueue ("Joining channel, " + Message.MsgSender +
                        ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                        Irc.JoinChannel (Message.MsgSender);
                    } else {
                        AddLobbyPrivMsgToQueue (Message.MsgSender +
                        ", I am already in your channel. Type !part if you wish me to leave your channel.");
                    }
                } else if (Regex.Match (Message.Msg, @"!part").Success) {
                    if (_db.RemoveFromChannels (Message.MsgSender)) {
                        Irc.PartChannel (Message.MsgSender);
                        AddLobbyPrivMsgToQueue (Message.MsgSender + ", I will no longer monitor your channel.");
                    } else {
                        AddLobbyPrivMsgToQueue (Message.MsgSender +
                        ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                    }
                }

                #endregion
            } else if (Message.MsgSender == "nightbot" || Message.MsgSender == "moobot" || Message.MsgSender == "xanbot") {
            }
			#region Built In Commands

			else {
                //Match Specific Channel Commands From Our Database
                if (!MatchCommand (Message.Msg, Message.FromChannel, Message.MsgSender)) {
                    //we found a user command in our database
                    return "";
                }

                //If we don't find a channel command check the built in commands

                if (Regex.Match (Message.Msg, @"!commands").Success) {
                    if (Message.FromChannel == "sophiabot")
                        return "";
                    AddPrivMsgToQueue (
                        Message.MsgSender +
                        ", the commands for this channel can be found here http://chinnbot.tv/commands?user=" +
                        Message.FromChannel, Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!masteries$").Success) {
                    AddPrivMsgToQueue (_commandHelpers.GetMasteries (Message.FromChannel, _riotApi),
                        Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!rank$").Success) {
                    string resoponse = _commandHelpers.GetLeagueRank (Message.FromChannel, Message.MsgSender, _db,
                                           _riotApi);
                    AddPrivMsgToQueue (resoponse, Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!runes$").Success) {
                    Dictionary<string, int> runeDictionary = _riotApi.GetRunes (Message.FromChannel);
                    if (runeDictionary != null) {
                        AddPrivMsgToQueue (_commandHelpers.ParseRuneDictionary (runeDictionary),
                            Message.FromChannel);
                    } else {
                        AddPrivMsgToQueue (
                            "Please set up your summoner name by typing !setsummoner [summonername]",
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!allowurls\son$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.UrlToggle (Message.FromChannel, true, _db),
                            Message.FromChannel);
                    } else {
                        AddPrivMsgToQueue ("Insufficient privileges", Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!allowurls\soff$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.UrlToggle (Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!dicksize$").Success) {
                    string response = _commandHelpers.DickSize (Message.FromChannel, Message.MsgSender, _db);
                    AddPrivMsgToQueue (response, Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!dicksize\son$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.DickSizeToggle (Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!dicksize\soff$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.DickSizeToggle (Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!gameq\son$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.QueueToggle (Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!gameq\soff$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.QueueToggle (Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"!addcom").Success) {
                    if (Message.FromChannel == "sophiabot")
                        return "";
                    if (Message.UserType == "mod") {
                        string response = _commandHelpers.AddCommand (Message.FromChannel, Message.Msg, _db);
                        if (response != null) {
                            AddPrivMsgToQueue (response, Message.FromChannel);
                        }
                    }
                } else if (Regex.Match (Message.Msg, @"!editcom").Success) {
                    if (Message.FromChannel == "sophiabot")
                        return "";
                    if (Message.UserType == "mod") {
                        string response = _commandHelpers.EditCommand (Message.FromChannel, Message.Msg, _db);
                        if (response != null) {
                            AddPrivMsgToQueue (response, Message.FromChannel);
                        }
                    }
                } else if (Regex.Match (Message.Msg, @"!removecom").Success || Regex.Match (Message.Msg, @"!delcom").Success) {
                    if (Message.FromChannel == "sophiabot")
                        return "";
                    if (Message.UserType == "mod") {
                        string response = _commandHelpers.RemoveCommand (Message.FromChannel, Message.Msg, _db);
                        if (response != null) {
                            AddPrivMsgToQueue (response, Message.FromChannel);
                        }
                    }
                } else if (Regex.Match (Message.Msg, @"^!roulette$").Success) {
                    if (_commandHelpers.Roulette (Message.FromChannel)) {
                        AddPrivMsgToQueue ("/timeout " + Message.MsgSender + " 300", Message.FromChannel);
                        AddWhisperToQueue (
                            " You have been killed. You can not speak for 5 minutes. To better simulate death the timeout has been increased from one minute to five minutes",
                            Message.MsgSender);
                        AddPrivMsgToQueue (Message.MsgSender + ", took a bullet to the head.",
                            Message.FromChannel);
                    } else {
                        AddPrivMsgToQueue (
                            Message.MsgSender + ", pulled the trigger and nothing happened.", Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"!setsummoner").Success) {
                    string summonerName = _commandHelpers.SplitSummonerName (Message.Msg);
                    AddPrivMsgToQueue (
                        _commandHelpers.SetSummonerName (Message.FromChannel, summonerName, Message.MsgSender, _db),
                        Message.FromChannel);
                    string summonerId = _riotApi.GetSummonerId (summonerName);
                    _db.SetSummonerId (Message.FromChannel, summonerId);
                } else if (Regex.Match (Message.Msg, @"^!suicide$").Success) {
                    kill_user (Message.FromChannel, Message.MsgSender, Message.UserType);
                } else if (Message.Msg == "gg") {
                    if (_commandHelpers.CheckGg (Message.FromChannel, _db)) {
                        AddPrivMsgToQueue ("GG", Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!gg\son$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.GgToggle (Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!gg\soff$").Success) {
                    if (Message.UserType == "mod") {
                        AddPrivMsgToQueue (_commandHelpers.GgToggle (Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                }
                //else if (
                //    Regex.Match(Message.Msg,
                //        @".*?[Ss][Hh][Oo][Ww].*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?")
                //    .Success ||
                //    Regex.Match(Message.Msg,
                //        @".*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?[Pp]+[Ll]+(([Ee]+[Aa]+[Ss]+[Ee]+)|([Zz]+)|([Ss]+)).*?")
                //    .Success)
                //{
                //    AddPrivMsgToQueue(
                //        Message.MsgSender + " here's  your boobs NSFW https://goo.gl/gGMasE", Message.FromChannel);
                //}
				else if (Regex.Match (Message.Msg, @"^!uptime$").Success) {
                    if (Message.FromChannel == "sophiabot")
                        return "";
                    AddPrivMsgToQueue (_twitchApi.GetStreamUptime (Message.FromChannel),
                        Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"!permit").Success) {
                    string response = _commandHelpers.PermitUser (Message.FromChannel, Message.MsgSender, Message.Msg,
                                          Message.UserType, _db);
                    if (response != null) {
                        AddPrivMsgToQueue (response, Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!roll$").Success) {
                    int diceRoll = _commandHelpers.DiceRoll ();
                    AddPrivMsgToQueue (Message.MsgSender + " rolled a " + diceRoll, Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!coinflip$").Success) {
                    bool coinFlip = _commandHelpers.CoinFlip ();
                    if (coinFlip) {
                        AddPrivMsgToQueue (Message.MsgSender + " flipped a coin and it came up heads",
                            Message.FromChannel);
                    } else {
                        {
                            AddPrivMsgToQueue (
                                Message.MsgSender + " flipped a coin and it came up tails", Message.FromChannel);
                        }
                    }
                } else if (Regex.Match (Message.Msg, @"^!regular\sadd\s").Success || Regex.Match (Message.Msg, @"!reg\sadd\s").Success) {
                    if (Message.UserType != "mod")
                        return "";
                    if (_db.GetRegularStatus (Message) == false)
                        return "";
                    _commandHelpers.AddRegular (Message, _db, Irc);
                } else if (Regex.Match (Message.Msg, @"^!regular\sdelete\s").Success || Regex.Match (Message.Msg, @"!reg\sdel\s").Success ||
                           Regex.Match (Message.Msg, @"^!regular\sremove\s").Success || Regex.Match (Message.Msg, @"^!reg\srem\s").Success) {
                    if (Message.UserType != "mod")
                        return "";
                    if (_db.GetRegularStatus (Message) == false)
                        return "";
                    _commandHelpers.RemoveRegular (Message, _db, Irc);
                }
				//else if (Regex.Match(message, @"!timer").Success)
				//{
				//    if (_commandHelpers.AddTimer(_db, message, FromChannel)) //Everything worked
				//    {
				//        AddMessageToMessageList("Timer was addedd succesffully", FromChannel);
				//    }
				//    else //faild to add timer
				//    {

				//        AddMessageToMessageList("Failed to add timer", FromChannel);
				//    }
				//}
				//else if (Regex.Match(message, @"^!mytimers$").Success)
				//{
				//    var toBeSent = _commandHelpers.ChannelTimers(_db, FromChannel);
				//    if (toBeSent != null)
				//    {
				//        AddMessageToMessageList(toBeSent, FromChannel);
				//    }
				//}

				//else if (Regex.Match(message, @"!addtimer").Success)
				//{
				//    var mytimer = _commandHelpers.AddTimer(FromChannel, message, 3, this);
				//    AddMessageToMessageList("Timer added", FromChannel);
				//    GC.KeepAlive(mytimer);
				//}
				else if (Regex.Match (Message.Msg, @"^!reg\son$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.RegToggle (Message, true, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!reg\soff$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.RegToggle (Message, false, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!sr\son$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.SongRequestToggle (Message.FromChannel, true, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!sr\soff$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.SongRequestToggle (Message.FromChannel, false, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!allowascii\son$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.AsciiToggle (Message.FromChannel, true, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!allowascii\soff$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.AsciiToggle (Message.FromChannel, false, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!allowemotes\son$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.EmoteToggle (Message.FromChannel, true, _db), Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!allowemotes\soff$").Success && Message.UserType == "mod") {
                    AddPrivMsgToQueue (
                        _commandHelpers.EmoteToggle (Message.FromChannel, false, _db), Message.FromChannel);
                } else if ((Regex.Match (Message.Msg, @"^!sr\s+").Success || Regex.Match (Message.Msg, @"^!songrequest\s+").Success) &&
                           _db.CheckSongRequestStatus (Message.FromChannel)) {
                    var response = _commandHelpers.SearchSong (_db, Message, BlockingMessageQueue, BlockingWhisperQueue);
                    if (response.Count == 0)
                        return ""; 
                    if (response.Count > 1) {
                        //Console.WriteLine(response.Count);
                        AddWhisperToQueue ("______________________________", Message.MsgSender);
                        AddWhisperToQueue ("Multiple results! What did you mean? Re-request with the track id.", Message.MsgSender);
                        foreach (var song in response) {
                            AddWhisperToQueue (song, Message.MsgSender);
                        }
                        return "";
                    }

                    Console.WriteLine (response.First ());
                    AddPrivMsgToQueue (response.First (), Message.FromChannel);
                } else if ((Regex.Match (Message.Msg, @"^!sr$").Success || Regex.Match (Message.Msg, @"^!songrequest$").Success) &&
                           _db.CheckSongRequestStatus (Message.FromChannel)) {
                    AddWhisperToQueue ("To request a song type !sr or !songrequest followed by the track ID." +
                    " If you do not know the track idea type !sr and the name of the song and I will give you a list of track IDs." +
                    " Alternatively you can type !msr followed by the song name and I will try to best match your song." +
                    " If you add the wrong song type !wrongsong or !ws to delete your last song.", Message.MsgSender);
                } else if (Regex.Match (Message.Msg, @"^!msr\s+").Success &&
                           _db.CheckSongRequestStatus (Message.FromChannel)) {
                    var response = _commandHelpers.MobileSearchSong (Message, _db);
                    if (!string.IsNullOrEmpty (response)) {
                        AddPrivMsgToQueue (Message.MsgSender + " " + response, Message.FromChannel);
                    } else {
                        AddPrivMsgToQueue (Message.MsgSender + "Song not Found", Message.FromChannel);
                    }
                } else if (Regex.Match (Message.Msg, @"^!currentsong$").Success) {
                    var song = _db.GetCurrentSong (Message.FromChannel);
                    AddPrivMsgToQueue (song, Message.FromChannel);
                } else if (Regex.Match (Message.Msg, @"^!wrongsong$").Success || Regex.Match (Message.Msg, @"^!ws$").Success && _db.CheckSongRequestStatus (Message.FromChannel)) {
                    var response = _commandHelpers.RemoveUserLastSong (_db, Message);
                    if (string.IsNullOrEmpty (response))
                        return "";
                    AddWhisperToQueue (response, Message.MsgSender);
                } else if ((Regex.Match (Message.Msg, @"^!songlist$").Success ||
                           Regex.Match (Message.Msg, @"^!sl$").Success ||
                           Regex.Match (Message.Msg, @"^!playlist$").Success)
                           && _db.CheckSongRequestStatus (Message.FromChannel)) {
                    AddPrivMsgToQueue (
                        Message.MsgSender +
                        " the playlist can be found here http://chinnbot.tv/songlist?user=" +
                        Message.FromChannel, Message.FromChannel);
                } else if ((Regex.Match (Message.Msg, @"^!queue$").Success || Regex.Match (Message.Msg, @"^!que$").Success) && _db.CheckQueueStatus (Message.FromChannel)) {
                    AddPrivMsgToQueue (Message.MsgSender + " to join the queue type !queue (summoner_name) . To see the current queue click this link http://chinnbot.tv/guest_player_queue?user=" + Message.FromChannel, Message.FromChannel);
                } else if ((Regex.Match (Message.Msg, @"^!queue\s").Success || Regex.Match (Message.Msg, @"^!que\s").Success) && _db.CheckQueueStatus (Message.FromChannel)) {
                    var queueStatus = _commandHelpers.AddToQueue (Message, _riotApi, _db);
                    if (queueStatus == null) {
                        AddWhisperToQueue ("Summoner name invalid.", Message.MsgSender);
                    } else {
                        AddPrivMsgToQueue (queueStatus, Message.FromChannel);
                    }
                } else if ((Regex.Match (Message.Msg, @"^!qleave$").Success || Regex.Match (Message.Msg, @"^!leaveq$").Success || Regex.Match (Message.Msg, @"^!queueleave$").Success || Regex.Match (Message.Msg, @"^!queleave$").Success) && _db.CheckQueueStatus (Message.FromChannel)) {
                    if (_db.RemovePersonFromQueue (Message.FromChannel, Message.MsgSender)) {
                        AddWhisperToQueue ("You have been taken out of the queue", Message.MsgSender);
                        return "";
                    }
                    AddWhisperToQueue ("You are not in the queue type \"!queue summoner_name\" to join the queue.", Message.MsgSender);
                } else if ((Regex.Match (Message.Msg, @"^!qpos$").Success || Regex.Match (Message.Msg, @"^!qpostion$").Success || Regex.Match (Message.Msg, @"^!qspot$").Success) && _db.CheckQueueStatus (Message.FromChannel)) {
                    var msgToBeSent = _commandHelpers.CheckPostion (Message, _db);
                    if (msgToBeSent != null) {
                        AddWhisperToQueue (msgToBeSent, Message.MsgSender);
                        return "";
                    }
                    AddWhisperToQueue ("You are not in the queue type \"!queue summoner_name\" to join the queue.", Message.MsgSender);

                } else if ((Regex.Match (Message.Msg, @"^!restart$").Success) && Message.MsgSender == "blackmarmalade") {
                    AddPrivMsgToQueue ("Restarting sit tight I will be right back.", Message.FromChannel);
                    return "restart";
                }

                //else if (Regex.Match(message, @"!addquote").Success)
                //{
                //    AddMessageToMessageList("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!quote").Success)
                //{
                //    AddMessageToMessageList("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!m8b").Success)
                //{
                //    AddMessageToMessageList("Temporaryily Unavailable.", FromChannel);
                //}
            }
            return "";

            #endregion
        }

        #endregion


    }


}

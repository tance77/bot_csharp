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
        private CommandHelpers _commandHelpers = new CommandHelpers();
        private DatabaseFunctions _db = new DatabaseFunctions();
        private RiotApi _riotApi;
        private TwitchApi _twitchApi = new TwitchApi();

        #region Constructors

        public IrcCommandHandler(TwitchMessage m, ref BlockingCollection<string> q, ref BlockingCollection<string> wq, IrcClient c)
        {
            Spotify = new Spotify();
            BlockingMessageQueue = q;
            BlockingWhisperQueue = wq;
            Irc = c;
            Message = m;
            _riotApi = new RiotApi(_db);
        }

        #endregion

        #region Getters/Setters

        public BlockingCollection<string> BlockingMessageQueue { get; set; }

        public BlockingCollection<string> BlockingWhisperQueue { get; set; }

        public Spotify Spotify { get; set; }

        public TwitchMessage Message { get; set; }

        public IrcClient Irc { get; set; }


        #endregion

        #region Methods

        public bool Run()
        {
            if (Message.Msg == null)
                return false;
            if (Message.Command == "PRIVMSG")
            {
                if (!CheckSpam())
                {
                    //If not spam
                    var response = CheckCommands();
                    if (response == "restart")
                    {
                        return true;
                    }
                }
            }
            if (Message.Command == "JOIN")
            {
                if (Message.Joiner == "luminexi")
                {
                    Irc.AddPrivMsgToQueue("Luminexi... you mean Lumisexi DatSheffy", Message.FromChannel);
                }
            }
            return false;
        }

        private void kill_user(string fromChannel, string msgSender, string userType)
        {
            if (userType == "mod")
                return;
            Irc.AddPrivMsgToQueue("/timeout " + msgSender + " 2", fromChannel);
            Irc.AddPrivMsgToQueue("/timeout " + msgSender + " 1", fromChannel);
        }


        public bool MatchCommand(string message, string fromChannel, string sender)
        {
            if (!message.StartsWith("!"))
                return true;
            Tuple<string, bool> commandFound = _db.MatchCommand(message, fromChannel);
            if (commandFound == null)
                return false;
            if (!commandFound.Item2)
            {
                Irc.AddPrivMsgToQueue(commandFound.Item1, fromChannel);
                return true;
            }
            Irc.AddPrivMsgToQueue(sender + ", " + commandFound.Item1, fromChannel);
            return true;
        }

        #region Spam Filters

        private bool CheckSpam(TwitchMessage msg)
        {
            if (Regex.Match(msg.Msg, @".*?I just got championship riven skin from here.*?").Success ||
                Regex.Match(msg.Msg, @".*?I just got championship riven skin code.*?").Success ||
                Regex.Match(msg.Msg, @".*?OMG I just won an Iphone 6.*?").Success ||
                Regex.Match(msg.Msg, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(msg.Msg, @".*?[Rr]\.*[Aa]\.*[Ff]\.*[2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(msg.Msg, @".*?v=IacCuPMkdXk.*?").Success ||
                Regex.Match(msg.Msg, @".*?articles4daily\.com.*?").Success ||
                Regex.Match(msg.Msg,
                    @".*?[Gg][Rr][Ee][Yy].*?[Ww][Aa][Rr][Ww][Ii][Cc][Kk].*?[Mm][Ee][Dd][Ii][Ee][Vv][Aa][Ll].*?[Tt][Ww][Ii][Tt][Cc][Hh].*?[Aa][Nn][Dd].*?\d*.*?\d*.*?[Ii][]Pp].*?")
                .Success ||
                Regex.Match(msg.Msg, @"\$50 prepaid riot points from here").Success ||
                Regex.Match(msg.Msg,
                    @"I just got \$50 prepaid riot points from here its legit xD!!! http:\/\/getriotpointscodes\.com\/")
                .Success ||
                Regex.Match(msg.Msg, @"http:\/\/bit\.ly\/").Success ||
                Regex.Match(msg.Msg, @".*?ddns.*?").Success ||
                Regex.Match(msg.Msg, @".*?testmuk.*?").Success ||
                Regex.Match(msg.Msg, @".*?traffic\.php.*?").Success ||
                Regex.Match(msg.Msg, @".*?\/ow\.ly\/.*?").Success ||
                Regex.Match(msg.Msg, @".*?testmuk.*?").Success ||
                Regex.Match(msg.Msg, @".*?myvnc.*?").Success ||
                Regex.Match(msg.Msg, @".*?ulirate.*?").Success ||
                Regex.Match(msg.Msg, @".*?uslada\..*?").Success ||
                Regex.Match(msg.Msg, @".*?bounceme\..*?").Success ||
                Regex.Match(msg.Msg, @".*?serveblog\..*?").Success ||
                Regex.Match(msg.Msg, @".*?oeptmf\..*?").Success ||
                Regex.Match(msg.Msg, @".*?servebeer\..*?").Success)
            {
                if (msg.UserType == "mod")
                    return false; //your a mod no timeout
                Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                Irc.AddWhisperToQueue("You have been banned from chatting in " +
                "'s channel. If you think you have been wrongly banned whisper a mod or message the channel owner.",
                    msg.MsgSender);
                Irc.AddPrivMsgToQueue("/ban " + msg.MsgSender, msg.FromChannel);
                return true;
            }
            return false; //no spam in message
        }

        public bool CheckEmotes(TwitchMessage msg, IrcClient ircServer)
        {
            if (msg.UserType == "mod" || msg.UserType == "1")
            {
                return false;
            }
            if (_db.EmoteStatus(msg.FromChannel) || _db.RegularExist(msg.FromChannel, msg.MsgSender))
            {
                return false;
            }
            var count = 0;
            var matchedEmoted = new Dictionary<string, int>();
            var msgArr = msg.Msg.Split(' ');
            foreach (var m in msgArr)
            {
                if (ircServer.EmoteList.Contains(m))
                {
                    count++;
                }
            }
            if (count > _db.CheckEmoteCount(msg.FromChannel))
            {
                Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
                Irc.AddWhisperToQueue("You are using to many emotes in your messages. [WARNING]", msg.MsgSender);
                Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
                return true;
            }
            return false;
        }

        public bool CheckAscii(TwitchMessage msg)
        {
            //Return True = Timeout
            if (msg.UserType == "mod" || msg.UserType == "1")
            {
                return false;
            }
            if (_db.AsciiStatus(msg.FromChannel))
            {
                return false;
            }
            var count = 0;
            foreach (char c in msg.Msg)
            {
                if (c > 127)
                {
                    count++;
                }
            }
            //If the ascii count in this message was greater than the channel count time that user out
            if (count > _db.CheckAsciiCount(Message.FromChannel))
            {
                Irc.AddPrivMsgToQueue(msg.MsgSender + ", [Warning]", msg.FromChannel);
                Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 120", msg.FromChannel);
                Irc.AddWhisperToQueue("You have been timed out for 2 minutes for excessive use of ascii symbols.", msg.MsgSender);
                return true;
            }
            return false;
        }

        public bool CheckUrls(TwitchMessage msg)
        {
            if (_db.UrlStatus(msg.FromChannel) || _db.RegularExist(msg.FromChannel, msg.MsgSender))
                return false;
            var msgParts = msg.Msg.Split(' ');
            var linkDetected = false;
            foreach (var a in msgParts)
            {
                if (Regex.IsMatch(a,
                        @"^(?!.{256})(?:[a-z0-9][a-z0-9-]{0,61}[a-z0-9]\.|[a-z0-9]\.)+(?:[a-z]{2}|AERO|ARPA|ASIA|BIZ|CAT|COM|COOP|EDU|GOV|INFO|INT|JOBS|MIL|MOBI|MUSEUM|NAME|NET|ORG|POST|PRO|TEL|TRAVEL|XXX)$",
                        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
                {
                    linkDetected = true;
                    break;
                }
            }
            if (!linkDetected || msg.UserType == "mod" || msg.UserType == "1")
                return false;
            if (_db.PermitExist(msg.FromChannel, msg.MsgSender) && _db.CheckPermitStatus(msg.FromChannel, msg.MsgSender))
            {				//if they exist in permit and if permit has not expired//if it got here that means it was a url and they were permitted
                _db.RemovePermit(msg.FromChannel, msg.MsgSender);
                return false;
            }
            //Otherwise your not a mod or you are posting a link
            Thread.Sleep(400);
            Irc.AddPrivMsgToQueue(msg.MsgSender + ", you need permission before posting a link. [Warning]", msg.FromChannel);
            Irc.AddWhisperToQueue(
                "You can't post links in " + msg.FromChannel +
                "'s channel. You have been timed out for 10 seconds.", msg.MsgSender);
            Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 10", msg.FromChannel);
            return true;
        }

        public bool CheckAccountCreation(TwitchMessage msg)
        {
            var result = _twitchApi.CheckAccountCreation(msg.MsgSender);
            //New Account less than a day old
            if (!result)
            {
                //if they exist in permit and if permit has not expired
                //if it got here that means it was a url and they were permitted
                if (_db.PermitExist(msg.FromChannel, msg.MsgSender) && _db.CheckPermitStatus(msg.FromChannel, msg.MsgSender))
                {
                    _db.RemovePermit(msg.FromChannel, msg.MsgSender);
                    return true;
                }
                //match url
                var msgParts = msg.Msg.Split(' ');
                var linkDetected = false;
                foreach (var a in msgParts)
                {
                    if (Regex.IsMatch(a,
                            @"^(?!.{256})(?:[a-z0-9][a-z0-9-]{0,61}[a-z0-9]\.|[a-z0-9]\.)+(?:[a-z]{2}|AERO|ARPA|ASIA|BIZ|CAT|COM|COOP|EDU|GOV|INFO|INT|JOBS|MIL|MOBI|MUSEUM|NAME|NET|ORG|POST|PRO|TEL|TRAVEL|XXX)$",
                            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
                    {
                        linkDetected = true;
                        break;
                    }
                }
                if (linkDetected)
                {
                    Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    Irc.AddPrivMsgToQueue("/timeout " + msg.MsgSender + " 1", msg.FromChannel);
                    Irc.AddPrivMsgToQueue(msg.MsgSender + " You're account is to new to be posting links.",
                        msg.FromChannel);
                    return true;
                }
            }
            //yolo they are old enough
            return false;
        }

        #endregion

        #region Message History
        public void AddUserMessageHistory(string fromChannel, string msgSender, string msg)
        {
            var toBeDeleted = new List<MessageHistory>();
            bool empty = true;

            foreach (MessageHistory msgContainer in Irc.ChannelHistory)
            {
                if (msgContainer.GetChannel() == fromChannel && msgContainer.GetSender() == msgSender &&
                    msgContainer.Count() < 20)
                {
                    msgContainer.AddMessage(msg);
                    empty = false;
                }
                else if (msgContainer.GetChannel() == fromChannel && msgContainer.GetSender() == msgSender &&
                         msgContainer.Count() >= 20)
                {
                    msgContainer.RemoveFirst();
                    msgContainer.AddMessage(msg);
                    empty = false;
                }
                if (msgContainer.LastMessageTime())
                {
                    toBeDeleted.Add(msgContainer);
                }
            }

            if (empty)
            {
                Irc.ChannelHistory.Add(new MessageHistory(fromChannel, msgSender, msg));
            }

            foreach (MessageHistory item in toBeDeleted)
            {
                Irc.ChannelHistory.Remove(item);
            }
        }
        #endregion

        public bool CheckSpam()
        {
            //Adds message to user message history
            AddUserMessageHistory(Message.FromChannel, Message.MsgSender, Message.Msg);

            if (CheckAscii(Message))
            {
                return true;
            }
            if (CheckEmotes(Message, Irc))
            {
                return true;
            }
            if (CheckAccountCreation(Message))
            {
                return true;
            }
            if (CheckUrls(Message))
            {
                return true;
            }
            if (CheckSpam(Message))
            {
                return true;
            }
            return false;
        }

        public string CheckCommands()
        {
            #region Chinnbot Only Commands

            if (Message.FromChannel == "chinnbot")
            {
                if (Regex.Match(Message.Msg, @"!join").Success)
                {
                    if (_db.AddToChannels(Message.MsgSender))
                    {
                        _commandHelpers.JoinAssembleFollowerList(Message.FromChannel, _db, _twitchApi);
                        Irc.AddLobbyPrivMsgToQueue("Joining channel, " + Message.MsgSender +
                                                   ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                        Irc.JoinChannel(Message.MsgSender);
                    }
                    else
                    {
                        Irc.AddLobbyPrivMsgToQueue(Message.MsgSender +
                                                   ", I am already in your channel. Type !part if you wish me to leave your channel.");
                    }
                }
                else if (Regex.Match(Message.Msg, @"!part").Success)
                {
                    if (_db.RemoveFromChannels(Message.MsgSender))
                    {
                        Irc.PartChannel(Message.MsgSender);
                        Irc.AddLobbyPrivMsgToQueue(Message.MsgSender + ", I will no longer monitor your channel.");
                    }
                    else
                    {
                        Irc.AddLobbyPrivMsgToQueue(Message.MsgSender +
                                                   ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                    }
                }
            }

            #endregion

            #region Other Bots
            else if (Message.MsgSender == "nightbot" || Message.MsgSender == "moobot" || Message.MsgSender == "xanbot")
            {
            }

            #endregion

            #region Built In Commands

            else
            {
                #region Database Commadns
                //Match Specific Channel Commands From Our Database
                if (!MatchCommand(Message.Msg, Message.FromChannel, Message.MsgSender))
                {
                    //we found a user command in our database
                    return "";
                }

                #endregion

                #region Mod Commands
                if ((Message.UserType == "mod" || Message.UserType == "1") && Message.FromChannel != "sophiabot")
                {
                    if (Regex.Match(Message.Msg, @"^!dicksize\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.DickSizeToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!dicksize\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.DickSizeToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!gameq\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.QueueToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!gameq\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.QueueToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!addcom").Success)
                    {
                        string response = _commandHelpers.AddCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddPrivMsgToQueue(response, Message.FromChannel);
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"^!editcom").Success)
                    {
                        string response = _commandHelpers.EditCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddPrivMsgToQueue(response, Message.FromChannel);
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"^!removecom").Success || Regex.Match(Message.Msg, @"!delcom").Success)
                    {
                        string response = _commandHelpers.RemoveCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddPrivMsgToQueue(response, Message.FromChannel);
                        }
                    }

                    else if (Regex.Match(Message.Msg, @"!permit").Success)
                    {
                        string response = _commandHelpers.PermitUser(Message.FromChannel, Message.MsgSender, Message.Msg,
                                              Message.UserType, _db);
                        if (response != null)
                        {
                            Irc.AddPrivMsgToQueue(response, Message.FromChannel);
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"^!regular\sadd\s").Success || Regex.Match(Message.Msg, @"!reg\sadd\s").Success)
                    {
                        if (_db.GetRegularStatus(Message) == false)
                            return "";
                        _commandHelpers.AddRegular(Message, _db, Irc);
                    }
                    else if (Regex.Match(Message.Msg, @"^!regular\sdelete\s").Success || Regex.Match(Message.Msg, @"!reg\sdel\s").Success ||
                             Regex.Match(Message.Msg, @"^!regular\sremove\s").Success || Regex.Match(Message.Msg, @"^!reg\srem\s").Success)
                    {
                        if (_db.GetRegularStatus(Message) == false)
                            return "";
                        _commandHelpers.RemoveRegular(Message, _db, Irc);
                    }
                    else if (Regex.Match(Message.Msg, @"^!links\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.UrlToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!links\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.UrlToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!gg\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.GgToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!gg\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.GgToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!reg\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.RegToggle(Message, true, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!reg\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.RegToggle(Message, false, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!sr\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.SongRequestToggle(Message.FromChannel, true, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!sr\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.SongRequestToggle(Message.FromChannel, false, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!ascii\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.AsciiToggle(Message.FromChannel, true, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!ascii\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.AsciiToggle(Message.FromChannel, false, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!emotes\son$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.EmoteToggle(Message.FromChannel, true, _db), Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!emotes\soff$").Success)
                    {
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.EmoteToggle(Message.FromChannel, false, _db), Message.FromChannel);
                    }
                }
                #endregion

                #region Non Mod Commands

                    //If we don't find a channel command check the built in commands

                    if (Regex.Match(Message.Msg, @"!commands").Success)
                    {
                        if (Message.FromChannel == "sophiabot")
                            return "";
                        Irc.AddPrivMsgToQueue(
                            Message.MsgSender +
                            ", the commands for this channel can be found here http://chinnbot.tv/commands?user=" +
                            Message.FromChannel, Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!masteries$").Success)
                    {
                        Irc.AddPrivMsgToQueue(_commandHelpers.GetMasteries(Message.FromChannel, _riotApi),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!rank$").Success)
                    {
                        string resoponse = _commandHelpers.GetLeagueRank(Message.FromChannel, Message.MsgSender, _db,
                            _riotApi);
                        Irc.AddPrivMsgToQueue(resoponse, Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!runes$").Success)
                    {
                        Dictionary<string, int> runeDictionary = _riotApi.GetRunes(Message.FromChannel);
                        if (runeDictionary != null)
                        {
                            Irc.AddPrivMsgToQueue(_commandHelpers.ParseRuneDictionary(runeDictionary),
                                Message.FromChannel);
                        }
                        else
                        {
                            Irc.AddPrivMsgToQueue(
                                "Please set up your summoner name by typing !setsummoner [summonername]",
                                Message.FromChannel);
                        }
                    }

                    else if (Regex.Match(Message.Msg, @"^!roulette$").Success)
                    {
                        if (_commandHelpers.Roulette(Message.FromChannel))
                        {
                            Irc.AddPrivMsgToQueue("/timeout " + Message.MsgSender + " 300", Message.FromChannel);
                            Irc.AddWhisperToQueue(
                                " You have been killed. You can not speak for 5 minutes. To better simulate death the timeout has been increased from one minute to five minutes",
                                Message.MsgSender);
                            Irc.AddPrivMsgToQueue(Message.MsgSender + ", took a bullet to the head.",
                                Message.FromChannel);
                        }
                        else
                        {
                            Irc.AddPrivMsgToQueue(
                                Message.MsgSender + ", pulled the trigger and nothing happened.", Message.FromChannel);
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"!setsummoner").Success)
                    {
                        string summonerName = _commandHelpers.SplitSummonerName(Message.Msg);
                        Irc.AddPrivMsgToQueue(
                            _commandHelpers.SetSummonerName(Message.FromChannel, summonerName, Message.MsgSender, _db),
                            Message.FromChannel);
                        string summonerId = _riotApi.GetSummonerId(summonerName);
                        _db.SetSummonerId(Message.FromChannel, summonerId);
                    }
                    else if (Regex.Match(Message.Msg, @"^!suicide$").Success)
                    {
                        kill_user(Message.FromChannel, Message.MsgSender, Message.UserType);
                    }
                    else if (Message.Msg == "gg")
                    {
                        if (_commandHelpers.CheckGg(Message.FromChannel, _db))
                        {
                            Irc.AddPrivMsgToQueue("GG", Message.FromChannel);
                        }
                    }

                    else if (Regex.Match(Message.Msg, @"^!uptime$").Success)
                    {
                        if (Message.FromChannel == "sophiabot")
                            return "";
                        Irc.AddPrivMsgToQueue(_twitchApi.GetStreamUptime(Message.FromChannel),
                            Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!dicksize$").Success)
                    {
                        string response = _commandHelpers.DickSize(Message.FromChannel, Message.MsgSender, _db);
                        Irc.AddPrivMsgToQueue(response, Message.FromChannel);
                    }

                    else if (Regex.Match(Message.Msg, @"^!roll$").Success)
                    {
                        int diceRoll = _commandHelpers.DiceRoll();
                        Irc.AddPrivMsgToQueue(Message.MsgSender + " rolled a " + diceRoll, Message.FromChannel);
                    }
                    else if (Regex.Match(Message.Msg, @"^!coinflip$").Success)
                    {
                        bool coinFlip = _commandHelpers.CoinFlip();
                        if (coinFlip)
                        {
                            Irc.AddPrivMsgToQueue(Message.MsgSender + " flipped a coin and it came up heads",
                                Message.FromChannel);
                        }
                        else
                        {
                            {
                                Irc.AddPrivMsgToQueue(
                                    Message.MsgSender + " flipped a coin and it came up tails", Message.FromChannel);
                            }
                        }
                    }

                    else if ((Regex.Match(Message.Msg, @"^!songrequest\s+").Success) &&
                             _db.CheckSongRequestStatus(Message.FromChannel))
                    {
                        if (_db.GetUsersSongCount(Message) >= 4)
                        {
                            var x = 4;
                            Irc.AddPrivMsgToQueue(
                                Message.MsgSender + ", you already have " + x + " songs in the queue.",
                                Message.FromChannel);
                        }
                        else
                        {
                            var response = Spotify.SearchSong(_db, Message, BlockingMessageQueue, BlockingWhisperQueue);
                            if (response.Count == 0)
                                return "";
                            if (response.Count > 1)
                            {
                                Irc.AddWhisperToQueue("______________________________", Message.MsgSender);
                                Irc.AddWhisperToQueue(
                                    "Multiple results! What did you mean? Re-request with the track id.",
                                    Message.MsgSender);
                                foreach (var song in response)
                                {
                                    Irc.AddWhisperToQueue(song, Message.MsgSender);
                                }
                                return "";
                            }
                            //Console.WriteLineWriteLine (response.First ());
                            Irc.AddPrivMsgToQueue(response.First(), Message.FromChannel);
                        }
                    }
                    else if ((Regex.Match(Message.Msg, @"^!sr$").Success ||
                              Regex.Match(Message.Msg, @"^!songrequest$").Success) &&
                             _db.CheckSongRequestStatus(Message.FromChannel))
                    {
                        Irc.AddWhisperToQueue("To request a song with Spotify type \"!songrequest TRACK ID\"." +
                                              " If you do not know the track idea type \"!songrequest TITLE\" and I will give you a list of track IDs." +
                                              " Alternatively you can type \"!sr TITLE \" and I will try to best match your song." +
                                              " If I add the wrong song type \"!wrongsong\" or \"!ws\" to delete your last song. " +
                                              "If at any time you would like to know what is playing type \"!currentsong.\"",
                            Message.MsgSender);
                    }
                    else if (Regex.Match(Message.Msg, @"^!sr\s+").Success &&
                             _db.CheckSongRequestStatus(Message.FromChannel))
                    {
                        if (_db.GetUsersSongCount(Message) >= 4)
                        {
                            var x = 4;
                          Irc.AddPrivMsgToQueue(Message.MsgSender + ", you already have " + x + " songs in the queue.", Message.FromChannel);
                        }
                        else
                        {
                            var response = Spotify.ImFeelingLuckySongSearch(Message, _db);
                            if (!string.IsNullOrEmpty(response))
                            {
                                Irc.AddPrivMsgToQueue(Message.MsgSender + " " + response, Message.FromChannel);
                            }
                            else
                            {
                                Irc.AddPrivMsgToQueue(Message.MsgSender + "Song not Found", Message.FromChannel);
                            }
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"^!currentsong$").Success)
                    {
                        var song = _db.GetCurrentSong(Message.FromChannel);
                        if (!string.IsNullOrEmpty(song))
                        {
                            Irc.AddPrivMsgToQueue(song, Message.FromChannel);
                        }
                    }
                    else if (Regex.Match(Message.Msg, @"^!wrongsong$").Success ||
                             Regex.Match(Message.Msg, @"^!ws$").Success &&
                             _db.CheckSongRequestStatus(Message.FromChannel))
                    {
                        var response = Spotify.RemoveUserLastSong(_db, Message);
                        if (string.IsNullOrEmpty(response))
                            return "";
                        Irc.AddWhisperToQueue(response, Message.MsgSender);
                    }
                    else if ((Regex.Match(Message.Msg, @"^!songlist$").Success ||
                              Regex.Match(Message.Msg, @"^!sl$").Success ||
                              Regex.Match(Message.Msg, @"^!playlist$").Success)
                             && _db.CheckSongRequestStatus(Message.FromChannel))
                    {
                        Irc.AddPrivMsgToQueue(
                            Message.MsgSender +
                            " the playlist can be found here http://chinnbot.tv/songlist?user=" +
                            Message.FromChannel, Message.FromChannel);
                    }
                    else if ((Regex.Match(Message.Msg, @"^!queue$").Success ||
                              Regex.Match(Message.Msg, @"^!que$").Success) &&
                             _db.CheckQueueStatus(Message.FromChannel))
                    {
                        Irc.AddPrivMsgToQueue(
                            Message.MsgSender +
                            " to join the queue type !queue (summoner_name) . To see the current queue click this link http://chinnbot.tv/guest_player_queue?user=" +
                            Message.FromChannel, Message.FromChannel);
                    }
                    else if ((Regex.Match(Message.Msg, @"^!queue\s").Success ||
                              Regex.Match(Message.Msg, @"^!que\s").Success) &&
                             _db.CheckQueueStatus(Message.FromChannel))
                    {
                        var queueStatus = _commandHelpers.AddToQueue(Message, _riotApi, _db);
                        if (queueStatus == null)
                        {
                            Irc.AddWhisperToQueue("Summoner name invalid.", Message.MsgSender);
                        }
                        else
                        {
                            Irc.AddPrivMsgToQueue(queueStatus, Message.FromChannel);
                        }
                    }
                    else if ((Regex.Match(Message.Msg, @"^!qleave$").Success ||
                              Regex.Match(Message.Msg, @"^!leaveq$").Success ||
                              Regex.Match(Message.Msg, @"^!queueleave$").Success ||
                              Regex.Match(Message.Msg, @"^!queleave$").Success) &&
                             _db.CheckQueueStatus(Message.FromChannel))
                    {
                        if (_db.RemovePersonFromQueue(Message.FromChannel, Message.MsgSender))
                        {
                            Irc.AddWhisperToQueue("You have been taken out of the queue",
                                Message.MsgSender);
                            return "";
                        }
                        Irc.AddWhisperToQueue(
                            "You are not in the queue type \"!queue summoner_name\" to join the queue.",
                            Message.MsgSender);
                    }
                    else if ((Regex.Match(Message.Msg, @"^!qpos$").Success ||
                              Regex.Match(Message.Msg, @"^!qpostion$").Success ||
                              Regex.Match(Message.Msg, @"^!qspot$").Success) &&
                             _db.CheckQueueStatus(Message.FromChannel))
                    {
                        var msgToBeSent = _commandHelpers.CheckPostion(Message, _db);
                        if (msgToBeSent != null)
                        {
                            Irc.AddWhisperToQueue(msgToBeSent, Message.MsgSender);
                            return "";
                        }
                        Irc.AddWhisperToQueue(
                            "You are not in the queue type \"!queue summoner_name\" to join the queue.",
                            Message.MsgSender);

                    }
                    else if ((Regex.Match(Message.Msg, @"^!restart$").Success) &&
                             Message.MsgSender == "blackmarmalade")
                    {
                        Irc.AddPrivMsgToQueue("Restarting sit tight I will be right back.",
                            Message.FromChannel);
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

                #endregion

            #endregion

            return "";
        }

        #endregion


    }


}

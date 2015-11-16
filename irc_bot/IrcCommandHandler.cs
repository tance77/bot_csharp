using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace twitch_irc_bot
{
    internal class IrcCommandHandler
    {
        private CommandHelpers _commandHelpers = new CommandHelpers();
        private DatabaseFunctions _db = new DatabaseFunctions();
        private RiotApi _riotApi;
        private TwitchApi _twitchApi = new TwitchApi();

        #region Constructors

        public IrcCommandHandler(TwitchMessage e, IrcClient irc, IrcClient whisper_server)
        {
            Irc = irc;
            WhisperServer = whisper_server;
            Message = e;
            _riotApi = new RiotApi(_db);
        }

        #endregion

        #region Getters/Setters

        public TwitchMessage Message { get; set; }

        public IrcClient Irc { get; set; }

        public IrcClient WhisperServer { get; set; }

        #endregion

        #region Methods

        public void Run()
        {
            if (Message.Msg == null) return;
            if (Message.Command == "PRIVMSG")
            {
                if (!CheckSpam())
                {
                    //If not spam
                    CheckCommands();
                }
            }
            if (Message.Command == "JOIN")
            {
                Irc.AddMessagesToMessageList(Message.Msg, Message.FromChannel);
            }
        }

        private void kill_user(string fromChannel, string msgSender, string userType)
        {
            if (userType == "mod") return;
            Irc.AddMessagesToMessageList("/timeout " + msgSender + " 4", fromChannel);
            Irc.AddMessagesToMessageList("/timeout " + msgSender + " 3", fromChannel);
            Irc.AddMessagesToMessageList("/timeout " + msgSender + " 2", fromChannel);
            Irc.AddMessagesToMessageList("/timeout " + msgSender + " 1", fromChannel);
            WhisperServer.AddWhisperToMessagesList(
                " Your chat has been purged in " + fromChannel +
                "'s channel. Please keep your dirty secrets to yourself.", fromChannel, msgSender);
        }


        public bool MatchCommand(string message, string fromChannel, string sender)
        {
            if (!message.StartsWith("!")) return true;
            Tuple<string, bool> commandFound = _db.MatchCommand(message, fromChannel);
            if (commandFound == null) return false;
            if (!commandFound.Item2)
            {
                Irc.AddMessagesToMessageList(commandFound.Item1, fromChannel);
                return true;
            }
            else
            {
                Irc.AddMessagesToMessageList(sender + ", " + commandFound.Item1, fromChannel);
                return true;
            }
        }

        #region Spam Filters

        private bool CheckSpam(string message, string fromChannel, string msgSender, string userType)
        {
            if (Regex.Match(message, @".*?I just got championship riven skin from here.*?").Success ||
                Regex.Match(message, @".*?I just got championship riven skin code.*?").Success ||
                Regex.Match(message, @".*?OMG I just won an Iphone 6.*?").Success ||
                Regex.Match(message, @".*?I am a 15 year old Rhinoceros.*?").Success ||
                Regex.Match(message, @".*?sexually Identify as*?").Success ||
                Regex.Match(message, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(message, @".*?[Rr]\.*[Aa]\.*[Ff]\.*[2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(message, @".*?v=IacCuPMkdXk.*?").Success ||
                Regex.Match(message, @".*?articles4daily\.com.*?").Success ||
                Regex.Match(message, @".*?com.*?.php\?.*?id.*?id.*?umk.*?").Success ||
                Regex.Match(message,
                    @".*?[Gg][Rr][Ee][Yy].*?[Ww][Aa][Rr][Ww][Ii][Cc][Kk].*?[Mm][Ee][Dd][Ii][Ee][Vv][Aa][Ll].*?[Tt][Ww][Ii][Tt][Cc][Hh].*?[Aa][Nn][Dd].*?\d*.*?\d*.*?[Ii][]Pp].*?")
                    .Success ||
                Regex.Match(message, @"\$50 prepaid riot points from here").Success ||
                Regex.Match(message,
                    @"I just got \$50 prepaid riot points from here its legit xD!!! http:\/\/getriotpointscodes\.com\/")
                    .Success ||
                Regex.Match(message, @"http:\/\/bit\.ly\/").Success ||
                Regex.Match(message, @".*?ddns.*?").Success ||
                Regex.Match(message, @".*?testmuk.*?").Success ||
                Regex.Match(message, @".*?traffic\.php.*?").Success ||
                Regex.Match(message, @".*?\/ow\.ly\/.*?").Success ||
                Regex.Match(message, @".*?testmuk.*?").Success ||
                Regex.Match(message, @".*?myvnc.*?").Success ||
                Regex.Match(message, @".*?ulirate.*?").Success ||
                Regex.Match(message, @".*?uslada\..*?").Success ||
                Regex.Match(message, @".*?bounceme\..*?").Success ||
                Regex.Match(message, @".*?serveblog\..*?").Success ||
                Regex.Match(message, @".*?oeptmf\..*?").Success ||
                Regex.Match(message, @".*?servebeer\..*?").Success
                )
            {
                if (userType == "mod") return false; //your a mod no timeout
                Thread.Sleep(400);
                Irc.AddMessagesToMessageList(msgSender + ", [Ban]", fromChannel);
                Irc.AddMessagesToMessageList("/timeout " + msgSender + " 120", fromChannel);
                WhisperServer.AddWhisperToMessagesList("You have been banned from chatting in " + fromChannel +
                                                       "'s channel. If you think you have been wrongly banned whisper a mod or message the channel owner.",
                    fromChannel, msgSender);
                Thread.Sleep(400);
                Irc.AddMessagesToMessageList("/timeout " + msgSender + " 120", fromChannel);
                Irc.AddMessagesToMessageList("/timeout " + msgSender + " 120", fromChannel);
                Thread.Sleep(400);
                Irc.AddMessagesToMessageList("/ban " + msgSender, fromChannel);
                return true;
            }
            return false; //no spam in message
        }

        public bool CheckUrls(string message, string fromChannel, string sender, string userType)
        {
            if (_db.UrlStatus(fromChannel)) return false;
            if (!Regex.Match(message, @"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/\/=]*)")
                .Success || userType == "mod") return false;
            if (_db.PermitExist(fromChannel, sender) && _db.CheckPermitStatus(fromChannel, sender))
            //if they exist in permit and if permit has not expired
            //if it got here that means it was a url and they were permitted
            {
                _db.RemovePermit(fromChannel, sender);
                return false;
            }
            //Otherwise your not a mod or you are posting a link
            Thread.Sleep(400);
            Irc.AddMessagesToMessageList(sender + ", you need permission before posting a link. [Warning]", fromChannel);
            WhisperServer.AddWhisperToMessagesList(
                "You can't post links in " + fromChannel +
                "'s channel. You have been timed out for 10 seconds.", fromChannel, sender);
            Irc.AddMessagesToMessageList("/timeout " + sender + " 10", fromChannel);
            Irc.AddMessagesToMessageList("/timeout " + sender + " 10", fromChannel);
            Irc.AddMessagesToMessageList("/timeout " + sender + " 10", fromChannel);
            return true;
        }

        public bool CheckAccountCreation(string msg, string fromChannel, string msgSender, string userType)
        {
            var result = _twitchApi.CheckAccountCreation(msgSender);
            //New Account less than a day old
            if (result)
            {
                //if they exist in permit and if permit has not expired
                //if it got here that means it was a url and they were permitted
                if (_db.PermitExist(fromChannel, msgSender) && _db.CheckPermitStatus(fromChannel, msgSender))
                {
                    _db.RemovePermit(fromChannel, msgSender);
                    return true;
                }
                //match url
                if (
                    Regex.Match(msg, @"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/\/=]*)")
                        .Success)
                {
                    Irc.AddMessagesToMessageList("/timeout " + msgSender + " 1", fromChannel);
                    Irc.AddMessagesToMessageList("/timeout " + msgSender + " 1", fromChannel);
                    Irc.AddMessagesToMessageList("/timeout " + msgSender + " 1", fromChannel);
                    Irc.AddMessagesToMessageList("/timeout " + msgSender + " 1", fromChannel);
                    Irc.AddMessagesToMessageList(msgSender + " You're account is to new to be posting links.",
                        fromChannel);
                    return true;
                }
            }
            //yolo they are old enough
            return false;
        }

        #endregion


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

        public bool CheckSpam()
        {
            //Adds message to user message history
            AddUserMessageHistory(Message.FromChannel, Message.MsgSender, Message.Msg);
            if (CheckAccountCreation(Message.Msg, Message.FromChannel, Message.MsgSender, Message.UserType))
            {
                return true;
            }
            if (CheckUrls(Message.Msg, Message.FromChannel, Message.MsgSender, Message.UserType))
            {
                return true;
            }
            if (CheckSpam(Message.Msg, Message.FromChannel, Message.MsgSender, Message.UserType))
            {
                return true;
            }
            return false;
        }

        public void CheckCommands()
        {
            #region Chinnbot Only Commands

            if (Message.FromChannel == "chinnbot")
            {
                if (Regex.Match(Message.Msg, @"!join").Success)
                {
                    if (_db.AddToChannels(Message.MsgSender))
                    {
                        _commandHelpers.JoinAssembleFollowerList(Message.FromChannel, _db, _twitchApi);
                        Irc.AddLobbyMessageToMessageList("Joining channel, " + Message.MsgSender +
                                                         ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                        Irc.JoinChannel(Message.MsgSender);
                    }
                    else
                    {
                        Irc.AddLobbyMessageToMessageList(Message.MsgSender +
                                                         ", I am already in your channel. Type !part if you wish me to leave your channel.");
                    }
                }
                else if (Regex.Match(Message.Msg, @"!part").Success)
                {
                    if (_db.RemoveFromChannels(Message.MsgSender))
                    {
                        Irc.PartChannel(Message.MsgSender);
                        Irc.AddLobbyMessageToMessageList(Message.MsgSender + ", I will no longer monitor your channel.");
                    }
                    else
                    {
                        Irc.AddLobbyMessageToMessageList(Message.MsgSender +
                                                         ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                    }
                }

            #endregion
            }
            else if (Message.MsgSender == "nightbot" || Message.MsgSender == "moobot" || Message.MsgSender == "xanbot")
            {
            }
            #region Built In Commands

            else
            {
                //Match Specific Channel Commands From Our Database
                if (!MatchCommand(Message.Msg, Message.FromChannel, Message.MsgSender))
                {
                    //we found a user command in our database
                    return;
                }

                //If we don't find a channel command check the built in commands

                if (Regex.Match(Message.Msg, @"!commands").Success)
                {
                    Irc.AddMessagesToMessageList(
                        Message.MsgSender +
                        ", the commands for this channel can be found here http://chinnbot.tv/commands?user=" +
                        Message.FromChannel, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!masteries$").Success)
                {
                    Irc.AddMessagesToMessageList(_commandHelpers.GetMasteries(Message.FromChannel, _riotApi),
                        Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!rank$").Success)
                {
                    string resoponse = _commandHelpers.GetLeagueRank(Message.FromChannel, Message.MsgSender, _db,
                        _riotApi);
                    Irc.AddMessagesToMessageList(resoponse, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!runes$").Success)
                {
                    Dictionary<string, int> runeDictionary = _riotApi.GetRunes(Message.FromChannel);
                    if (runeDictionary != null)
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.ParseRuneDictionary(runeDictionary),
                            Message.FromChannel);
                    }
                    else
                    {
                        Irc.AddMessagesToMessageList(
                            "Please set up your summoner name by typing !setsummoner [summonername]",
                            Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!allowurls\son$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.UrlToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                    else
                    {
                        Irc.AddMessagesToMessageList("Insufficient privileges", Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!allowurls\soff$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.UrlToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!dicksize$").Success)
                {
                    string response = _commandHelpers.DickSize(Message.FromChannel, Message.MsgSender, _db);
                    Irc.AddMessagesToMessageList(response, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!dicksize\son$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.DickSizeToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!dicksize\soff$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.DickSizeToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"!addcom").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        string response = _commandHelpers.AddCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddMessagesToMessageList(response, Message.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(Message.Msg, @"!editcom").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        string response = _commandHelpers.EditCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddMessagesToMessageList(response, Message.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(Message.Msg, @"!removecom").Success || Regex.Match(Message.Msg, @"!delcom").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        string response = _commandHelpers.RemoveCommand(Message.FromChannel, Message.Msg, _db);
                        if (response != null)
                        {
                            Irc.AddMessagesToMessageList(response, Message.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!roulette$").Success)
                {
                    if (_commandHelpers.Roulette(Message.FromChannel))
                    {
                        Irc.AddMessagesToMessageList("/timeout " + Message.MsgSender + " 300", Message.FromChannel);
                        WhisperServer.AddWhisperToMessagesList(
                            " You have been killed. You can not speak for 5 minutes. To better simulate death the timeout has been increased from one minute to five minutes",
                            Message.FromChannel, Message.MsgSender);
                        Irc.AddMessagesToMessageList(Message.MsgSender + ", took a bullet to the head.",
                            Message.FromChannel);
                    }
                    else
                    {
                        Irc.AddMessagesToMessageList(
                            Message.MsgSender + ", pulled the trigger and nothing happened.", Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"!setsummoner").Success)
                {
                    string summonerName = _commandHelpers.SplitSummonerName(Message.Msg);
                    Irc.AddMessagesToMessageList(
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
                        Irc.AddMessagesToMessageList("GG", Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!gg\son$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.GgToggle(Message.FromChannel, true, _db),
                            Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!gg\soff$").Success)
                {
                    if (Message.UserType == "mod")
                    {
                        Irc.AddMessagesToMessageList(_commandHelpers.GgToggle(Message.FromChannel, false, _db),
                            Message.FromChannel);
                    }
                }
                else if (
                    Regex.Match(Message.Msg,
                        @".*?[Ss][Hh][Oo][Ww].*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?")
                        .Success ||
                    Regex.Match(Message.Msg,
                        @".*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?[Pp]+[Ll]+(([Ee]+[Aa]+[Ss]+[Ee]+)|([Zz]+)|([Ss]+)).*?")
                        .Success)
                {
                    Irc.AddMessagesToMessageList(
                        Message.MsgSender + " here's  your boobs NSFW https://goo.gl/gGMasE", Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!uptime$").Success)
                {
                    Irc.AddMessagesToMessageList(_twitchApi.GetStreamUptime(Message.FromChannel),
                        Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"!permit").Success)
                {
                    string response = _commandHelpers.PermitUser(Message.FromChannel, Message.MsgSender, Message.Msg,
                        Message.UserType, _db);
                    if (response != null)
                    {
                        Irc.AddMessagesToMessageList(response, Message.FromChannel);
                    }
                }
                else if (Regex.Match(Message.Msg, @"^!roll$").Success)
                {
                    int diceRoll = _commandHelpers.DiceRoll();
                    Irc.AddMessagesToMessageList(Message.MsgSender + " rolled a " + diceRoll, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!coinflip$").Success)
                {
                    bool coinFlip = _commandHelpers.CoinFlip();
                    if (coinFlip)
                    {
                        Irc.AddMessagesToMessageList(Message.MsgSender + " flipped a coin and it came up heads",
                            Message.FromChannel);
                    }
                    else
                    {
                        {
                            Irc.AddMessagesToMessageList(
                                Message.MsgSender + " flipped a coin and it came up tails", Message.FromChannel);
                        }
                    }
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

                else if (Regex.Match(Message.Msg, @"!sron").Success && Message.UserType == "mod")
                {
                    Irc.AddMessagesToMessageList(
                        _commandHelpers.SongRequestToggle(Message.FromChannel, true, _db), Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"!sroff").Success && Message.UserType == "mod")
                {
                    Irc.AddMessagesToMessageList(
                        _commandHelpers.SongRequestToggle(Message.FromChannel, false, _db), Message.FromChannel);
                }

                else if (Regex.Match(Message.Msg, @"^!sr\s+").Success &&
                         _db.CheckSongRequestStatus(Message.FromChannel) != false)
                {
                    var response = _commandHelpers.SearchSong(Message.Msg, Message.MsgSender, _db,
                        Message.FromChannel);
                    response = Message.MsgSender + ", " + response;
                    WhisperServer.AddWhisperToMessagesList(response, Message.FromChannel, Message.MsgSender);
                    Console.WriteLine(response);
                    //Irc.AddMessagesToMessageList(response, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"^!currentsong$").Success)
                {
                    var song = _db.GetCurrentSong(Message.FromChannel);
                    Irc.AddMessagesToMessageList(song, Message.FromChannel);
                }
                else if (Regex.Match(Message.Msg, @"!songlist").Success ||
                         Regex.Match(Message.Msg, @"!sl").Success ||
                         Regex.Match(Message.Msg, @"!playlist").Success)
                {
                    Irc.AddMessagesToMessageList(
                        Message.MsgSender +
                        " the playlist can be found here http://chinnbot.tv/songlist?user=" +
                        Message.FromChannel, Message.FromChannel);
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
        }

        #endregion


    }
}

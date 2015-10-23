using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace twitch_irc_bot
{
    class TwitchChatEventHandler
    {
        private CommandFunctions _commandFunctions = new CommandFunctions();
        private DatabaseFunctions _db = new DatabaseFunctions();
        private RiotApi _riotApi;
        private TwitchApi _twitchApi = new TwitchApi();

        #region Constructors
        public TwitchChatEventHandler(TwitchChatEvent e, IrcClient irc, IrcClient whisper_server)
        {
            Irc = irc;
            WhisperServer = whisper_server;
            ChatEvent = e;           
            _riotApi = new RiotApi(_db);
        }
        #endregion

        #region Getters/Setters
        public TwitchChatEvent ChatEvent { get; set; }

        public IrcClient Irc { get; set; }

        public IrcClient WhisperServer { get; set; }

        #endregion

        #region Methods

        private void kill_user(string fromChannel, string msgSender, string userType)
        {
            if (userType == "mod") return;
            Irc.SendChatMessage("/timeout " + msgSender + " 4", fromChannel);
            Irc.SendChatMessage("/timeout " + msgSender + " 3", fromChannel);
            Irc.SendChatMessage("/timeout " + msgSender + " 2", fromChannel);
            Irc.SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
            WhisperServer.SendWhisper(
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
                Irc.SendChatMessage(commandFound.Item1, fromChannel);
                return true;
            }
            else
            {
                Irc.SendChatMessage(sender + ", " + commandFound.Item1, fromChannel);
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
                Regex.Match(message, @".*?ulirate.*?").Success
                )
            {
                if (userType == "mod") return false; //your a mod no timeout
                Thread.Sleep(400);
                Irc.SendChatMessage(msgSender + ", [Ban]", fromChannel);
                Irc.SendChatMessage("/timeout " + msgSender + " 120", fromChannel);
                WhisperServer.SendWhisper("You have been banned from chatting in " + fromChannel +
                    "'s channel. If you think you have been wrongly banned whisper a mod or message the channel owner.",
                    fromChannel, msgSender);
                Thread.Sleep(400);
                Irc.SendChatMessage("/timeout " + msgSender + " 120", fromChannel);
                Irc.SendChatMessage("/timeout " + msgSender + " 120", fromChannel);
                Thread.Sleep(400);
                Irc.SendChatMessage("/ban " + msgSender, fromChannel);
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
            Irc.SendChatMessage(sender + ", you need permission before posting a link. [Warning]", fromChannel);
            WhisperServer.SendWhisper(
                "You can't post links in " + fromChannel +
                "'s channel. You have been timed out for 10 seconds.", fromChannel, sender);
            Irc.SendChatMessage("/timeout " + sender + " 10", fromChannel);
            Irc.SendChatMessage("/timeout " + sender + " 10", fromChannel);
            Irc.SendChatMessage("/timeout " + sender + " 10", fromChannel);
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
                if (Regex.Match(msg, @"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/\/=]*)").Success)
                {
                    Irc.SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
                    Irc.SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
                    Irc.SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
                    Irc.SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
                    Irc.SendChatMessage(msgSender + " You're account is to new to be posting links.", fromChannel);
                    return true;
                }
            }
            //yolo they are old enough
            return false;
        }

        #endregion


        public void AddUserMessageHistory(string fromChannel, string msgSender, string msg)
        {
            var toBeDeleted = new List<Messages>();
            bool empty = true;

            foreach (Messages msgContainer in Irc._ChannelHistory)
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
                Irc._ChannelHistory.Add(new Messages(fromChannel, msgSender, msg));
            }

            foreach (Messages item in toBeDeleted)
            {
                Irc._ChannelHistory.Remove(item);
            }
        }

        public bool CheckSpam()
        {
            //Adds message to user message history
            AddUserMessageHistory(ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.Msg);
            if (CheckAccountCreation(ChatEvent.Msg, ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.UserType)) { return true; }
            if (CheckUrls(ChatEvent.Msg, ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.UserType)) { return true; }
            if (CheckSpam(ChatEvent.Msg, ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.UserType)) { return true;}
            return false;
        }

        public void CheckCommands()
        {
            #region Chinnbot Only Commands
            if (ChatEvent.FromChannel == "chinnbot")
            {
                if (Regex.Match(ChatEvent.Msg, @"!join").Success)
                {
                    if (_db.AddToChannels(ChatEvent.MsgSender))
                    {
                        _commandFunctions.JoinAssembleFollowerList(ChatEvent.FromChannel, _db, _twitchApi);
                        Irc.SendChatMessageLobby("Joining channel, " + ChatEvent.MsgSender +
                                             ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                        Irc.JoinChannel(ChatEvent.MsgSender);
                    }
                    else
                    {
                        Irc.SendChatMessageLobby(ChatEvent.MsgSender +
                                             ", I am already in your channel. Type !part if you wish me to leave your channel.");
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"!part").Success)
                {
                    if (_db.RemoveFromChannels(ChatEvent.MsgSender))
                    {
                        Irc.PartChannel(ChatEvent.MsgSender);
                        Irc.SendChatMessageLobby(ChatEvent.MsgSender + ", I will no longer monitor your channel.");
                    }
                    else
                    {
                        Irc.SendChatMessageLobby(ChatEvent.MsgSender +
                                             ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                    }
                }
            #endregion
            }
            else if (ChatEvent.MsgSender == "nightbot" || ChatEvent.MsgSender == "moobot" || ChatEvent.MsgSender == "xanbot")
            {
            }
            #region Built In Commands
            else
            {
                //Match Specific Channel Commands From Our Database
                if (!MatchCommand(ChatEvent.Msg, ChatEvent.FromChannel, ChatEvent.MsgSender))
                {
                    //we found a user command in our database
                    return;
                }

                //If we don't find a channel command check the built in commands

                if (Regex.Match(ChatEvent.Msg, @"!commands").Success)
                {
                    Irc.SendChatMessage(
                        ChatEvent.MsgSender +
                        ", the commands for this channel can be found here http://chinnbot.tv/commands?user=" +
                        ChatEvent.FromChannel, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!masteries$").Success)
                {
                    Irc.SendChatMessage(_commandFunctions.GetMasteries(ChatEvent.FromChannel, _riotApi), ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!rank$").Success)
                {
                    string resoponse = _commandFunctions.GetLeagueRank(ChatEvent.FromChannel, ChatEvent.MsgSender, _db, _riotApi);
                    Irc.SendChatMessage(resoponse, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!runes$").Success)
                {
                    Dictionary<string, int> runeDictionary = _riotApi.GetRunes(ChatEvent.FromChannel);
                    if (runeDictionary != null)
                    {
                        Irc.SendChatMessage(_commandFunctions.ParseRuneDictionary(runeDictionary), ChatEvent.FromChannel);
                    }
                    else
                    {
                        Irc.SendChatMessage("Please set up your summoner name by typing !setsummoner [summonername]",
                            ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!allowurls\son$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.UrlToggle(ChatEvent.FromChannel, true, _db), ChatEvent.FromChannel);
                    }
                    else
                    {
                        Irc.SendChatMessage("Insufficient privileges", ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!allowurls\soff$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.UrlToggle(ChatEvent.FromChannel, false, _db), ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!dicksize$").Success)
                {
                    string response = _commandFunctions.DickSize(ChatEvent.FromChannel, ChatEvent.MsgSender, _db);
                    Irc.SendChatMessage(response, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!dicksize\son$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.DickSizeToggle(ChatEvent.FromChannel, true, _db), ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!dicksize\soff$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.DickSizeToggle(ChatEvent.FromChannel, false, _db), ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"!addcom").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        string response = _commandFunctions.AddCommand(ChatEvent.FromChannel, ChatEvent.Msg, _db);
                        if (response != null)
                        {
                            Irc.SendChatMessage(response, ChatEvent.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"!editcom").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        string response = _commandFunctions.EditCommand(ChatEvent.FromChannel, ChatEvent.Msg, _db);
                        if (response != null)
                        {
                            Irc.SendChatMessage(response, ChatEvent.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"!removecom").Success || Regex.Match(ChatEvent.Msg, @"!delcom").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        string response = _commandFunctions.RemoveCommand(ChatEvent.FromChannel, ChatEvent.Msg, _db);
                        if (response != null)
                        {
                            Irc.SendChatMessage(response, ChatEvent.FromChannel);
                        }
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!roulette$").Success)
                {
                    if (_commandFunctions.Roulette(ChatEvent.FromChannel))
                    {
                        Thread.Sleep(400);
                        Irc.SendChatMessage("/timeout " + ChatEvent.MsgSender + " 60", ChatEvent.FromChannel);
                        WhisperServer.SendWhisper(" You have been killed. You can not speak for 1 minute.",
                            ChatEvent.FromChannel, ChatEvent.MsgSender);
                        Irc.SendChatMessage(ChatEvent.MsgSender + ", took a bullet to the head.", ChatEvent.FromChannel);
                    }
                    else
                    {
                        Irc.SendChatMessage(ChatEvent.MsgSender + ", pulled the trigger and nothing happened.", ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"!setsummoner").Success)
                {
                    string summonerName = _commandFunctions.SplitSummonerName(ChatEvent.Msg);
                    Irc.SendChatMessage(_commandFunctions.SetSummonerName(ChatEvent.FromChannel, summonerName, ChatEvent.MsgSender, _db),
                        ChatEvent.FromChannel);
                    string summonerId = _riotApi.GetSummonerId(summonerName);
                    _db.SetSummonerId(ChatEvent.FromChannel, summonerId);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!suicide$").Success)
                {
                    kill_user(ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.UserType);
                }

                else if (ChatEvent.Msg == "gg")
                {
                    if (_commandFunctions.CheckGg(ChatEvent.FromChannel, _db))
                    {
                        Irc.SendChatMessage("GG", ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!gg\son$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.GgToggle(ChatEvent.FromChannel, true, _db), ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!gg\soff$").Success)
                {
                    if (ChatEvent.UserType == "mod")
                    {
                        Irc.SendChatMessage(_commandFunctions.GgToggle(ChatEvent.FromChannel, false, _db), ChatEvent.FromChannel);
                    }
                }
                else if (
                    Regex.Match(ChatEvent.Msg,
                        @".*?[Ss][Hh][Oo][Ww].*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?")
                        .Success ||
                    Regex.Match(ChatEvent.Msg,
                        @".*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?[Pp]+[Ll]+(([Ee]+[Aa]+[Ss]+[Ee]+)|([Zz]+)|([Ss]+)).*?")
                        .Success)
                {
                    Irc.SendChatMessage(ChatEvent.MsgSender + " here's  your boobs NSFW https://goo.gl/BNl3Gl", ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!uptime$").Success)
                {
                    Irc.SendChatMessage(_twitchApi.GetStreamUptime(ChatEvent.FromChannel), ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"!permit").Success)
                {
                    string response = _commandFunctions.PermitUser(ChatEvent.FromChannel, ChatEvent.MsgSender, ChatEvent.Msg, ChatEvent.UserType, _db);
                    if (response != null)
                    {
                        Irc.SendChatMessage(response, ChatEvent.FromChannel);
                    }
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!roll$").Success)
                {
                    int diceRoll = _commandFunctions.DiceRoll();
                    Irc.SendChatMessage(ChatEvent.MsgSender + " rolled a " + diceRoll, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!coinflip$").Success)
                {
                    bool coinFlip = _commandFunctions.CoinFlip();
                    if (coinFlip)
                    {
                        Irc.SendChatMessage(ChatEvent.MsgSender + " flipped a coin and it came up heads", ChatEvent.FromChannel);
                    }
                    else
                    {
                        Irc.SendChatMessage(ChatEvent.MsgSender + " flipped a coin and it came up tails", ChatEvent.FromChannel);
                    }
                }
                //else if (Regex.Match(message, @"!timer").Success)
                //{
                //    if (_commandFunctions.AddTimer(_db, message, FromChannel)) //Everything worked
                //    {
                //        SendChatMessage("Timer was addedd succesffully", FromChannel);
                //    }
                //    else //faild to add timer
                //    {

                //        SendChatMessage("Failed to add timer", FromChannel);
                //    }
                //}
                //else if (Regex.Match(message, @"^!mytimers$").Success)
                //{
                //    var toBeSent = _commandFunctions.ChannelTimers(_db, FromChannel);
                //    if (toBeSent != null)
                //    {
                //        SendChatMessage(toBeSent, FromChannel);
                //    }
                //}

                //else if (Regex.Match(message, @"!addtimer").Success)
                //{
                //    var mytimer = _commandFunctions.AddTimer(FromChannel, message, 3, this);
                //    SendChatMessage("Timer added", FromChannel);
                //    GC.KeepAlive(mytimer);
                //}

                //if (Regex.Match(message, @"!sron").Success)
                //{

                //    var listOfMods = get_list_of_mods(FromChannel);
                //    if (listOfMods.Contains(MsgSender))
                //    {
                //        //sr turned on
                //        SendChatMessage("Song requests are now on.", FromChannel);
                //    }
                //    else
                //    {
                //        SendChatMessage("Only moderators can turn song requests on.", FromChannel);
                //    }
                //}
                //else if (Regex.Match(message, @"!sroff").Success)
                //{
                //    var listOfMods = get_list_of_mods(FromChannel);
                //    if (listOfMods.Contains(MsgSender))
                //    {
                //        //sr turned on
                //        SendChatMessage("Song requests are now off.", FromChannel);
                //    }
                //    else
                //    {
                //        SendChatMessage("Only moderators can turn song requests off.", FromChannel);
                //    }
                //}
                //}
                //else if (Regex.Match(message, @"!gameqon").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!gameqoff").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!clearq").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!players").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!leave").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!position").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //Regex.Match(message, @"^!songrequest\s+").Success ||

                else if (Regex.Match(ChatEvent.Msg, @"^!sr\s+").Success)
                {
                    var response = _commandFunctions.SearchSong(ChatEvent.Msg, ChatEvent.MsgSender, _db, ChatEvent.FromChannel);
                    response = ChatEvent.MsgSender + ", " + response;
                    Irc.SendChatMessage(response, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"^!currentsong$").Success)
                {
                    var song = _db.GetCurrentSong(ChatEvent.FromChannel);
                    Irc.SendChatMessage(song, ChatEvent.FromChannel);
                }
                else if (Regex.Match(ChatEvent.Msg, @"!songlist").Success || Regex.Match(ChatEvent.Msg, @"!sl").Success || Regex.Match(ChatEvent.Msg, @"!playlist").Success)
                {
                    Irc.SendChatMessage(ChatEvent.MsgSender + " the playlist can be found here http://chinnbot.tv/songlist?user=" + ChatEvent.FromChannel, ChatEvent.FromChannel);
                }

                //else if (Regex.Match(message, @"!addquote").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!quote").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
                //else if (Regex.Match(message, @"!m8b").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", FromChannel);
                //}
            }
            #endregion
        }

        #endregion


    }
}

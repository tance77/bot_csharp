using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    class IrcClient
    {
        private readonly string _botUserName;
        private readonly StreamReader _inputStream;
        private readonly StreamWriter _outputStream;
        private readonly DatabaseFunctions _db = new DatabaseFunctions();
        private readonly RiotApi _riotApi;
        private readonly TwitchApi _twitchApi = new TwitchApi();
        private readonly CommandFunctions _commandFunctions = new CommandFunctions();
        private List<Messages> _channelHistory; 



        public IrcClient(string ip, int port, string userName, string oAuth)
        {
            _riotApi = new RiotApi(_db);
            _botUserName = userName;
            _channelHistory = new List<Messages>();
            var tcpClient = new TcpClient(ip, port);
            _inputStream = new StreamReader(tcpClient.GetStream());
            _outputStream = new StreamWriter(tcpClient.GetStream());

            _outputStream.WriteLine("PASS " + oAuth);
            _outputStream.WriteLine("NICK " + userName);
            _outputStream.WriteLine("CAP REQ :twitch.tv/membership");
            _outputStream.WriteLine("CAP REQ :twitch.tv/tags");
            _outputStream.WriteLine("CAP REQ :twitch.tv/commands");
            _outputStream.Flush();

            var followerTimer = new System.Timers.Timer { Interval = 30000 };
            followerTimer.Elapsed += AnnounceFollowers;
            followerTimer.AutoReset = true;
            followerTimer.Enabled = true;

            var pointsTenTimer = new System.Timers.Timer { Interval = 600000 }; //1 coin every 10 minutes
            pointsTenTimer.Elapsed += AddPointsTen;
            pointsTenTimer.AutoReset = true;
            pointsTenTimer.Enabled = true;



        }
        public void AnnounceFollowers(Object source, System.Timers.ElapsedEventArgs e)
        {
            var channelList = _db.GetListOfChannels();
            foreach (var channel in channelList)
            {
                var message = _commandFunctions.AssembleFollowerList(channel, _db, _twitchApi);
                if (message != null)
                {
                    SendChatMessage(message, channel);
                    Thread.Sleep(1000);
                }
            }
        }
        public void AddPointsTen(Object source, System.Timers.ElapsedEventArgs e)
        {
            var channelList = _db.GetListOfChannels();
            foreach (var channel in channelList)
            {
                var response = _twitchApi.GetStreamUptime(channel);
                if (response == "Stream is offline." || response == "Could not reach Twitch API") continue; //continue the loop
                var userList = _twitchApi.GetActiveUsers(channel);
                _db.AddCoins(1, channel, userList);
            }
        } 

        public void JoinChannel(string channel)
        {
            _outputStream.WriteLine("JOIN #" + channel);
            _outputStream.Flush();
        }


        public void JoinChannelStartup()
        {
            Console.Write("-------------------------------- Loading Channels to Join ------------------------------- \r\n");
            var channelsToJoin = _db.JoinChannels();
            foreach (var channel in channelsToJoin)
            {
                JoinChannel(channel);
                Console.Write("Joining Channel " + channel + "\r\n");
            }
            Console.Write("-------------------------------- Finished Loading Channels ------------------------------- \r\n");
        }

        private void PartChannel(string channel)
        {
            _outputStream.WriteLine("PART #" + channel);
            _outputStream.Flush();
        }

        private void SendIrcMessage(string message)
        {
            _outputStream.WriteLine(message);
            _outputStream.Flush();
        }

        private void SendChatMessageLobby(string message)
        {
            SendIrcMessage(":" + _botUserName + "!" + _botUserName + "@"
                + _botUserName + ".tim.twitch.tv PRIVMSG #chinnbot :" + message);
        }

        public void SendChatMessage(string message, string channelName)
        {
            SendIrcMessage(":" + _botUserName + "!" + _botUserName + "@"
                + _botUserName + ".tim.twitch.tv PRIVMSG #" + channelName + " :" + message);
        }

        public string ReadMessage()
        {
                var buf = _inputStream.ReadLine();
                if (buf == null) return "";
                if (!buf.StartsWith("PING ")) return buf;
                _outputStream.Write(buf.Replace("PING", "PONG") + "\r\n");
                Console.Write(buf.Replace("PING", "PONG") + "\r\n");
                _outputStream.Flush();
                return buf;
        }

        private void kill_user(string fromChannel, string msgSender, string userType)
        {
            if (userType == "mod")
            {
                return;
            }
            SendChatMessage("/timeout " + msgSender + " 4", fromChannel);
            SendChatMessage("/timeout " + msgSender + " 3", fromChannel);
            SendChatMessage("/timeout " + msgSender + " 2", fromChannel);
            SendChatMessage("/timeout " + msgSender + " 1", fromChannel);
        }

        private bool CheckSpam(string message, string fromChannel, string msgSender, string userType)
        {
            if (Regex.Match(message, @".*?I just got championship riven skin from here.*?").Success ||
                Regex.Match(message, @".*?I just got championship riven skin code.*?").Success ||
                Regex.Match(message, @".*?OMG I just won an Iphone 6.*?").Success ||
                Regex.Match(message, @".*?I am a 15 year old Rhinoceros.*?").Success ||
                Regex.Match(message, @".*?sexually Identify as*?").Success ||
                Regex.Match(message, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(message, @".*?[Rr]\.*[Aa]\.*[Ff]\.*[2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(message, @".*?[Gg][Rr][Ee][Yy].*?[Ww][Aa][Rr][Ww][Ii][Cc][Kk].*?[Mm][Ee][Dd][Ii][Ee][Vv][Aa][Ll].*?[Tt][Ww][Ii][Tt][Cc][Hh].*?[Aa][Nn][Dd].*?\d*.*?\d*.*?[Ii][]Pp].*?").Success ||
                Regex.Match(message, @"\$50 prepaid riot points from here").Success ||
                Regex.Match(message, @"I just got \$50 prepaid riot points from here its legit xD!!! http:\/\/getriotpointscodes\.com\/").Success)
            {
                if (userType == "mod") return false; //your a mod no timeout
                Thread.Sleep(400);
                SendChatMessage("/timeout " + msgSender + " 100", fromChannel);
                SendChatMessage(msgSender + ", [Spam Detected]", fromChannel);
                Thread.Sleep(400);
                SendChatMessage("/timeout " + msgSender + " 100", fromChannel);
                SendChatMessage("/timeout " + msgSender + " 100", fromChannel);
                Thread.Sleep(400);
                SendChatMessage("/ban " + msgSender, fromChannel);
                return true;
            }
            return false; //no spam in message
        }

        public bool CheckUrls(string message, string fromChannel, string sender, string userType)
        {
            if (_db.UrlStatus(fromChannel)) return false;
            if (!Regex.Match(message, @"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)")
                .Success || userType == "mod") return false;
            if (_db.PermitExist(fromChannel, sender) && _db.CheckPermitStatus(fromChannel, sender)) //if they exist in permit and if permit has not expired
            //if it got here that means it was a url and they were permitted
            {
                _db.RemovePermit(fromChannel, sender);
                return false;
            }
            //Otherwise your not a mod or you are posting a link
            Thread.Sleep(400);
            SendChatMessage(sender + ", you need permission before posting a link. [Warning]", fromChannel);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            return true;
        }


        public void AddUserMessageHistory(string fromChannel, string msgSender, string msg)
        {

            var toBeDeleted = new List<Messages>();
            var empty = true;

            foreach (var msgContainer in _channelHistory)
            {
                if (msgContainer.GetChannel() == fromChannel && msgContainer.GetSender() == msgSender && msgContainer.Count() < 20)
                {
                    msgContainer.AddMessage(msg);
                    empty = false;
                }
                else if (msgContainer.GetChannel() == fromChannel && msgContainer.GetSender() == msgSender && msgContainer.Count() >= 20)
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
                _channelHistory.Add(new Messages(fromChannel, msgSender, msg));
            }

            foreach (var item in toBeDeleted)
            {
                _channelHistory.Remove(item);
            }
        }


        private static List<string> GetListOfMods(string fromChannel) //via chatters json deprecated
        {
            var modList = new List<string>();
            var url = "http://tmi.twitch.tv/group/user/" + fromChannel + "/chatters";
            var request = WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null) return modList;
                    using (var objReader = new StreamReader(responseStream))
                    {
                        var jsonString = objReader.ReadToEnd();
                        var mods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("moderators");
                        var staff = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("staff");
                        var admins = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("admins");
                        var globalMods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("global_mods");
                        modList.AddRange(mods.Select(mod => (string)mod));
                        modList.AddRange(staff.Select(user => (string)user));
                        modList.AddRange(admins.Select(admin => (string)admin));
                        modList.AddRange(globalMods.Select(globalMod => (string)globalMod));
                        return modList;
                    }
                }
            }
        }

        public void MatchCommand(string message, string fromChannel, string sender)
        {
            if (!message.StartsWith("!")) return;
            var commandFound = _db.MatchCommand(message, fromChannel);
            if (commandFound == null) return;
            if (!commandFound.Item2)
                SendChatMessage(commandFound.Item1, fromChannel);
            else
                SendChatMessage(sender + ", " + commandFound.Item1, fromChannel);
        }

        public void MessageHandler(string m)
        {
            /*------- Successfull Twitch Connection -----------*/
            if (Regex.Match(m, @":tmi.twitch.tv").Success)
            {
                var messageArray = m.Split(' ');
                if (messageArray.Length != 2)
                {
                    var messageSender = messageArray[0];
                    var messageCommand = messageArray[1];
                    var messageRecipient = messageArray[2];
                    var message = "";
                    for (var i = 3; i < messageArray.Length; i++)
                    {
                        if (i == messageArray.Length - 1)
                        {
                            message += messageArray[i];
                        }
                        else
                        {
                            message += messageArray[i] + " ";
                        }
                    }
                }
            }
            else if (Regex.Match(m, @"tmi.twitch.tv JOIN").Success)
            {

                var fromChannel = m.Split('#')[1];
                var joiner = m.Split('!')[0].Split(':')[1];
                joiner = joiner.ToLower();
                if (joiner == "dongerinouserino")
                {
                    SendChatMessage("ᕙ༼ຈل͜ຈ༽ᕗ flex your dongers ᕙ༼ຈل͜ຈ༽ᕗᕙ༼ຈل͜ຈ༽ᕗ DongerinoUserino is here ᕙ༼ຈل͜ຈ༽ᕗ ",
                        fromChannel);
                }
                if (joiner == "luminexi")
                {
                    SendChatMessage("Luminexi... you mean Lumisexi DatSheffy", fromChannel);
                }
            }
            else if (Regex.Match(m, @"tmi.twitch.tv PART").Success)
            {
            }
            else if (Regex.Match(m, @"tmi.twitch.tv 353").Success || Regex.Match(m, @"tmi.twitch.tv 366").Success)
            {
            }
            else if (Regex.Match(m, @":jtv MODE").Success)
            {
                var messageParts = m.Split(' ');
                var fromChanel = messageParts[2].Split('#')[1];
                var privlages = messageParts[3];
                var user = messageParts[4];
            }
            else if (Regex.Match(m, @"msg-id=subs_on :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=subs_off :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=slow_on :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=slow_off :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=r9k_on :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=r9k_off :tmi.twitch.tv NOTICE").Success)
            {
            }
            else if (Regex.Match(m, @"msg-id=host_on :tmi.twitch.tv NOTICE").Success)
            {
                //:tmi.twitch.tv HOSTTARGET #hosting_channel :target_channel [number]
            }
            else if (Regex.Match(m, @"msg-id=host_off :tmi.twitch.tv NOTICE").Success)
            {
                //> :tmi.twitch.tv HOSTTARGET #hosting_channel :- [number]
            }
            else if (Regex.Match(m, @":tmi.twitch.tv CLEARCHAT").Success)
            {
            }
            else if (Regex.Match(m, @":tmi.twitch.tv USERSTATE").Success)
            {
            }
            else if (Regex.Match(m, @":twitchnotify!twitchnotify@twitchnotify.tmi.twitch.tv").Success)
            {
            }
            else if (Regex.Match(m, @"tmi.twitch.tv PRIVMSG").Success)
            {
                var msgArray = m.Split(' ');
                var msg = "";
                var fromChannel = msgArray[3].Split('#')[1];
                var msgSender = msgArray[1].Split(':')[1].Split('!')[0];
                var msgCommand = msgArray[2];
                for (var s = 4; s < msgArray.Length; s++) //form the message since we split on space
                {
                    if (s == msgArray.Length - 1)
                        msg += msgArray[s];
                    else
                        msg += msgArray[s] + " ";
                }
                msg = msg.TrimStart(':');
                var prefix = msgArray[0].Split(';');
                var color = prefix[0].Split('=')[1].Split('"')[0];
                var displayName = prefix[1].Split('=')[1].Split('"')[0];
                try
                {
                    var emotes = prefix[2].Split('=')[1].Split('"')[0];
                }
                catch (IndexOutOfRangeException)
                {
                    var emotes = "";
                }
                var subscriber = prefix[3].Split('=')[1].Split('"')[0];
                var turbo = prefix[4].Split('=')[1].Split('"')[0];
                var userType = prefix[5].Split('=')[1].Split('"')[0].Split(' ')[0];
                if (fromChannel == msgSender)
                {
                    userType = "mod";
                }

                //Adds message to user message history
                AddUserMessageHistory(fromChannel,msgSender, msg);

                if (CheckSpam(msg, fromChannel, msgSender, userType)) return;
                if (CheckUrls(msg, fromChannel, msgSender, userType)) return;
                CheckCommands(msg, userType, fromChannel, msgCommand, msgSender);
            }
        }

        public void CheckCommands(string message, string userType, string fromChannel, string msgCommand, string msgSender)
        {
            /*-------------------------JOIN/PART FOR CHINNBOT ONLY ------------------------*/
            if (fromChannel == "chinnbot")
            {
                if (Regex.Match(message, @"!join").Success)
                {
                    if (_db.AddToChannels(msgSender))
                    {
                        _commandFunctions.JoinAssembleFollowerList(fromChannel, _db, _twitchApi);
                        SendChatMessageLobby("Joining channel, " + msgSender +
                                             ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                        JoinChannel(msgSender);
                    }
                    else
                    {
                        SendChatMessageLobby(msgSender +
                                             ", I am already in your channel. Type !part if you wish me to leave your channel.");
                    }
                }
                else if (Regex.Match(message, @"!part").Success)
                {
                    if (_db.RemoveFromChannels(msgSender))
                    {
                        PartChannel(msgSender);
                        SendChatMessageLobby(msgSender + ", I will no longer monitor your channel.");
                    }
                    else
                    {
                        SendChatMessageLobby(msgSender +
                                             ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                    }
                }
                /*---------------------END JOIN/PART FOR CHINNBOT ONLY ------------------------*/
            }
            else if (msgSender == "nightbot" || msgSender == "moobot" || msgSender == "xanbot")
            {
            }
            else
            {
                MatchCommand(message, fromChannel, msgSender);

                if (Regex.Match(message, @"!commands").Success)
                {
                    SendChatMessage(_commandFunctions.GetChannelCommands(fromChannel, _db), fromChannel);
                }
                else if (Regex.Match(message, @"^!masteries$").Success)
                {
                    SendChatMessage(_commandFunctions.GetMasteries(fromChannel, _riotApi), fromChannel);
                }
                else if (Regex.Match(message, @"^!rank$").Success)
                {
                    var resoponse = _commandFunctions.GetLeagueRank(fromChannel, msgSender, _db, _riotApi);
                    SendChatMessage(resoponse, fromChannel);
                }
                else if (Regex.Match(message, @"^!runes$").Success)
                {
                    var runeDictionary = _riotApi.GetRunes(fromChannel);
                    if (runeDictionary != null)
                    {
                        SendChatMessage(_commandFunctions.ParseRuneDictionary(runeDictionary), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Please set up your summoner name by typing !setsummoner [summonername]",
                            fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!allowurls\son$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.UrlToggle(fromChannel, true, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!allowurls\soff$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.UrlToggle(fromChannel, false, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!dicksize$").Success)
                {
                    var response = _commandFunctions.DickSize(fromChannel, msgSender, _db);
                    SendChatMessage(response, fromChannel);
                }
                else if (Regex.Match(message, @"^!dicksize\son$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.DickSizeToggle(fromChannel, true, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!dicksize\soff$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.DickSizeToggle(fromChannel, false, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"!addcom").Success)
                {
                    if (userType == "mod")
                    {
                        var response = _commandFunctions.AddCommand(fromChannel, message, _db);
                        if (response != null)
                        {
                            SendChatMessage(response, fromChannel);
                        }
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"!editcom").Success)
                {
                    if (userType == "mod")
                    {
                        var response = _commandFunctions.EditCommand(fromChannel, message, _db);
                        if (response != null)
                        {
                            SendChatMessage(response, fromChannel);
                        }
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"!removecom").Success || Regex.Match(message, @"!delcom").Success)
                {
                    if (userType == "mod")
                    {
                        var response = _commandFunctions.RemoveCommand(fromChannel, message, _db);
                        if (response != null)
                        {
                            SendChatMessage(response, fromChannel);
                        }
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!roulette$").Success)
                {
                    if (_commandFunctions.Roulette(fromChannel))
                    {

                        Thread.Sleep(400);
                        SendChatMessage("/timeout " + msgSender + " 60", fromChannel);
                        SendChatMessage(msgSender + ", took a bullet to the head.", fromChannel);
                    }
                    else
                    {
                        SendChatMessage(msgSender + ", pulled the trigger and nothing happened.", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"!setsummoner").Success)
                {
                    var summonerName = _commandFunctions.SplitSummonerName(message);
                    SendChatMessage(_commandFunctions.SetSummonerName(fromChannel, summonerName, msgSender, _db), fromChannel);
                    var summonerId = _riotApi.GetSummonerId(summonerName);
                    _db.SetSummonerId(fromChannel, summonerId);
                }
                else if (Regex.Match(message, @"^!suicide$").Success)
                {
                    kill_user(fromChannel, msgSender, userType);
                }

                else if (message == "gg")
                {
                    if (_commandFunctions.CheckGg(fromChannel, _db))
                    {
                        SendChatMessage("GG", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!gg\son$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.GgToggle(fromChannel, true, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!gg\soff$").Success)
                {
                    if (userType == "mod")
                    {
                        SendChatMessage(_commandFunctions.GgToggle(fromChannel, false, _db), fromChannel);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (
                    Regex.Match(message,
                        @".*?[Ss][Hh][Oo][Ww].*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?")
                        .Success ||
                    Regex.Match(message,
                        @".*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?[Pp]+[Ll]+(([Ee]+[Aa]+[Ss]+[Ee]+)|([Zz]+)|([Ss]+)).*?")
                        .Success)
                {
                    SendChatMessage(msgSender + " here's  your boobs NSFW https://goo.gl/BNl3Gl", fromChannel);
                }
                else if (Regex.Match(message, @"^!uptime$").Success)
                {
                    SendChatMessage(_twitchApi.GetStreamUptime(fromChannel), fromChannel);
                }
                else if (Regex.Match(message, @"!permit").Success)
                {
                    var response = _commandFunctions.PermitUser(fromChannel, msgSender, message, userType, _db);
                    if (response != null)
                    {
                        SendChatMessage(response, fromChannel);
                    }
                }
                //else if (Regex.Match(message, @"!addtimer").Success)
                //{
                //    var mytimer = _commandFunctions.AddTimer(fromChannel, message, 3, this);
                //    SendChatMessage("Timer added", fromChannel);
                //    GC.KeepAlive(mytimer);
                //}

                /*------------------------- Built In Commands ------------------------*/
                //if (Regex.Match(message, @"!sron").Success)
                //{

                //    var listOfMods = get_list_of_mods(fromChannel);
                //    if (listOfMods.Contains(msgSender))
                //    {
                //        //sr turned on
                //        SendChatMessage("Song requests are now on.", fromChannel);
                //    }
                //    else
                //    {
                //        SendChatMessage("Only moderators can turn song requests on.", fromChannel);
                //    }
                //}
                //else if (Regex.Match(message, @"!sroff").Success)
                //{
                //    var listOfMods = get_list_of_mods(fromChannel);
                //    if (listOfMods.Contains(msgSender))
                //    {
                //        //sr turned on
                //        SendChatMessage("Song requests are now off.", fromChannel);
                //    }
                //    else
                //    {
                //        SendChatMessage("Only moderators can turn song requests off.", fromChannel);
                //    }
                //}
                //}
                //else if (Regex.Match(message, @"!gameqon").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!gameqoff").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!clearq").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!players").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!leave").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!position").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}

                //else if (Regex.Match(message, @"!songrequest").Success || Regex.Match(post_message, @"!sr").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!songlist").Success || Regex.Match(post_message, @"!sl").Success || Regex.Match(post_message, @"!playlist").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}

                //else if (Regex.Match(message, @"!addquote").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!quote").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
                //else if (Regex.Match(message, @"!m8b").Success)
                //{
                //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                //}
            }

        }
    }
}

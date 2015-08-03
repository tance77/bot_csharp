using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
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


        public IrcClient(string ip, int port, string userName, string oAuth)
        {
            _riotApi = new RiotApi(_db);
            _botUserName = userName;
            var tcpClient = new TcpClient(ip, port);
            _inputStream = new StreamReader(tcpClient.GetStream());
            _outputStream = new StreamWriter(tcpClient.GetStream());

            _outputStream.WriteLine("PASS " + oAuth);
            _outputStream.WriteLine("NICK " + userName);
            _outputStream.WriteLine("CAP REQ :twitch.tv/membership");
            _outputStream.WriteLine("CAP REQ :twitch.tv/tags");
            _outputStream.Flush();

        }

        public void JoinChannel(string channel)
        {
            _outputStream.WriteLine("JOIN #" + channel);
            _outputStream.Flush();
        }


        public void JoinChannelStartup()
        {
            Console.Write("-------------------------------- Loading Channels to Join ------------------------------- \r\n");
            //_db.ConnectToDatabase();
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

        private bool CheckSpam(string message, string fromChannel, string msgSender, string userType)
        {
            if (Regex.Match(message, @".*?I just got championship riven skin from here.*?").Success ||
                Regex.Match(message, @".*?I just got championship riven skin code.*?").Success ||
                Regex.Match(message, @".*?OMG I just won an Iphone 6.*?").Success ||
                Regex.Match(message, @".*?I am a 15 year old Rhinoceros.*?").Success ||
                Regex.Match(message, @".*?sexually Identify as*?").Success ||
                Regex.Match(message, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success ||
                Regex.Match(message, @".*?[Rr]\.*[Aa]\.*[Ff]\.*[2].*?[Cc][Oo][Mm].*?").Success)
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
            //Otherwise your not a mod or you are posting a link
            Thread.Sleep(400);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            SendChatMessage(sender + ", no urls allowed.", fromChannel);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            SendChatMessage("/timeout " + sender + " 10", fromChannel);
            return true;
        }

        public bool CheckGg(string fromChannel)
        {
            return _db.GgStatus(fromChannel);
        }

        public string CheckSummonerName(string fromChannel)
        {
            var summonerName = _db.SummonerStatus(fromChannel);
            if (summonerName == "") return "No Summoner Name";
            var summonerId = _riotApi.GetSummonerId(summonerName);
            //GetRunes(summonerId);
            if (summonerId == "400" || summonerId == "401" || summonerId == "404" || summonerId == "429" || summonerId == "500" || summonerId == "503") // Invalid summoner name
            {
                return summonerId;
            }
            if (!_db.SetSummonerId(fromChannel, summonerId)) return "ERR Summoner ID";
            var rank = _riotApi.GetRank(summonerId);
            if (rank == "400" || rank == "401" || rank == "404" || rank == "429" || rank == "500" || rank == "503") // Invalid summoner name
            {
                return rank;
            }
            return rank;
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
                        modList.AddRange(mods.Select(mod => (string) mod));
                        modList.AddRange(staff.Select(user => (string) user));
                        modList.AddRange(admins.Select(admin => (string) admin));
                        modList.AddRange(globalMods.Select(globalMod => (string) globalMod));
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

        public void GetChannelCommands(string channel)
        {
            var commands = _db.GetChannelCommands(channel);
            if (commands == null || !commands.Any())
            {
                SendChatMessage("No commands were found for this channel.", channel);
                return;
            }
            var sendString = "";
            for (var i = 0; i < commands.Count(); i++)
            {
                if (i == commands.Count() - 1)
                {
                    sendString += " !" + commands[i];
                }
                else
                {
                    sendString += " !" + commands[i] + ",";
                }
            }
            SendChatMessage("Commands are" + sendString, channel);
        }

        public void AddCommand(string channel, string message)
        {
            if (!message.StartsWith("!")) { return; }
            try
            {
                var messageArray = message.Split(' ');
                var command = messageArray[1].Split('!')[1];
                var commandDescription = "";
                for (var i = 2; i < messageArray.Length; i++)
                {
                    if (i == messageArray.Length - 1)
                    {
                        commandDescription += messageArray[i];
                    }
                    else
                    {
                        commandDescription += messageArray[i] + " ";
                    }
                }
                var success = _db.AddCommand(command, commandDescription, false, channel);
                command = "!" + command;
                if (success)
                {
                    SendChatMessage(command + " was add successfully.", channel);
                }
                else
                {
                    SendChatMessage(command + " command already exists use !editcom if you would like to change it.",
                        channel);
                }
            }
            catch (IndexOutOfRangeException)
            {
                SendChatMessage("With no <> syntax is <!addcom> <!command_name> <response>", channel);
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                SendChatMessage("Sorry something went wrong on my end, please try again.", channel);
            }
        }

        public void RemoveCommand(string channel, string message)
        {
            if (!message.StartsWith("!")) return;
            try
            {
                var splitMessage = message.Split(' ');
                var command = splitMessage[1].Split('!')[1];
                var succes = _db.RemoveCommand(command, channel);
                command = "!" + command;
                if (succes)
                {
                    SendChatMessage(command + " was deleted successfully.", channel);
                }
                else
                {
                    SendChatMessage(command + " there is no such command.", channel);
                }

            }
            catch (IndexOutOfRangeException)
            {
                SendChatMessage("With no <> syntax is <!delcom> <!command_name>", channel);
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                SendChatMessage("Sorry something went wrong on my end, please try again.", channel);
            }
        }

        public void EditCommand(string channel, string message)
        {

            if (!message.StartsWith("!")) { return; }
            try
            {
                var splitMessage = message.Split(' ');
                var command = splitMessage[1].Split('!')[1];
                var commandDescription = "";
                for (var i = 2; i < splitMessage.Length; i++)
                {
                    if (i == splitMessage.Length - 1)
                    {
                        commandDescription += splitMessage[i];
                    }
                    else commandDescription += splitMessage[i] + " ";
                }
                var success = _db.EditCommand(command, commandDescription, false, channel);
                command = "!" + command;
                if (success)
                {
                    SendChatMessage(command + " was updated successfully.", channel);
                }
                else
                {
                    SendChatMessage(command + " there is no such command. Use !addcom if you wish to add a command.",
                        channel);
                }

            }
            catch (IndexOutOfRangeException)
            {
                SendChatMessage("With no <> syntax is <!editcom> <!command_name> <response>", channel);
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                SendChatMessage("Sorry something went wrong on my end, please try again.", channel);

            }
        }

        public void DickSize(string channel, string sender)
        {
            var response = _db.DickSize(channel);
            if (response == null) { return; }
            SendChatMessage(sender + ", " + response, channel);
        }

        public void DickSizeToggle(string channel, bool toggle)
        {
            var success = _db.DickSizeToggle(channel, toggle);
            if (success)
            {
                SendChatMessage(toggle ? "Dicksize is now on." : "Dicksize is now off.", channel);
            }
            else
            {
                SendChatMessage("Something went wrong on my end.", channel);
            }
        }

        public void UrlToggle(string channel, bool toggle)
        {
            var success = _db.UrlToggle(channel, toggle);
            if (success)
            {
                SendChatMessage(toggle ? "URL's are now allowed." : "URL's are no longer allowed.", channel);
            }
            else
            {
                SendChatMessage("Something went wrong on my end.", channel);
            }
        }

        public void GgToggle(string channel, bool toggle)
        {
            var success = _db.GgToggle(channel, toggle);
            if (success)
            {
                SendChatMessage(toggle ? "GG is now on." : "GG is now off.", channel);
            }
            else
            {
                SendChatMessage("Something went wrong on my end.", channel);
            }
        }

        public void Roulette(string channel, string sender)
        {
            var chamber = new Random();
            var deathShot = chamber.Next(1, 3);
            var playerShot = chamber.Next(1, 3);
            if (deathShot == playerShot)
            {
                Thread.Sleep(400);
                SendChatMessage("/timeout " + sender + " 60", channel);
                SendChatMessage(sender + ", took a bullet to the head.", channel);
            }
            else
            {
                SendChatMessage(sender + ", pulled the trigger and nothing happened.", channel);
            }
        }


        public void ParseRuneDictionary(Dictionary<string, int> runeDictionary, string fromChannel)
        {
            string message = "";
            foreach (var name in runeDictionary)
            {
                if (name.Equals(runeDictionary.Last()))
                {
                    message += name.Key + " x" + name.Value;
                    break;
                }
                message += name.Key + " x" + name.Value + " ";
            }
            SendChatMessage(message, fromChannel);
        }


        public void GetLeagueRank(string fromChannel, string msgSender)
        {
            var result = CheckSummonerName(fromChannel);
            if (result == "No Summoner Name")
            {
                SendChatMessage(
                    "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]",
                    fromChannel);
            }
            else if (result == "400" || result == "401")
            {
                SendChatMessage("Invalid Summoner Name", fromChannel);
            }
            else if (result == "404")
            {
                SendChatMessage(fromChannel + " is not yet ranked.", fromChannel);
            }
            else if (result == "429")
            {
                SendChatMessage("To many requests at one time please try again.", fromChannel);
            }
            else if (result == "500" || result == "503")
            {
                SendChatMessage("Could not reach Riot API. Please try again in a few minutes.", fromChannel);
            }
            else
            {
                SendChatMessage(fromChannel + " is currently " + result, fromChannel);
            }

        }

        public void GetMasteries(string fromChannel)
        {
            var masteriesDictionary = _riotApi.GetMasteries(fromChannel);
            if (masteriesDictionary == null)
            {
                SendChatMessage(
                    "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]",
                    fromChannel);
            }
            else
            {
                var message = new StringBuilder();
                foreach (var tree in masteriesDictionary)
                    message.AppendFormat("{0}: {1} ", tree.Key, tree.Value);
                SendChatMessage(message.ToString(), fromChannel);
            }
        }



        public bool SetSummonerName(string fromChannel, string summonerName, string msgSender)
        {
            if (msgSender == fromChannel)//do this so only channel admin can set the summoenr name
            {
                if (_db.SetSummonerName(fromChannel, summonerName))// on success
                {
                    SendChatMessage("Summoner name has been set to " + summonerName, fromChannel);
                    return true;
                }
                SendChatMessage("Something went wrong on my end please try again", fromChannel);
                return false;
            }

            SendChatMessage("Insufficient privileges", fromChannel);
            return false;
        }

        public string SplitSummonerName(string message)
        {
            var msgParts = message.Split(' ');
            var summonerName = new StringBuilder();
            for (int i = 1; i < msgParts.Length ; i++)
            {
                if (i == msgParts.Length)
                {
                    summonerName.Append(msgParts[i]);
                }
                else
                {
                    summonerName.Append(msgParts[i] + " ");
                }
            }
            return summonerName.ToString();
        }

        public bool MessageHandler(string m)
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
                    return true;
                }
            }
            if (Regex.Match(m, @"tmi.twitch.tv JOIN").Success)
            {

                var fromChannel = m.Split('#')[1];
                var joiner = m.Split('!')[0].Split(':')[1];
                if (joiner == "dongerinouserino")
                {
                    SendChatMessage("ᕙ༼ຈل͜ຈ༽ᕗ flex your dongers ᕙ༼ຈل͜ຈ༽ᕗᕙ༼ຈل͜ຈ༽ᕗ DongerinoUserino is here ᕙ༼ຈل͜ຈ༽ᕗ ",
                        fromChannel);
                }
                return true;
            }
            if (Regex.Match(m, @"tmi.twitch.tv PART").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"tmi.twitch.tv 353").Success || Regex.Match(m, @"tmi.twitch.tv 366").Success)
            {
                return true;

            }
            if (Regex.Match(m, @":jtv MODE").Success)
            {
                var messageParts = m.Split(' ');
                var fromChanel = messageParts[2].Split('#')[1];
                var privlages = messageParts[3];
                var user = messageParts[4];
                return true;

            }
            if (Regex.Match(m, @"msg-id=subs_on :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=subs_off :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=slow_on :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=slow_off :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=r9k_on :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=r9k_off :tmi.twitch.tv NOTICE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"msg-id=host_on :tmi.twitch.tv NOTICE").Success)
            {
                //:tmi.twitch.tv HOSTTARGET #hosting_channel :target_channel [number]
                return true;
            }
            if (Regex.Match(m, @"msg-id=host_off :tmi.twitch.tv NOTICE").Success)
            {
                //> :tmi.twitch.tv HOSTTARGET #hosting_channel :- [number]
                return true;
            }
            if (Regex.Match(m, @":tmi.twitch.tv CLEARCHAT").Success)
            {
                return true;
            }
            if (Regex.Match(m, @":tmi.twitch.tv USERSTATE").Success)
            {
                return true;
            }
            if (Regex.Match(m, @":twitchnotify!twitchnotify@twitchnotify.tmi.twitch.tv").Success)
            {
                return true;
            }
            if (Regex.Match(m, @"tmi.twitch.tv PRIVMSG").Success)
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
                if (CheckSpam(msg, fromChannel, msgSender, userType)) return true;
                if (CheckUrls(msg, fromChannel, msgSender, userType)) return true;
                CheckCommands(msg, userType, fromChannel, msgCommand, msgSender);
                return true;
            }
            return false;
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
                    GetChannelCommands(fromChannel);
                }
                else if (Regex.Match(message, @"^!masteries$").Success)
                {
                    GetMasteries(fromChannel);
                }
                else if (Regex.Match(message, @"^!rank$").Success)
                {
                    GetLeagueRank(fromChannel, msgSender);
                }
                else if (Regex.Match(message, @"^!runes$").Success)
                {
                    var runeDictionary = _riotApi.GetRunes(fromChannel);
                    if (runeDictionary != null)
                    {
                        ParseRuneDictionary(runeDictionary, fromChannel);
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
                        UrlToggle(fromChannel, true);
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
                        UrlToggle(fromChannel, false);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!dicksize$").Success)
                {
                    DickSize(fromChannel, msgSender);
                }
                else if (Regex.Match(message, @"^!dicksize\son$").Success)
                {
                    if (userType == "mod")
                    {
                        DickSizeToggle(fromChannel, true);
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
                        DickSizeToggle(fromChannel, false);
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
                        AddCommand(fromChannel, message);
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
                        EditCommand(fromChannel, message);
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
                        RemoveCommand(fromChannel, message);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^!roulette$").Success)
                {
                    Roulette(fromChannel, msgSender);
                }
                else if (Regex.Match(message, @"!setsummoner").Success)
                {
                    var summonerName = SplitSummonerName(message);
                    SetSummonerName(fromChannel, summonerName, msgSender);
                }

                else if (message == "gg")
                {
                    if (CheckGg(fromChannel))
                    {
                        SendChatMessage("GG", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^gg\son$").Success)
                {
                    if (userType == "mod")
                    {
                        GgToggle(fromChannel, true);
                    }
                    else
                    {
                        SendChatMessage("Insufficient privileges", fromChannel);
                    }
                }
                else if (Regex.Match(message, @"^gg\soff$").Success)
                {
                    if (userType == "mod")
                    {
                        GgToggle(fromChannel, false);
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
                //else if (Regex.Match(message, @"!uptime").Success)
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

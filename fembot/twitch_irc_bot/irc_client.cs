using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    class IrcClient
    {
        private readonly string _botUserName;
        private readonly StreamReader _inputStream;
        private readonly StreamWriter _outputStream;
        private readonly DatabaseFunctions _db = new DatabaseFunctions();


        public IrcClient(string ip, int port, string userName, string oAuth)
        {
            _botUserName = userName;
            var tcpClient = new TcpClient(ip, port);
            _inputStream = new StreamReader(tcpClient.GetStream());
            _outputStream = new StreamWriter(tcpClient.GetStream());

            _outputStream.WriteLine("PASS " + oAuth);
            _outputStream.WriteLine("NICK " + userName);
            _outputStream.Flush();

        }

        private void JoinChannel(string channel)
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
            //_db.CloseConnection();
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

        private void SendChatMessage(string message)
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

        private bool CheckSpam(string message)
        {
            var fromChannel = message.Split('#')[1].Split(' ')[0];
            var sender = message.Split(':')[1].Split('!')[0].ToLower();

            if (Regex.Match(message, @"I just got championship riven skin from here").Success ||
                Regex.Match(message, @"I just got championship riven skin code").Success)
            {
                SendChatMessage("/timeout " + sender + " 1", fromChannel);
                SendChatMessage(sender + ", [Spam Detected]", fromChannel);
                return true;
            }
            if (!Regex.Match(message, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success) return false;
            SendChatMessage("/timeout " + sender + " 1", fromChannel);
            SendChatMessage(sender + ", [Spam Detected]", fromChannel);
            return true;
        }

        private List<string> GetListOfMods(string fromChannel)
        {
            var modList = new List<string>();
            var url = "http://tmi.twitch.tv/group/user/" + fromChannel + "/chatters";
            var response = WebRequest.Create(url);
            var objStream = response.GetResponse().GetResponseStream();
            if (objStream == null) return modList;
            var objReader = new StreamReader(objStream);

            string line = "", jsonString = "";

            while (line != null)
            {
                line = objReader.ReadLine();
                if (line != null)
                {
                    jsonString += line;
                }
            }

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
            };
            var sendString = commands.Aggregate(":", (current, command) => current + (", !" + command));
            SendChatMessage("Commands are" + sendString, channel);
        }

        public void CheckCommands(string message)
        {
            if (Regex.Match(message, @":tmi.twitch.tv").Success)
            {
                // var sender = message.Split(':')[1].Split(' ')[0].ToLower();
                // var ircNumber = message.Split(' ')[1].Split(' ')[0];
                // var origin_channel = message.Split(' ')[1].Split(' ')[1];
            }
            else if (Regex.Match(message, @":twitchnotify").Success) { }
            else
            {
                if (message.Split('#')[1].Split(' ')[0] == "chinnbot")
                {
                    /*-------------------------JOIN/PART FOR CHINNBOT ONLY ------------------------*/
                    if (Regex.Match(message, @"!join").Success)
                    {
                        var sender = message.Split(':')[1].Split('!')[0].ToLower();
                        //db.ConnectToDatabase();
                        if (_db.AddToChannels(sender))
                        {
                            SendChatMessage("Joining channel, " + sender +
                                              ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                            JoinChannel(sender);
                        }
                        else
                        {
                            SendChatMessage(sender +
                                              ", I am already in your channel. Type !part if you wish me to leave your channel.");
                        }
                        //_db.CloseConnection();
                    }
                    else if (Regex.Match(message, @"!part").Success)
                    {
                        var sender = message.Split(':')[1].Split('!')[0].ToLower();
                        //_db.ConnectToDatabase();
                        if (_db.RemoveFromChannels(sender))
                        {
                            PartChannel(sender);
                            SendChatMessage(sender + ", I will no longer monitor your channel.");
                        }
                        else
                        {
                            SendChatMessage(sender +
                                              ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                        }
                        //_db.CloseConnection();
                    }
                }
                else
                {
                    if (CheckSpam(message))
                    {
                        return;
                    }

                    /*------------------------- Built In Commands ------------------------*/

                    if (!Regex.Match(message, @"JOIN").Success && !Regex.Match(message, @"[\d]+ chinnbot").Success &&
                        !Regex.Match(message, @"PART").Success)
                    {
                        var fromChannel = message.Split('#')[1].Split(' ')[0];
                        var sender = message.Split(':')[1].Split('!')[0].ToLower();
                        var postMessage = message.Split('#')[1].Split(':')[1];

                        MatchCommand(postMessage, fromChannel, sender); //Checks the DB For a specific channel command

                        if (Regex.Match(postMessage, @"!commands").Success)
                        {
                            GetChannelCommands(fromChannel);
                        }

                        //if (Regex.Match(postMessage, @"!sron").Success)
                        //{

                        //    var listOfMods = get_list_of_mods(fromChannel);
                        //    if (listOfMods.Contains(sender))
                        //    {
                        //        //sr turned on
                        //        SendChatMessage("Song requests are now on.", fromChannel);
                        //    }
                        //    else
                        //    {
                        //        SendChatMessage("Only moderators can turn song requests on.", fromChannel);
                        //    }
                        //}
                        //else if (Regex.Match(postMessage, @"!sroff").Success)
                        //{
                        //    var listOfMods = get_list_of_mods(fromChannel);
                        //    if (listOfMods.Contains(sender))
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
                        //else if (Regex.Match(postMessage, @"!gameqon").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!gameqoff").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!clearq").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!players").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!leave").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!position").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!addcom").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!editcom").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        //else if (Regex.Match(postMessage, @"!removecom").Success)
                        //{
                        //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                        //}
                        if (Regex.Match(postMessage, @"!dicksize").Success)
                        {
                            var response = _db.DickSize();
                            SendChatMessage(sender + ", " + response, fromChannel);
                        }
                        else if (Regex.Match(postMessage, @"!roulette").Success)
                        {
                            var chamber = new Random();
                            var deathShot = chamber.Next(1, 3);
                            var playerShot = chamber.Next(1, 3);
                            if (deathShot == playerShot)
                            {
                                SendChatMessage("/timeout " + sender + " 60", fromChannel);
                                SendChatMessage(sender + ", took a bullet to the head.", fromChannel);
                            }
                            else
                            {
                                SendChatMessage(sender + ", pulled the trigger and nothing happened.", fromChannel);
                            }
                        }
                    
                            //else if (Regex.Match(postMessage, @"!songrequest").Success || Regex.Match(post_message, @"!sr").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                            //else if (Regex.Match(postMessage, @"!songlist").Success || Regex.Match(post_message, @"!sl").Success || Regex.Match(post_message, @"!playlist").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                            //else if (Regex.Match(postMessage, @"!uptime").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                            //else if (Regex.Match(postMessage, @"!addquote").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                            //else if (Regex.Match(postMessage, @"!quote").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                            //else if (Regex.Match(postMessage, @"!m8b").Success)
                            //{
                            //    SendChatMessage("Temporaryily Unavailable.", fromChannel);
                            //}
                        else if (postMessage == "gg")
                        {
                            SendChatMessage("GG", fromChannel);
                        }
                    }
                } 
            }
        }
    }
}

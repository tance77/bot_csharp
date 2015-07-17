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

        private bool CheckSpam(string message)
        {
            var fromChannel = message.Split('#')[1].Split(' ')[0];
            var sender = message.Split(':')[1].Split('!')[0].ToLower();

            if (Regex.Match(message, @"I just got championship riven skin from here").Success ||
                Regex.Match(message, @"I just got championship riven skin code").Success)
            {
                //SendChatMessage("/timeout " + sender + " 1", fromChannel);
                SendChatMessage("/ban " + sender, fromChannel);
                SendChatMessage(sender + ", [Spam Detected]", fromChannel);
                return true;
            }
            if (!Regex.Match(message, @".*?[Rr][Aa][Ff][2].*?[Cc][Oo][Mm].*?").Success) return false;
            //SendChatMessage("/timeout " + sender + " 1", fromChannel);
            SendChatMessage("/ban " + sender, fromChannel);
            SendChatMessage(sender + ", [Spam Detected]", fromChannel);
            return true;
        }

        private static List<string> GetListOfMods(string fromChannel)
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
            }
            var sendString = commands.Aggregate(":", (current, command) => current + (", !" + command));
            SendChatMessage("Commands are" + sendString, channel);
        }

        public void AddCommand(string channel, string message)
        {
            if (!message.StartsWith("!")) return;
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
                var success = _db.AddCommand(command, commandDescription, false, channel);
                command = "!" + command;
                if (success)
                    SendChatMessage(command + " was add successfully.", channel);
                else
                    SendChatMessage(command + " command already exists use !editcom if you would like to change it.", channel);
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
                    SendChatMessage(command + " was deleted successfully.", channel);
                else
                    SendChatMessage(command + " there is no such command.", channel);
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
            
            if (!message.StartsWith("!")) return;
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
                    SendChatMessage(command + " was updated successfully.", channel);
                else
                    SendChatMessage(command + " there is no such command. Use !addcom if you wish to add a command.", channel);
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

        public void DickSize(string fromChannel, string sender)
        {
            var response = _db.DickSize();
            SendChatMessage(sender + ", " + response, fromChannel);
        }

        public void Roulette(string channel, string sender)
        {
            var chamber = new Random();
            var deathShot = chamber.Next(1, 3);
            var playerShot = chamber.Next(1, 3);
            if (deathShot == playerShot)
            {
                SendChatMessage("/timeout " + sender + " 60", channel);
                SendChatMessage(sender + ", took a bullet to the head.", channel);
            }
            else
            {
                SendChatMessage(sender + ", pulled the trigger and nothing happened.", channel);
            }
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
                            SendChatMessageLobby("Joining channel, " + sender +
                                              ", please remember to mod me in your channel. Type /mod chinnbot into the chat to mod me.");
                            JoinChannel(sender);
                        }
                        else
                        {
                            SendChatMessageLobby(sender +
                                              ", I am already in your channel. Type !part if you wish me to leave your channel.");
                        }
                    }
                    else if (Regex.Match(message, @"!part").Success)
                    {
                        var sender = message.Split(':')[1].Split('!')[0].ToLower();
                        if (_db.RemoveFromChannels(sender))
                        {
                            PartChannel(sender);
                            SendChatMessageLobby(sender + ", I will no longer monitor your channel.");
                        }
                        else
                        {
                            SendChatMessageLobby(sender +
                                              ", I don't belive I'm in your channel. Type !join if you wish me to monitor your channel.");
                        }
                    }
                }
                else
                {
                    if (CheckSpam(message)) return;
                    if (Regex.Match(message, @"JOIN").Success || Regex.Match(message, @"[\d]+ chinnbot").Success ||
                        Regex.Match(message, @"PART").Success) return;
                    var fromChannel = message.Split('#')[1].Split(' ')[0];
                    var sender = message.Split(':')[1].Split('!')[0].ToLower();
                    var postMessage = message.Split('#')[1].Split(':')[1];

                    MatchCommand(postMessage, fromChannel, sender);

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
                    else if (Regex.Match(postMessage, @"!addcom").Success)
                    {
                        var listOfMods = GetListOfMods(fromChannel);
                        if (listOfMods.Contains(sender))
                            AddCommand(fromChannel, postMessage);
                        else
                            SendChatMessage("Insufficient privileges", fromChannel);
                    }
                    else if (Regex.Match(postMessage, @"!editcom").Success)
                    {
                        var listOfMods = GetListOfMods(fromChannel);
                        if (listOfMods.Contains(sender))
                            EditCommand(fromChannel, postMessage);
                        else
                            SendChatMessage("Insufficient privileges", fromChannel);
                    }
                    else if (Regex.Match(postMessage, @"!removecom").Success)
                    {
                        var listOfMods = GetListOfMods(fromChannel);
                        if (listOfMods.Contains(sender))
                            RemoveCommand(fromChannel, postMessage);
                        else
                            SendChatMessage("Insufficient privileges", fromChannel);
                    }
                    if (Regex.Match(postMessage, @"!dicksize").Success)
                    {
                        DickSize(fromChannel, sender);
                    }
                    else if (Regex.Match(postMessage, @"!roulette").Success)
                    {
                        Roulette(fromChannel, sender);
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
                    else if (
                        Regex.Match(postMessage,
                            @".*?[Ss][Hh][Oo][Ww].*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?")
                            .Success ||
                        Regex.Match(postMessage,
                            @".*?(([Tt][Ii][Tt][Ss])|([Bb](([Oo]+)|([Ee]+[Ww]+))[Bb]+[Ss]+)).*?[Pp]+[Ll]+(([Ee]+[Aa]+[Ss]+[Ee]+)|([Zz]+)|([Ss]+)).*?")
                            .Success)
                    {
                        SendChatMessage("https://www.youtube.com/watch?v=ODKTITUPusM", fromChannel);
                    }

                    /*------------------------- Built In Commands ------------------------*/
                }
            }
        }
    }
}

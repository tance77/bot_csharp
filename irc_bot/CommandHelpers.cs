using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class CommandHelpers : WebFunctions
    {
        public bool AddTimer (DatabaseFunctions db, string msg, string channel)
        {
            string[] msgArray = msg.Split (' ');
            msg = "";
            var actualMessage = new StringBuilder ();
            for (int i = 1; i < msgArray.Length; i++) {
                if (msgArray.Length > 2) {
                    if (i == msgArray.Length) {
                        actualMessage.Append (msgArray [i]);
                    } else {
                        actualMessage.Append (msgArray [i] + " ");
                    }
                } else {
                    actualMessage.Append (msgArray [i]);
                }
            }
            if (db.AddTimer (channel, actualMessage.ToString ())) {
                return true;
            }
            return false;
        }

        public string ChannelTimers (DatabaseFunctions db, string fromChannel)
        {
            Dictionary<int, string> channelTimerDict = db.GetTimers (fromChannel);
            if (channelTimerDict == null)
                return null;
            var message = new StringBuilder ();
            message.Append ("Timer list with ID's: ");
            foreach (var item in channelTimerDict) {
                message.Append (item.Value + " " + item.Key + " ");
            }
            return message.ToString ();
        }

        public int DiceRoll ()
        {
            var r = new Random ();
            int diceRoll = r.Next (1, 100);
            return diceRoll;
        }

        public bool Roulette (string channel)
        {
            var chamber = new Random ();
            int deathShot = chamber.Next (1, 3);
            int playerShot = chamber.Next (1, 3);
            if (deathShot == playerShot) {
                return true;
            }
            return false;
        }

        public string CheckSummonerName (string fromChannel, DatabaseFunctions db, RiotApi riotApi)
        {
            string summonerName = db.SummonerStatus (fromChannel);
            if (string.IsNullOrEmpty(summonerName))
                return "No Summoner Name";
            string summonerId = riotApi.GetSummonerId (summonerName);
            //GetRunes(summonerId);
            //
            if (summonerId.Length == 3)
                return summonerId;
            if (!db.SetSummonerId (fromChannel, summonerId))
                return "ERR Summoner ID";
            string rank = riotApi.GetRank (summonerId);
            return rank;
        }

        public string GetLeagueRank (string fromChannel, string msgSender, DatabaseFunctions db, RiotApi riotApi)
        {
            string result = CheckSummonerName (fromChannel, db, riotApi);
            switch (result) {
            case "No Summoner Name":
                return
                        "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]";
            case "401":
            case "400":
                return "Invalid Summoner Name";
            case "404":
                return fromChannel + " is not yet ranked.";
            case "429":
                return "To many requests at one time please try again.";
            case "503":
            case "500":
                return "Could not reach Riot API. Please try again in a few minutes.";
            default:
                return fromChannel + " is currently " + result;
            }
        }

        public string GetMasteries (TwitchMessage msg, string definedUser, RiotApi riotApi)
        {
            Dictionary<string, int> masteriesDictionary = riotApi.GetMasteries (msg, definedUser);
            if (masteriesDictionary == null) {
                return
                    "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]";
            }
            if (masteriesDictionary.Count() == 1)
            {
                return "Invalid summoner name.";
            }
            var message = new StringBuilder ();
            foreach (var tree in masteriesDictionary) {
                message.AppendFormat ("{0}: {1} ", tree.Key, tree.Value);
            }
            return message.ToString ();
        }


        public string SetSummonerName (string fromChannel, string summonerName, string msgSender, DatabaseFunctions db)
        {
            if (msgSender != fromChannel) {
                return "Insufficient privileges";
            }
            if (db.SetSummonerName (fromChannel, summonerName)) { // on success
                return "Summoner name has been set to " + summonerName;
            }
            return "Something went wrong on my end please try again";
        }

        public string SplitSummonerName (string message)
        {
            string[] msgParts = message.Split (' ');
            var summonerName = new StringBuilder ();
            for (int i = 1; i < msgParts.Length; i++) {
                if (i == msgParts.Length) {
                    summonerName.Append (msgParts [i]);
                } else {
                    summonerName.Append (msgParts [i] + " ");
                }
            }
            return summonerName.ToString ();
        }

        public string ParseRuneDictionary (Dictionary<string, int> runeDictionary)
        {
            string message = "";
            foreach (var name in runeDictionary) {
                if (name.Equals (runeDictionary.Last ())) {
                    message += name.Key + " x" + name.Value;
                    break;
                }
                message += name.Key + " x" + name.Value + " ";
            }
            return message;
        }

        public string GetChannelCommands (string fromChannel, DatabaseFunctions db)
        {
            List<string> commands = db.GetChannelCommands (fromChannel);
            if (commands == null || !commands.Any ()) {
                return "No commands were found for this channel.";
            }
            string sendString = "";
            for (int i = 0; i < commands.Count (); i++) {
                if (i == commands.Count () - 1) {
                    sendString += " !" + commands [i];
                } else {
                    sendString += " !" + commands [i] + ",";
                }
            }
            return "Commands are" + sendString;
        }

        public string AddCommand (string channel, string message, DatabaseFunctions db)
        {
            if (!message.StartsWith ("!")) {
                return null;
            }
            try {
                string[] messageArray = message.Split (' ');
                string command = messageArray [1].Split ('!') [1];
                string commandDescription = "";
                for (int i = 2; i < messageArray.Length; i++) {
                    if (i == messageArray.Length - 1) {
                        commandDescription += messageArray [i];
                    } else {
                        commandDescription += messageArray [i] + " ";
                    }
                }
                bool success = db.AddCommand (command, commandDescription, false, channel);
                command = "!" + command;
                if (success) {
                    return command + " was add successfully.";
                }
                return command + " command already exists use !editcom if you would like to change it.";
            } catch (IndexOutOfRangeException) {
                return "With no <> syntax is <!addcom> <!command_name> <response>";
            } catch (Exception e) {
                Console.Write (e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string RemoveCommand (string fromChannel, string message, DatabaseFunctions db)
        {
            if (!message.StartsWith ("!"))
                return null;
            try {
                string[] splitMessage = message.Split (' ');
                string command = splitMessage [1].Split ('!') [1];
                bool succes = db.RemoveCommand (command, fromChannel);
                command = "!" + command;
                if (succes) {
                    return command + " was deleted successfully.";
                }
                return command + " there is no such command.";
            } catch (IndexOutOfRangeException) {
                return "With no <> syntax is <!delcom> <!command_name>";
            } catch (Exception e) {
                Console.Write (e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string EditCommand (string fromChannel, string message, DatabaseFunctions db)
        {
            if (!message.StartsWith ("!")) {
                return null;
            }
            try {
                string[] splitMessage = message.Split (' ');
                string command = splitMessage [1].Split ('!') [1];
                string commandDescription = "";
                for (int i = 2; i < splitMessage.Length; i++) {
                    if (i == splitMessage.Length - 1) {
                        commandDescription += splitMessage [i];
                    } else
                        commandDescription += splitMessage [i] + " ";
                }
                bool success = db.EditCommand (command, commandDescription, false, fromChannel);
                command = "!" + command;
                if (success) {
                    return command + " was updated successfully.";
                }
                return command + " there is no such command. Use !addcom if you wish to add a command.";
            } catch (IndexOutOfRangeException) {
                return "With no <> syntax is <!editcom> <!command_name> <response>";
            } catch (Exception e) {
                Console.Write (e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string DickSize (string channel, string sender, DatabaseFunctions db)
        {
            string response = db.DickSize (channel);
            if (response == null) {
                return null;
            }
            return sender + ", " + response;
        }

        public string DickSizeToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.DickSizeToggle (channel, toggle);
            if (success) {
                return toggle ? "Dicksize is now on." : "Dicksize is now off.";
            }
            return "Something went wrong on my end.";
        }

        public string QueueToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.QueueToggle (channel, toggle);
            if (success) {
                return toggle ? "Game queue is now on." : "Game queue is now off.";
            }
            return "Something went wrong on my end.";
        }

        public string RegToggle (TwitchMessage message, bool toggle, DatabaseFunctions db)
        {
            bool success = db.ToggleRegularOnOff (message, toggle);
            if (success) {
                return toggle ? "Regulars are now on." : "Regulars are now off.";
            }
            return "Something went wrong on my end.";
        }


        public string EmoteToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.EmoteToggle (channel, toggle);
            if (success) {
                return toggle ? "Spam away emotes are fair game." : "Excessive emotes will be purged.";
            }
            return "Something went wrong on my end.";
        }

        public string AsciiToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.AsciiToggle (channel, toggle);
            if (success) {
                return toggle ? "Spam away ascii's are fair game." : "Excessive ascii characters will be purged";
            }
            return "Something went wrong on my end.";
        }


        public string SongRequestToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.SongRequestToggle (channel, toggle);
            if (success) {
                return toggle ? "Song Requests a now on." : "Song requests are now off.";
            }
            return "Something went wrong on my end.";
        }

        public string UrlToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.UrlToggle (channel, toggle);
            if (success) {
                return toggle ? "URL's are now allowed." : "URL's are no longer allowed.";
            }
            return "Something went wrong on my end.";
        }

        public string GgToggle (string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.GgToggle (channel, toggle);
            if (success) {
                return toggle ? "GG is now on." : "GG is now off.";
            }
            return "Something went wrong on my end.";
        }

        public bool CheckGg (string fromChannel, DatabaseFunctions db)
        {
            return db.GgStatus (fromChannel);
        }

        public string PermitUser (string fromChannel, string msgSender, string message, string userType,
                                  DatabaseFunctions db)
        {
            if (db.UrlStatus (fromChannel) || userType != "mod")
                return null;
            string[] msgArr = message.Split (' ');
            var userToPermit = new StringBuilder ();
            for (int i = 1; i < msgArr.Length; i++) {
                if (i == msgArr.Length) {
                    userToPermit.Append (msgArr [i]);
                } else {
                    userToPermit.Append (msgArr [i] + " ");
                }
            }
            if (userToPermit.ToString () == "")
                return null;
            bool success = db.PermitUser (fromChannel.ToLower ().Trim (' '), userToPermit.ToString ().ToLower ().Trim (' '));
            if (success) {
                return msgSender + " -> " + userToPermit +
                " can post 1 link anytime in the next 3 minutes and will not get timed out.";
            }
            return null;
        }


        public void GetFollowers (string fromChannel, DatabaseFunctions db, TwitchApi twitchApi)
        {
            List<string> followersList = db.ParseFirstHundredFollowers (fromChannel, twitchApi);
        }


        public string AssembleFollowerList (string fromChannel, DatabaseFunctions db, TwitchApi twitchApi)
        {
            List<string> followersList = db.ParseRecentFollowers (fromChannel, twitchApi);
            if (followersList == null)
                return null;
            var message = "";
            if (followersList.Count == 1) {
                message += followersList.First () + " thanks for following!";
                //message += "A new follower approaches " + followersList.First() + "!";

            } else {
                foreach (var item in followersList) {
                    if (item == followersList.Last ()) {
                        message += "and " + item + ", thank you for following!";
                        //message += "and " + item + "!";
                    } else {
                        message += item + ", ";
                        //message += "Multiple followers have appeared " + item + ", ";
                    }
                }
            }
            return message == "" ? null : message;
        }

        public void JoinAssembleFollowerList (string fromChannel, DatabaseFunctions db, TwitchApi twitchApi)
        {
            db.ParseRecentFollowers (fromChannel, twitchApi);
        }

        public bool CoinFlip ()
        {
            var r = new Random ();
            int a = r.Next (0, 2);
            return a == 0;
        }



        public void AddRegular (TwitchMessage Message, DatabaseFunctions db, IrcClient irc)
        {
            if (irc.WhisperServer)
                return;
            var userToAdd = Message.Msg.Split (' ') [2];
            var success = db.AddRegular (Message.FromChannel, userToAdd);
            if (success) {
                irc.AddPrivMsgToQueue (userToAdd + " " + "was added to the regular list by " + Message.MsgSender, Message.FromChannel);
            }
        }

        public void RemoveRegular (TwitchMessage message, DatabaseFunctions db, IrcClient irc)
        {
            if (irc.WhisperServer)
                return;
            var userToRemove = message.Msg.Split (' ') [2];
            var success = db.RemoveRegular (message.FromChannel, userToRemove);
            if (success) {
                irc.AddPrivMsgToQueue (userToRemove + " " + "is no longer a regular", message.FromChannel);
            }
        }



        public string AddToQueue (TwitchMessage msg, RiotApi riotApi, DatabaseFunctions db)
        {
            var summonerName = msg.Msg.Substring (msg.Msg.IndexOf (' ') + 1);
            var postion = 1;
            if (riotApi.GetSummonerId (summonerName) == null)
                return null;
            var leagueQueue = db.AddToQueue (msg, summonerName);
            if (leagueQueue != null) {
                var currentCount = leagueQueue.Count;
                foreach (var person in leagueQueue) {
                    if (person == msg.MsgSender) {
                        break;
                    }
                    postion++;
                }
                return msg.MsgSender + " you have been added to the queue. Your position in the queue is " + postion + ".";
            }
            return "Could not reach database please try again.";
        }

        public string CheckPostion (TwitchMessage msg, DatabaseFunctions db)
        {
            var leagueQueue = db.GetQueuePostion (msg);
            var postion = 1;
            var exists = false;
            if (leagueQueue != null && leagueQueue.Count != 0) {
                foreach (var person in leagueQueue) {
                    if (person == msg.MsgSender) {
                        exists = true;
                        break;
                    }
                    postion++;
                }
                if (exists) {
                    return msg.MsgSender + " you are number " + postion + " in queue.";
                }
                return msg.MsgSender + " you are not in the queue type \"!queue summonername\" to join the queue"; 
            }
            return null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class CommandFunctions : WebFunctions
    {

        public string GetStreamUptime(string fromChannel)
        {
            var url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            var jsonString = RequestJson(url);
            if (!JObject.Parse(jsonString).SelectToken("stream").HasValues)
            {
                return "Stream is offline.";
            }
            var createdAt = JObject.Parse(jsonString).SelectToken("stream").SelectToken("created_at").ToString();
            DateTime startedAt;
            if (!DateTime.TryParse(createdAt, out startedAt))
            {
                return "Could not reach Twitch API";
            }
            var nowTime = DateTime.Now;
            nowTime = nowTime.ToUniversalTime();
            var onlineForHours = (nowTime - startedAt).Hours;
            var onlineForMinutes = (nowTime - startedAt).Minutes;
            var onlineForSeconds = (nowTime - startedAt).Seconds;

            return fromChannel + " has been online for " + onlineForHours + " hours " + onlineForMinutes + " minutes " +
                   onlineForSeconds + " seconds";

        }

        public Timer AddTimer(string fromChannel, string message, int seconds, IrcClient irc)
        {
            var miliseconds = seconds*1000;
            return new Timer(miliseconds, message, fromChannel, irc);
        }

        public bool Roulette(string channel)
        {
            var chamber = new Random();
            var deathShot = chamber.Next(1, 3);
            var playerShot = chamber.Next(1, 3);
            if (deathShot == playerShot)
            {
                return true;
            }
            return false;

        }
        public string CheckSummonerName(string fromChannel, DatabaseFunctions db, RiotApi riotApi)
        {
            var summonerName = db.SummonerStatus(fromChannel);
            if (summonerName == "") return "No Summoner Name";
            var summonerId = riotApi.GetSummonerId(summonerName);
            //GetRunes(summonerId);
            if (summonerId == "400" || summonerId == "401" || summonerId == "404" || summonerId == "429" || summonerId == "500" || summonerId == "503") // Invalid summoner name
            {
                return summonerId;
            }
            if (!db.SetSummonerId(fromChannel, summonerId)) return "ERR Summoner ID";
            var rank = riotApi.GetRank(summonerId);
            if (rank == "400" || rank == "401" || rank == "404" || rank == "429" || rank == "500" || rank == "503") // Invalid summoner name
            {
                return rank;
            }
            return rank;
        }

        public string GetLeagueRank(string fromChannel, string msgSender, DatabaseFunctions db, RiotApi riotApi)
        {
            var result = CheckSummonerName(fromChannel, db, riotApi);
            switch (result)
            {
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

        public string GetMasteries(string fromChannel, RiotApi riotApi)
        {
            var masteriesDictionary = riotApi.GetMasteries(fromChannel);
            if (masteriesDictionary == null)
            {
                return "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]";
            }
            var message = new StringBuilder();
            foreach (var tree in masteriesDictionary)
            {
                message.AppendFormat("{0}: {1} ", tree.Key, tree.Value);
            }
            return message.ToString();
        }


        public string SetSummonerName(string fromChannel, string summonerName, string msgSender, DatabaseFunctions db)
        {
            if (msgSender != fromChannel) { return "Insufficient privileges"; }
            if (db.SetSummonerName(fromChannel, summonerName))// on success
            {
                return "Summoner name has been set to " + summonerName;
            }
            return "Something went wrong on my end please try again";
        }

        public string SplitSummonerName(string message)
        {
            var msgParts = message.Split(' ');
            var summonerName = new StringBuilder();
            for (int i = 1; i < msgParts.Length; i++)
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

        public string ParseRuneDictionary(Dictionary<string, int> runeDictionary)
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
            return message;
        }

        public string GetChannelCommands(string fromChannel, DatabaseFunctions db)
        {
            var commands = db.GetChannelCommands(fromChannel);
            if (commands == null || !commands.Any())
            {
                return "No commands were found for this channel.";
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
            return "Commands are" + sendString;
        }

        public string AddCommand(string channel, string message, DatabaseFunctions db)
        {
            if (!message.StartsWith("!")) { return null; }
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
                var success = db.AddCommand(command, commandDescription, false, channel);
                command = "!" + command;
                if (success)
                {
                    return command + " was add successfully.";
                }
                return command + " command already exists use !editcom if you would like to change it.";
            }
            catch (IndexOutOfRangeException)
            {
                return "With no <> syntax is <!addcom> <!command_name> <response>";
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string RemoveCommand(string fromChannel, string message, DatabaseFunctions db)
        {
            if (!message.StartsWith("!")) return null;
            try
            {
                var splitMessage = message.Split(' ');
                var command = splitMessage[1].Split('!')[1];
                var succes = db.RemoveCommand(command, fromChannel);
                command = "!" + command;
                if (succes)
                {
                    return command + " was deleted successfully.";
                }
                return command + " there is no such command.";
            }
            catch (IndexOutOfRangeException)
            {
                return "With no <> syntax is <!delcom> <!command_name>";
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string EditCommand(string fromChannel, string message, DatabaseFunctions db)
        {

            if (!message.StartsWith("!")) { return null; }
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
                var success = db.EditCommand(command, commandDescription, false, fromChannel);
                command = "!" + command;
                if (success)
                {
                    return command + " was updated successfully.";
                }
                return command + " there is no such command. Use !addcom if you wish to add a command.";
            }
            catch (IndexOutOfRangeException)
            {
                return "With no <> syntax is <!editcom> <!command_name> <response>";
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return "Sorry something went wrong on my end, please try again.";
            }
        }

        public string DickSize(string channel, string sender, DatabaseFunctions db)
        {
            var response = db.DickSize(channel);
            if (response == null) { return null; }
            return sender + ", " + response;
        }

        public string DickSizeToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            var success = db.DickSizeToggle(channel, toggle);
            if (success)
            {
                return toggle ? "Dicksize is now on." : "Dicksize is now off.";
            }
            return "Something went wrong on my end.";
        }

        public string UrlToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            var success = db.UrlToggle(channel, toggle);
            if (success)
            {
                return toggle ? "URL's are now allowed." : "URL's are no longer allowed.";
            }
            return "Something went wrong on my end.";
        }

        public string GgToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            var success = db.GgToggle(channel, toggle);
            if (success)
            {
                return toggle ? "GG is now on." : "GG is now off.";
            }
               return "Something went wrong on my end.";
        }
        public bool CheckGg(string fromChannel, DatabaseFunctions db)
        {
            return db.GgStatus(fromChannel);
        }

        public string PermitUser(string fromChannel, string msgSender, string message, string userType, DatabaseFunctions db)
        {
            if(db.UrlStatus(fromChannel) || userType != "mod") return null;
            var msgArr = message.Split(' ');
            var userToPermit = new StringBuilder();
            for (var i = 1; i < msgArr.Length; i++)
            {
                if (i == msgArr.Length)
                {
                    userToPermit.Append(msgArr[i]);
                }
                else
                {
                    userToPermit.Append(msgArr[i] + " ");
                }
            }
            var success = db.PermitUser(fromChannel.ToLower().Trim(' '), userToPermit.ToString().ToLower().Trim(' '));
            if (success)
            {
                return msgSender + " -> " + userToPermit + " can post 1 link anytime in the next 3 minutes and will not get timed out.";
            }
            return null;
        }
    }
}

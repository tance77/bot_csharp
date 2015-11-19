using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SpotifyAPI.Web.Models;

namespace twitch_irc_bot
{
    internal class CommandHelpers : WebFunctions
    {
        public bool AddTimer(DatabaseFunctions db, string msg, string channel)
        {
            string[] msgArray = msg.Split(' ');
            msg = "";
            var actualMessage = new StringBuilder();
            for (int i = 1; i < msgArray.Length; i++)
            {
                if (msgArray.Length > 2)
                {
                    if (i == msgArray.Length)
                    {
                        actualMessage.Append(msgArray[i]);
                    }
                    else
                    {
                        actualMessage.Append(msgArray[i] + " ");
                    }
                }
                else
                {
                    actualMessage.Append(msgArray[i]);
                }
            }
            if (db.AddTimer(channel, actualMessage.ToString()))
            {
                return true;
            }
            return false;
        }

        public string ChannelTimers(DatabaseFunctions db, string fromChannel)
        {
            Dictionary<int, string> channelTimerDict = db.GetTimers(fromChannel);
            if (channelTimerDict == null) return null;
            var message = new StringBuilder();
            message.Append("Timer list with ID's: ");
            foreach (var item in channelTimerDict)
            {
                message.Append(item.Value + " " + item.Key + " ");
            }
            return message.ToString();
        }

        public int DiceRoll()
        {
            var r = new Random();
            int diceRoll = r.Next(1, 100);
            return diceRoll;
        }

        public bool Roulette(string channel)
        {
            var chamber = new Random();
            int deathShot = chamber.Next(1, 3);
            int playerShot = chamber.Next(1, 3);
            if (deathShot == playerShot)
            {
                return true;
            }
            return false;
        }

        public string CheckSummonerName(string fromChannel, DatabaseFunctions db, RiotApi riotApi)
        {
            string summonerName = db.SummonerStatus(fromChannel);
            if (summonerName == "") return "No Summoner Name";
            string summonerId = riotApi.GetSummonerId(summonerName);
            //GetRunes(summonerId);
            if (summonerId == "400" || summonerId == "401" || summonerId == "404" || summonerId == "429" ||
            summonerId == "500" || summonerId == "503") // Invalid summoner name
            {
                return summonerId;
            }
            if (!db.SetSummonerId(fromChannel, summonerId)) return "ERR Summoner ID";
            string rank = riotApi.GetRank(summonerId);
            if (rank == "400" || rank == "401" || rank == "404" || rank == "429" || rank == "500" || rank == "503")
            // Invalid summoner name
            {
                return rank;
            }
            return rank;
        }

        public string GetLeagueRank(string fromChannel, string msgSender, DatabaseFunctions db, RiotApi riotApi)
        {
            string result = CheckSummonerName(fromChannel, db, riotApi);
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
            Dictionary<string, int> masteriesDictionary = riotApi.GetMasteries(fromChannel);
            if (masteriesDictionary == null)
            {
                return
                "No summoner name linked to this twitch channel. To enable this feature channel owner please type !setsummoner [summonername]";
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
            if (msgSender != fromChannel)
            {
                return "Insufficient privileges";
            }
            if (db.SetSummonerName(fromChannel, summonerName)) // on success
            {
                return "Summoner name has been set to " + summonerName;
            }
            return "Something went wrong on my end please try again";
        }

        public string SplitSummonerName(string message)
        {
            string[] msgParts = message.Split(' ');
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
            List<string> commands = db.GetChannelCommands(fromChannel);
            if (commands == null || !commands.Any())
            {
                return "No commands were found for this channel.";
            }
            string sendString = "";
            for (int i = 0; i < commands.Count(); i++)
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
            if (!message.StartsWith("!"))
            {
                return null;
            }
            try
            {
                string[] messageArray = message.Split(' ');
                string command = messageArray[1].Split('!')[1];
                string commandDescription = "";
                for (int i = 2; i < messageArray.Length; i++)
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
                bool success = db.AddCommand(command, commandDescription, false, channel);
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
                string[] splitMessage = message.Split(' ');
                string command = splitMessage[1].Split('!')[1];
                bool succes = db.RemoveCommand(command, fromChannel);
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
            if (!message.StartsWith("!"))
            {
                return null;
            }
            try
            {
                string[] splitMessage = message.Split(' ');
                string command = splitMessage[1].Split('!')[1];
                string commandDescription = "";
                for (int i = 2; i < splitMessage.Length; i++)
                {
                    if (i == splitMessage.Length - 1)
                    {
                        commandDescription += splitMessage[i];
                    }
                    else commandDescription += splitMessage[i] + " ";
                }
                bool success = db.EditCommand(command, commandDescription, false, fromChannel);
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
            string response = db.DickSize(channel);
            if (response == null)
            {
                return null;
            }
            return sender + ", " + response;
        }

        public string DickSizeToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.DickSizeToggle(channel, toggle);
            if (success)
            {
                return toggle ? "Dicksize is now on." : "Dicksize is now off.";
            }
            return "Something went wrong on my end.";
        }
        public string SongRequestToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.SongRequestToggle(channel, toggle);
            if (success)
            {
                return toggle ? "Song Request is now on." : "Songrequest is now off.";
            }
            return "Something went wrong on my end.";
        }

        public string UrlToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.UrlToggle(channel, toggle);
            if (success)
            {
                return toggle ? "URL's are now allowed." : "URL's are no longer allowed.";
            }
            return "Something went wrong on my end.";
        }

        public string GgToggle(string channel, bool toggle, DatabaseFunctions db)
        {
            bool success = db.GgToggle(channel, toggle);
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

        public string PermitUser(string fromChannel, string msgSender, string message, string userType,
        DatabaseFunctions db)
        {
            if (db.UrlStatus(fromChannel) || userType != "mod") return null;
            string[] msgArr = message.Split(' ');
            var userToPermit = new StringBuilder();
            for (int i = 1; i < msgArr.Length; i++)
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
            if (userToPermit.ToString() == "") return null;
            bool success = db.PermitUser(fromChannel.ToLower().Trim(' '), userToPermit.ToString().ToLower().Trim(' '));
            if (success)
            {
                return msgSender + " -> " + userToPermit +
                " can post 1 link anytime in the next 3 minutes and will not get timed out.";
            }
            return null;
        }

        public string AssembleFollowerList(string fromChannel, DatabaseFunctions db, TwitchApi twitchApi)
        {
            List<string> followersList = db.ParseRecentFollowers(fromChannel, twitchApi);
            if (followersList == null) return null;
            var message = "/me ";
            if (followersList.Count == 1)
            {
                message += followersList.First() + " thanks for following!";
            }
            else
            {
                foreach (string item in followersList)
                {
                    if (item == followersList.Last())
                    {
                        message += "and " + item + ", thank you for following!";
                    }
                    else
                    {
                        message += item + ", ";
                    }
                }
            }
            return message == "/me " ? null : message;
        }

        public void JoinAssembleFollowerList(string fromChannel, DatabaseFunctions db, TwitchApi twitchApi)
        {
            db.ParseRecentFollowers(fromChannel, twitchApi);
        }

        public bool CoinFlip()
        {
            var r = new Random();
            int a = r.Next(0, 2);
            return a == 0;
        }

        public Dictionary<string, KeyValuePair<string,string>> SearchSongByName(string queryString)
        {
            var songArtists = "";
            var songId = "";
            var songTitle = "";

            var foundSongs = new Dictionary<string, KeyValuePair<string,string>>() ;

            Console.WriteLine(queryString);


            var response = RequestJson(queryString);
            if (response == "400" || response == "401" || response == "404" || response == "429" ||
            response == "500" || response == "503")
            {
                return null;
            }

            JToken jsonArr = JObject.Parse(response).SelectToken("tracks").SelectToken("items");

            //grab the first result

            if (!jsonArr.HasValues)
                return null;

            var k = 0;
            foreach(var song in jsonArr){
                if(k >= 3){
                    break;
                }
                var availableMarketsAry = song.SelectToken("available_markets");
                var validCountry = false;
                foreach (var country in availableMarketsAry)
                {
                    if (country.ToString() != "US") continue;
                    validCountry = true;
                    break;
                }
                //not playable in us
                if (!validCountry) return null;

                songTitle = song.SelectToken("name").ToString();
                songId = song.SelectToken("id").ToString();
                songArtists = "";
                var artistAry = song.SelectToken("artists").ToArray();

                for (var i = 0; i < artistAry.Length; i++)
                {
                    if (i == artistAry.Length - 1)
                    {
                        songArtists += artistAry[i].SelectToken("name");
                    }
                    else
                    {
                        songArtists += artistAry[i].SelectToken("name") + " and ";
                    }
                }

                var pair = new KeyValuePair<string, string>(songTitle, songArtists);
                foundSongs.Add(songId, pair);
                k++;

            }

            return foundSongs;
        }

        public string AddSongById(string songId, DatabaseFunctions db, string fromChannel, string messageSender)
        {


            const string baseUrl = "https://api.spotify.com/v1";
            var foundSong = "";
            var songDuration = "";
            var songAlbumUrl = "";
            var songArtists = "";
            var songUrl = "";
            var songTitle = "";

            //track ID only look up by track id
                //look up by track id here

            var requestUrl = baseUrl + "/tracks/" + songId;

                var response = RequestJson(requestUrl);
                if (response == "400" || response == "401" || response == "404" || response == "429" ||
                    response == "500" || response == "503")
                {
                    return "song not found.";
                }
                JToken jsonArr = JObject.Parse(response);
                if (!jsonArr.HasValues)
                {
                    return "song not found.";
                }

                var availableMarketsAry = jsonArr.SelectToken("available_markets");
                var validCountry = false;
                foreach (var country in availableMarketsAry)
                {
                    if (country.ToString() != "US") continue;
                    validCountry = true;
                    break;
                }
                //not playable in us
                if (!validCountry) return "song not found.";

                songAlbumUrl = jsonArr.SelectToken("album").SelectToken("images")[0].SelectToken("url").ToString();
                var artistAry = jsonArr.SelectToken("artists").ToArray();
                songArtists = "";
                for (var i = 0; i < artistAry.Length; i++)
                {
                    if (i == artistAry.Length - 1)
                    {
                        songArtists += artistAry[i].SelectToken("name");
                    }
                    else
                    {
                        songArtists += artistAry[i].SelectToken("name") + " and ";
                    }
                }

                var miliseconds = Int32.Parse(jsonArr.SelectToken("duration_ms").ToString());

                var a = miliseconds / 1000;
                var minutes = a / 60;
                a = a % 60;

                var seconds = a.ToString();

                if (a < 10)
                {
                    seconds = "0" + a;
                }

                songDuration = minutes + ":" + seconds;

                songUrl = jsonArr.SelectToken("external_urls").SelectToken("spotify").ToString();
                songId = jsonArr.SelectToken("id").ToString();
                songTitle = jsonArr.SelectToken("name").ToString();

                Console.Write("\r\n" +
                              "ID " + songId + "\r\n" +
                              "Title " + songTitle + "\r\n" +
                              "Artists " + songArtists + "\r\n" +
                              "Album Url " + songAlbumUrl + "\r\n" +
                              "Duration " + songDuration + "\r\n" +
                              "Song Url " + songUrl + "\r\n" + "\r\n");
                foundSong = songTitle + " by " + songArtists + " was added to the playlist";
            var succuess = db.AddSong(fromChannel, messageSender, songId, songDuration, songArtists, songTitle, songUrl, songAlbumUrl);
            if (succuess)
            {
                return foundSong;
            }
            return "song already exists or something went wrong on my end.";

        }

        public List<string> SearchSong(string message, string messageSender, DatabaseFunctions db, string fromChannel)
        {
            //Get multiple results
            //message user the results
            //expect response back from user within a time period
            //Possible listener for that user
            //Act on response

            const string baseUrl = "https://api.spotify.com/v1";
            var foundSong = "";
            var songDuration = "";
            var songAlbumUrl = "";
            var songArtists = "";
            var songUrl = "";
            var songId = "";
            var songTitle = "";
			var songList = new List<string>();
            if (message.StartsWith("!songrequest ") || message.StartsWith("!sr "))
            {
                var msgAry = message.Split(' ');
                var queryString = "";


                for (var i = 1; i < msgAry.Length; i++)
                {
                    queryString += msgAry[i] + " ";
                }
                queryString = queryString.Trim(' ');

                //song track URL
                if (Regex.Match(queryString, @"^https:\/\/.*com\/track\/").Success)
                {
                    queryString = queryString.Split('/')[4];
                }

                //song URI Request
                if (queryString.Length == 36 && queryString.Contains("spotify:track:"))
                {
                    queryString = queryString.Split(':')[2];
                }

                //don't allow request by album
                if (queryString.Length == 36 && queryString.Contains("spotify:album:") ||
                Regex.Match(message, @"^https:\/\/.*com\/album\/").Success)
                {
					songList.Add("sorry you can't request a whole album.");
					return songList;
				}

                //don't allow request by artist
                if (queryString.Length == 37 && queryString.Contains("spotify:artist:") ||
                Regex.Match(message, @"^https:\/\/.*com\/artist\/").Success)
                {
					songList.Add("Sorry you can't request an artist");
					return songList;
				}

                //don't allow youtube requests
                const string pattern =
                @"^(?:https?:\/\/|\/\/)?(?:www\.|m\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})(?![\w-])";

                if (Regex.Match(message, pattern).Success)
                {
					songList.Add("sorry we are using Spotify not YouTube.");
					return songList;
				}

                //track ID only look up by track id
                if (!queryString.Contains(" ") && queryString.Length == 22)
                {
					songList.Add(AddSongById(queryString, db, fromChannel, messageSender));
					return songList;
				}
                    //If we are searching for a track with words do below

                    string requestUrl = baseUrl + "/search?type=track&offset=0&limit=20&market=US&q=" + queryString;
                    var multipleResults = SearchSongByName(requestUrl);
                    if (multipleResults == null || multipleResults.Count == 0)
                    {
					songList.Add("Song not found.");
					return songList;    
				}

                    if (multipleResults.Count > 1)
                    {


                        foreach (var song in multipleResults)
                        {
                            Console.WriteLine(song);
						songList.Add(song.Value.Key + " - " + song.Value.Value + "" +
							" [Track Id]: => " + song.Key);
                        }
                        return songList;
                    }
				songList.Add(AddSongById(multipleResults.First().Key, db, fromChannel, messageSender));
				return songList;    
			}
            var succuess = db.AddSong(fromChannel, messageSender, songId, songDuration, songArtists, songTitle, songUrl, songAlbumUrl);
            if (succuess)
            {
				songList.Add(foundSong);
				return songList;
			}
			songList.Add("song already exists or something went wrong on my end.");
			return songList;
		}
    }
}

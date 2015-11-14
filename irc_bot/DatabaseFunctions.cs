using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace twitch_irc_bot
{
    internal class DatabaseFunctions
    {
        private const string ConnectionString =
        "Persist Security Info=False;" +
        "Server=192.241.219.172;" +
        "Port=3306;" +
        "Database=chinnbot;" +
        "Uid=me;" +
        "Pwd=GUM5fLtzuHPq;";

        public string DateTimeSqLite(DateTime datetime)
        {
            const string dateTimeFormat = "{0}-{1}-{2} {3}:{4}:{5}.{6}";
            return string.Format(dateTimeFormat, datetime.Year, datetime.Month, datetime.Day, datetime.Hour,
            datetime.Minute, datetime.Second, datetime.Millisecond);
        }

        public List<string> JoinChannels()
        {
            var channelsToJoin = new List<string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Channels GROUP BY channel_name",
                    dbConnection))
                    {
                        using (MySqlDataReader dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            channelsToJoin.Add(dr.GetValue(0).ToString());
                        }
                        GC.Collect();
                        return channelsToJoin;
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return channelsToJoin;
            }
        }


        public bool AddToChannels(string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var inDb = new MySqlCommand("select * from Channels where channel_name=@channel", dbConnection)
                    )
                    {
                        inDb.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = inDb.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return false;
                            }
                        }
                        GC.Collect();
                    }
                    using (var cmd =
                    new MySqlCommand(
                    "INSERT INTO Channels(channel_name,allow_urls,dicksize,gg,eight_ball,gameq)VALUES(@channel,@urls,@dicksize,@gg,@eight_ball,@gameq)",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@urls", true);
                        cmd.Parameters.AddWithValue("@dicksize", false);
                        cmd.Parameters.AddWithValue("@gg", false);
                        cmd.Parameters.AddWithValue("@eight_ball", false);
                        cmd.Parameters.AddWithValue("@gameq", false);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd =
                    new MySqlCommand(
                    "INSERT INTO League(channel_name,summoner_name,summoner_id)VALUES(@channel,@summoner,@summoner_id)",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@summoner", null);
                        cmd.Parameters.AddWithValue("@summoner_id", null);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool RemoveFromChannels(string channel)
        {
            try
            {
                using (
                var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var inDb = new MySqlCommand("select * from Channels where channel_name=@channel", dbConnection))
                    {
                        inDb.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = inDb.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return false;
                            }
                        }
                        GC.Collect();
                    }
                    using (
                    var cmd = new MySqlCommand("delete from Channels where channel_name=@channel", dbConnection)
                    )
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    using (
                    var cmd = new MySqlCommand("delete from Spotify where channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    using (
                    var cmd = new MySqlCommand("delete from League WHERE channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public Tuple<string, bool> MatchCommand(string matchCommand, string channel)
        {
            matchCommand = matchCommand.Split('!')[1];
            string description = null;
            bool toUser = false;
            try
            {
                using (
                var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var cmd = new MySqlCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string fetchedCommand = reader.GetString(2);
                                if (fetchedCommand != matchCommand) continue;
                                description = reader.GetString(1);
                                toUser = reader.GetBoolean(3);
                                break;
                            }
                        }
                    }
                    GC.Collect();
                    return new Tuple<string, bool>(description, toUser);
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public List<string> GetChannelCommands(string channel)
        {
            var commands = new List<string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var cmd = new MySqlCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                commands.Add(reader.GetString(2));
                            }
                        }
                        GC.Collect();
                        return commands;
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool AddCommand(string command, string commandDescription, bool toUser, string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var cmd = new MySqlCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (command == reader.GetString(2))
                                {
                                    return false;
                                }
                            }
                        }
                        GC.Collect();
                    }
                    using (var cmd =
                    new MySqlCommand(
                    "INSERT INTO Commands(channel_name, description, command, to_user)VALUES(@channel, @description, @command, @to_user)",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@description", commandDescription);
                        cmd.Parameters.AddWithValue("@command", command);
                        cmd.Parameters.AddWithValue("@to_user", toUser);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool EditCommand(string command, string commandDescription, bool toUser, string channel)
        {
            try
            {
                using (
                var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var cmd = new MySqlCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        bool okToEdit = false;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (command != reader.GetString(2)) continue;
                                okToEdit = true;
                                break;
                            }
                        }
                        GC.Collect();

                        if (!okToEdit)
                        {
                            return false;
                        }
                    }
                    using (var cmd =
                    new MySqlCommand(
                    "UPDATE Commands SET channel_name=@channel, description=@description, to_user=@to_user WHERE channel_name=@channel AND command=@command",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@description", commandDescription);
                        cmd.Parameters.AddWithValue("@command", command);
                        cmd.Parameters.AddWithValue("@to_user", toUser);
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool RemoveCommand(string command, string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var cmd = new MySqlCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        bool okToDelete = false;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (command != reader.GetString(2)) continue;
                                okToDelete = true;
                                break;
                            }
                        }
                        GC.Collect();
                        if (!okToDelete)
                        {
                            return false;
                        }
                    }
                    using (var cmd =
                    new MySqlCommand(
                    "DELETE FROM Commands WHERE channel_name=@channel and command=@command",
                    dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@command", command);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool DickSizeToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command =
                new MySqlCommand("UPDATE Channels SET dicksize=@dick_size WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@dick_size", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public bool UrlToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command = new MySqlCommand("UPDATE Channels SET allow_urls=@urls WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@urls", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public bool UrlStatus(string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        bool result = false;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = reader.GetBoolean(1);
                            }
                        }
                        GC.Collect();
                        return result;
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public string SummonerStatus(string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM League WHERE channel_name=@channel",
                    dbConnection)
                    )
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        string result = null;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = reader.GetValue(2).ToString();
                            }
                        }
                        GC.Collect();
                        return result;
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool SetSummonerName(string channel, string summonerName)
        {
            summonerName = summonerName.Trim(' ');
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();

                using (
                var command =
                new MySqlCommand("UPDATE League SET summoner_name=@summoner_name WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@summoner_name", summonerName);
                        command.ExecuteNonQuery();
                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public string GetSummonerId(string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM League WHERE channel_name=@channel",
                    dbConnection)
                    )
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        string summonerId = "";
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                summonerId = reader.GetValue(1).ToString();
                            }
                        }
                        GC.Collect();
                        return summonerId == "" ? null : summonerId;
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool SetSummonerId(string channel, string summonerId)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command =
                new MySqlCommand("UPDATE League SET summoner_id=@summoner_id WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@summoner_id", summonerId);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public bool GgStatus(string channel)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read() && reader.GetBoolean(3);
                        }
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool GgToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command = new MySqlCommand("UPDATE Channels SET gg=@gg WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@gg", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }
        public bool SongRequestToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command = new MySqlCommand("UPDATE Channels SET song_request=@sr WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@sr", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public string DickSize(string channel)
        {
            var randRange = new Random();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read() && reader.GetBoolean(2) == false)
                            {
                                return null;
                            }
                        }
                        GC.Collect();
                    }
                    var listOfResponses = new List<String>();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Dicksizes", dbConnection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            if (reader.Read())
                            {
                                listOfResponses.Add(reader.GetString(0));
                            }
                        }
                        GC.Collect();
                        int randOne = randRange.Next(1, listOfResponses.Count);
                        Console.Write(listOfResponses[randOne] + "\r\n");
                        return listOfResponses[randOne];
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool CheckPermitStatus(string fromChannel, string user)
        //Checks to see if the permit is still valid in the given time period
        {
            user = user.ToLower();
            user = user.Trim(' ');
            string startTime = "";
            string endTime = "";
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand(
                    "SELECT * FROM ChannelUsers WHERE channel_name=@channel and user=@msg_sender",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@msg_sender", user);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                startTime = reader.GetString(3);
                                endTime = reader.GetString(4);
                            }
                        }
                        GC.Collect();
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
            DateTime permitedAt;
            if (!DateTime.TryParse(startTime, out permitedAt))
            {
                return false;
            }
            DateTime permitEnds;
            if (!DateTime.TryParse(endTime, out permitEnds))
            {
                return false;
            }
            int minutesTillExpire = (permitEnds - permitedAt).Minutes;
            int secondsTillExpire = (permitEnds - permitedAt).Seconds;
            if (minutesTillExpire == 0 && secondsTillExpire <= 0)
            {
                return false;
            }
            return true;
        }

        public bool PermitExist(string fromChannel, string user)
        {
            user = user.ToLower();
            user = user.Trim(' ');
            try
            {
                using (
                var dbConnection =
                new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand(
                    "SELECT * FROM ChannelUsers WHERE channel_name=@channel and user=@msg_sender",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@msg_sender", user);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                        }
                        GC.Collect();
                        return true;
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool PermitUser(string fromChannel, string permitedUser)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand(
                    "INSERT INTO ChannelUsers(channel_name, user, permitted_at, permit_expires_at)VALUES(@channel, @user, @permit, @expires)",
                    dbConnection))
                    {
                        string permit = DateTimeSqLite(DateTime.Now);
                        string expires = DateTimeSqLite((DateTime.Now.AddMinutes(3)));
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", permitedUser);
                        command.Parameters.AddWithValue("@permit", permit);
                        command.Parameters.AddWithValue("@expires", expires);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public void RemovePermit(string fromChannel, string user)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand(
                    "delete from ChannelUsers where channel_name=@channel AND user=@user", dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", user);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
            }
        }

        public Dictionary<string, List<string>> GetTimmedMessages()
        {
            var timedMessages = new Dictionary<string, List<string>>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand("SELECT * FROM TimedMessages", dbConnection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (timedMessages.ContainsKey(reader.GetString(0)))
                                {
                                    timedMessages[reader.GetString(0)].Add(reader.GetString(1));
                                }
                                else
                                {
                                    timedMessages.Add(reader.GetString(0), new List<string>());
                                    timedMessages[reader.GetString(0)].Add(reader.GetString(1));
                                }
                            }
                        }
                    }
                }
                GC.Collect();
                return timedMessages;
            }
            catch (Exception e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public List<string> ParseRecentFollowers(string fromChannel, TwitchApi twitchApi)
        {
            var followersList = new List<string>();
            Dictionary<string, DateTime> followersDictionary = twitchApi.GetRecentFollowers(fromChannel);
            if (followersDictionary != null)
            {
                try
                {
                    using (var dbConnection = new MySqlConnection(ConnectionString))
                    {
                        dbConnection.Open();
                        foreach (var follower in followersDictionary)
                        {
                            using (
                                var command =
                                    new MySqlCommand(
                                        "SELECT * FROM Followers WHERE follower_name=@follower and channel_name=@channel",
                                        dbConnection))
                            {
                                command.Parameters.AddWithValue("@channel", fromChannel);
                                command.Parameters.AddWithValue("@follower", follower.Key);
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        continue;
                                    }
                                }
                            }
                            GC.Collect();
                            using (
                                var command =
                                    new MySqlCommand(
                                        "INSERT INTO Followers(channel_name,follower_name,follow_date)VALUES(@channel,@follower,@date)",
                                        dbConnection)
                                )
                            {
                                command.Parameters.AddWithValue("@channel", fromChannel);
                                command.Parameters.AddWithValue("@follower", follower.Key);
                                command.Parameters.AddWithValue("@date", follower.Value.ToUniversalTime());
                                command.ExecuteNonQuery();
                                followersList.Add(follower.Key);
                            }
                        }
                    }
                    //Console.Write(followersList + "\r\n");
                    return followersList;
                }
                catch
                    (MySqlException e)
                {
                    Console.Write(e + "\r\n");
                    return null;
                }
                catch (TimeoutException e)
                {
                    Console.Write(e + "\r\n");
                    return null;
                }
            }
            return null;
        }

        public List<string> GetListOfChannels()
        {
            var channelsList = new List<string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (var command = new MySqlCommand("SELECT * FROM Channels", dbConnection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string channel = reader.GetString(0);
                                if (channel != "chinnbot")
                                {
                                    channelsList.Add(channel);
                                }
                            }
                        }
                    }
                    GC.Collect();
                    return channelsList;
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public void AddCoins(int numberOfCoins, string channel, List<string> userList)
        {
            if (userList == null) return;
            if (userList.Contains("chinnbot"))
            {
                userList.Remove("chinnbot"); //gets rid of the bot from the list
            }
            var updateList = new List<string>();
            List<string> insertList = userList.ToList();

            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM ChinnCoins WHERE channel_name=@channel",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                updateList.Add(reader.GetString(1));
                            }
                        }
                    }
                    GC.Collect();
                    foreach (string person in userList)
                    {
                        if (updateList.Contains(person))
                        {
                            insertList.Remove(person);
                        }
                    }
                    foreach (string person in insertList)
                    {
                        using (
                        var command =
                        new MySqlCommand(
                        "INSERT INTO ChinnCoins(channel_name,user,chinn_coins)VALUES(@channel,@user,@chinn_coins)",
                        dbConnection))
                        {
                            command.Parameters.AddWithValue("@channel", channel);
                            command.Parameters.AddWithValue("@user", person);
                            command.Parameters.AddWithValue("@chinn_coins", numberOfCoins);
                            command.ExecuteNonQuery();
                        }
                        GC.Collect();
                    }
                    foreach (string person in updateList)
                    {
                        using (
                        var command =
                        new MySqlCommand(
                        "UPDATE ChinnCoins SET channel_name=@channel, user=@user, chinn_coins = chinn_coins + @chinn_coins WHERE channel_name=@channel AND user=@user",
                        dbConnection))
                        {
                            command.Parameters.AddWithValue("@channel", channel);
                            command.Parameters.AddWithValue("@user", person);
                            command.Parameters.AddWithValue("@chinn_coins", numberOfCoins);
                            command.ExecuteNonQuery();
                        }
                        GC.Collect();
                    }
                }
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
            }
        }

        public bool AddTimer(string fromChannel, string message)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command =
                    new MySqlCommand(
                    "INSERT INTO TimedMessages(channel_name,message)VALUES(@channel, @msg)",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@msg", message);
                        command.ExecuteNonQuery();
                    }
                }
                GC.Collect();
                return true;
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public Dictionary<int, string> GetTimers(string fromChannel)
        {
            var channelTimersDict = new Dictionary<int, string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM TimedMessages WHERE channel_name=@channel",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string[] msgSample = reader.GetString(1).Split(' ');
                                channelTimersDict.Add(reader.GetInt32(2), msgSample[0]);
                            }
                        }
                    }
                }
                return channelTimersDict;
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool CheckSongRequestStatus(string channelName){
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                var command = new MySqlCommand("Select * From Channels WHERE channel_name=@channel",
                dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channelName);
                        command.ExecuteNonQuery();
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader.GetBoolean(4);
                            }
                            return false;
                        }
                    }
                    catch (MySqlException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        //1 of the same song check to see if the same sone exists before adding Lets avoid dupilcates

        public bool AddSong(string channelName, string requestedBy, string songId, string duration, string artist,
        string title, string url, string albumUrl)
        {
            var exists = false;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();

                    using (var command = new MySqlCommand("SELECT * FROM Songs WHERE channel_name=@c AND song_id=@s", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", channelName);
                        command.Parameters.AddWithValue("@s", songId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // if we found a value that means it exits and we don't want ot add a duplicate
                            while (reader.Read())
                            {
                                exists = true;
                                break;
                            }
                        }
                    }
                    GC.Collect();

                    if (!exists)
                    {
                        using (
                        var command =
                        new MySqlCommand(
                        "insert into Songs (channel_name,requested_by,song_id,duration,artist,title,url, album_url) Values(@c, @rb, @s, @d, @a, @t, @u, @au)",
                        dbConnection))
                        {
                            command.Parameters.AddWithValue("@c", channelName);
                            command.Parameters.AddWithValue("@rb", requestedBy);
                            command.Parameters.AddWithValue("@s", songId);
                            command.Parameters.AddWithValue("@d", duration);
                            command.Parameters.AddWithValue("@a", artist);
                            command.Parameters.AddWithValue("@t", title);
                            command.Parameters.AddWithValue("@u", url);
                            command.Parameters.AddWithValue("@au", albumUrl);
                            command.ExecuteNonQuery();
                        }
                        GC.Collect();
                        return true;
                    }
                }
                //if we here the song was a duplicate or there weas an error
                return false;
            }
            catch (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return false;

            }
        }
        public string GetCurrentSong(string channel)
        {
            var song = "";
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Songs Where channel_name=@channel LIMIT 1",
                    dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        using (MySqlDataReader dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            song += dr.GetString(6) + " - " + dr.GetString(5);
                        }
                        GC.Collect();
                        return song;
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return song;
            }
        }
        public List<string> GetListOfActiveChannels()
        {
            var listOfActiveChannels = new List<string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                    var command = new MySqlCommand("SELECT * FROM Channels",
                    dbConnection))
                    {
                        using (MySqlDataReader dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                listOfActiveChannels.Add(dr.GetString(0));
                            }
                        }
                        GC.Collect();
                    }
                }
            }
            catch
            (MySqlException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
            return listOfActiveChannels;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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
            "Pwd=GUM5fLtzuHPq;" +
            "Charset=utf8;";

        public void WriteError(Exception e)
        {
            const string filePath = @"C:\Users\starr\Documents\GitHub\bot_csharp\irc_bot\errors.txt";

            using (var writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Message :" + e.Message + "<br/>" + Environment.NewLine + "StackTrace :" + e.StackTrace +
                   "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }

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
                            var command = new MySqlCommand("SELECT * FROM Channels WHERE join_status=1",
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
                WriteError(e);
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
                                "INSERT INTO Channels(channel_name,allow_urls,dicksize,gg,song_request,gameq) VALUES(@channel,@urls,@dicksize,@gg,@song_request,@gameq)",
                                dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@urls", true);
                        cmd.Parameters.AddWithValue("@dicksize", false);
                        cmd.Parameters.AddWithValue("@gg", false);
                        cmd.Parameters.AddWithValue("@song_request", false);
                        cmd.Parameters.AddWithValue("@gameq", false);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new MySqlCommand("INSERT INTO FollowerNotifications(channel_name)Values(@c)", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@c", channel);
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
                    using (var cmd = new MySqlCommand("INSERT INTO FollowerNotifications(channel_name)VALUES(@channel)", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
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
                            var cmd = new MySqlCommand("delete from League WHERE channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    using (
                    var cmd = new MySqlCommand("delete from FollowerNotifications WHERE channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
                return false;
            }
            catch (Exception e)
            {
                WriteError(e);
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
                WriteError(e);
                return false;
            }
            catch (Exception e)
            {
                WriteError(e);
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
                        WriteError(e);
                        return false;
                    }
                }
            }
        }


        public bool AsciiToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                    var command =
                        new MySqlCommand("UPDATE Channels SET ascii_status=@a WHERE channel_name=@channel",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@a", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        WriteError(e);
                        return false;
                    }
                }
            }
        }

        public bool EmoteToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                    var command =
                        new MySqlCommand("UPDATE Channels SET emote_status=@e WHERE channel_name=@channel",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@e", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        WriteError(e);
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
                        WriteError(e);
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
                WriteError(e);
                return false;
            }
        }

        public bool AsciiStatus(string channel)
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
                                result = reader.GetBoolean(8);
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
                WriteError(e);
                return false;
            }
        }


        public bool EmoteStatus(string channel)
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
                                result = reader.GetBoolean(9);
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
                WriteError(e);
                return false;
            }
        }



        public bool CheckQueueStatus(string channel)
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
                            if (reader.Read())
                            {
                                var result = false;
                                result = reader.GetBoolean(5);
                                return result;
                            }

                        }
                        GC.Collect();
                        return false;
                    }
                }
            }
            catch
                (MySqlException e)
            {
                WriteError(e);
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
                WriteError(e);
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
                        WriteError(e);
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
                WriteError(e);
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
                        WriteError(e);
                        return false;
                    }
                }
            }
        }

        public int CheckAsciiCount(string channel)
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
                            reader.Read();
                            return reader.GetInt32(6);
                        }
                    }
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return 0;
            }
        }

        public int CheckEmoteCount(string channel)
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
                            reader.Read();
                            return reader.GetInt32(7);
                        }
                    }
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return 0;
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
                WriteError(e);
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
                        WriteError(e);
                        return false;
                    }
                }
            }
        }


        public bool QueueToggle(string channel, bool toggle)
        {
            using (var dbConnection = new MySqlConnection(ConnectionString))
            {
                dbConnection.Open();
                using (
                        var command = new MySqlCommand("UPDATE Channels SET gameq=@g WHERE channel_name=@c",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@c", channel);
                        command.Parameters.AddWithValue("@g", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (MySqlException e)
                    {
                        WriteError(e);
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
                        WriteError(e);
                        return false;
                    }
                }
            }
        }

        public string DickSize(string channel)
        {
            var randRange = new Random((int)DateTime.Now.Ticks & (0x0000FFFF));
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
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
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
                WriteError(e);
            }
        }

        public Dictionary<string, List<string>> GetTimers(int time)
        {
            var timedMessages = new Dictionary<string, List<string>>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                            var command = new MySqlCommand("getTimers", dbConnection))
                    {

                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@t", time);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (timedMessages.ContainsKey(reader.GetString(1)))
                                {
                                    timedMessages[reader.GetString(1)].Add(reader.GetString(2));
                                }
                                else
                                {
                                    timedMessages.Add(reader.GetString(1), new List<string>());
                                    timedMessages[reader.GetString(1)].Add(reader.GetString(2));
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
                WriteError(e);
                return null;
            }
        }


        public List<string> ParseFirstHundredFollowers(string fromChannel, TwitchApi twitchApi)
        {
            var followersList = new List<string>();
            Dictionary<string, DateTime> followersDictionary = twitchApi.GetFirstHundredFollowers(fromChannel);
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
                    WriteError(e);
                    return null;
                }
                catch (TimeoutException e)
                {
                    WriteError(e);
                    return null;
                }
            }
            return null;
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
                    WriteError(e);
                    return null;
                }
                catch (TimeoutException e)
                {
                    WriteError(e);
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
            catch (TimeoutException e)
            {
                WriteError(e);
                return null;
            }
            catch
                (MySqlException e)
            {
                WriteError(e);
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
                WriteError(e);
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
                                "INSERT INTO Timers(channel_name,message, time_interval)VALUES(@channel, @msg, @time)",
                                dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", "jchinn");
                        command.Parameters.AddWithValue("@msg", message + " SHAME " + message);
                        command.Parameters.AddWithValue("@time", 15);
                        command.ExecuteNonQuery();
                    }
                }
                GC.Collect();
                return true;
            }
            catch (MySqlException e)
            {
                WriteError(e);
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
                WriteError(e);
                return null;
            }
        }

        public bool CheckSongRequestStatus(string channelName)
        {
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
                        WriteError(e);
                        return false;
                    }
                }
            }
        }


        public bool AddRegular(string channelName, string userToAdd)
        {
            var exists = false;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();

                    using (var command = new MySqlCommand("SELECT * FROM RegularList WHERE ChannelName=@c AND User=@u", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", channelName);
                        command.Parameters.AddWithValue("@u", userToAdd);
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
                                    "insert into RegularList (ChannelName, User) Values(@c, @u)",
                                    dbConnection))
                        {
                            command.Parameters.AddWithValue("@c", channelName);
                            command.Parameters.AddWithValue("@u", userToAdd);
                            command.ExecuteNonQuery();
                        }
                        GC.Collect();
                        return true;
                    }
                }
                return false;
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return false;

            }
        }


        public bool RemoveRegular(string fromChannel, string userToRemove)
        {
            try
            {
                using (
                        var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                            var inDb = new MySqlCommand("select * from RegularList where ChannelName=@c and User=@u", dbConnection))
                    {
                        inDb.Parameters.AddWithValue("@c", fromChannel);
                        inDb.Parameters.AddWithValue("@u", userToRemove);
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
                        var cmd = new MySqlCommand("delete from RegularList where ChannelName=@c and User=@u", dbConnection)
                        )
                    {
                        cmd.Parameters.AddWithValue("@c", fromChannel);
                        cmd.Parameters.AddWithValue("@u", userToRemove);
                        cmd.ExecuteNonQuery();
                    }
                    GC.Collect();
                    return true;
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return false;
            }
        }


        public bool RegularExist(string channelName, string userToAdd)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();

                    using (
                        var command = new MySqlCommand("SELECT * FROM RegularList WHERE ChannelName=@c AND User=@u",
                            dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", channelName);
                        command.Parameters.AddWithValue("@u", userToAdd);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // if we found a value that means it exits and we don't want ot add a duplicate
                            while (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                    GC.Collect();
                }
                return false;
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return false;

            }
        }


        public int GetUsersSongCount(TwitchMessage message)
        {
            var count = 0;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();

                    using (
                        var command =
                            new MySqlCommand("Select Count(*) FROM Songs WHERE channel_name=@c AND requested_by=@r",
                                dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", message.FromChannel);
                        command.Parameters.AddWithValue("@r", message.MsgSender);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // if we found a value that means it exits and we don't want ot add a duplicate
                            if (reader.Read())
                            {
                                count = reader.GetInt32(0);
                            }
                        }
                    }
                    GC.Collect();
                    return count;
                }
            }
            catch
                (MySqlException e)
            {
                WriteError(e);
                return 100;

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
                WriteError(e);
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
                WriteError(e);
                return song;
            }
        }

        public string RemoveUserLastSong(string fromChannel, string userToRemove)
        {
            try
            {
                var songName = "";
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (var inDb = new MySqlCommand("SELECT * FROM Songs where channel_name=@c and requested_by=@r ORDER BY id DESC LIMIT 1;", dbConnection))
                    {
                        inDb.Parameters.AddWithValue("@c", fromChannel);
                        inDb.Parameters.AddWithValue("@r", userToRemove);
                        using (var reader = inDb.ExecuteReader())
                        {
                            if (reader.Read())
                                songName = reader.GetString(6);
                            songName += " by ";
                            songName += reader.GetString(5);
                            songName += " was removed from the playlist";
                        }
                    }
                    GC.Collect();
                }
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (var inDb = new MySqlCommand("delete from Songs where channel_name=@c and requested_by=@r ORDER BY id DESC LIMIT 1;", dbConnection))
                    {
                        inDb.Parameters.AddWithValue("@c", fromChannel);
                        inDb.Parameters.AddWithValue("@r", userToRemove);
                        inDb.ExecuteNonQuery();
                    }
                }
                GC.Collect();
                return songName;
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return null;
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
                        var command = new MySqlCommand("SELECT * FROM Channels WHERE join_status=1",
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
            catch (TimeoutException e)
            {
                WriteError(e);
                return null;
            }
            catch
                (MySqlException e)
            {
                WriteError(e);
                return null;
            }
            return listOfActiveChannels;
        }


        public List<string> AddToQueue(TwitchMessage msg, bool regular, string leagueName, string summonerId, string rank)
        {
            var leagueQueue = new List<string>();
            var update = false;
            var dbId = 0;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (var command = new MySqlCommand("Select * From LeagueQueue WHERE channelName=@c AND twitchName=@t", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", msg.FromChannel);
                        command.Parameters.AddWithValue("@t", msg.MsgSender);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                update = true;
                                dbId= reader.GetInt32(0);
                            }
                        }
                    }
                    GC.Collect();
                    Console.WriteLine(dbId);
                    //Console.WriteLine(summ)
                    if (dbId != 0)
                    {
                        using (var command = new MySqlCommand("UPDATE LeagueQueue SET leagueName=@l, summonerId=@i, rank=@r, sub=@sub, regular=@reg WHERE id=@id", dbConnection))
                        {
                            command.Parameters.AddWithValue("@l", leagueName);
                            command.Parameters.AddWithValue("@i", summonerId);
                            command.Parameters.AddWithValue("@id", dbId);
                            command.Parameters.AddWithValue("@r", rank);
                            command.Parameters.AddWithValue("@reg", msg.Subscriber);
                            command.Parameters.AddWithValue("@sub", regular);
                            command.ExecuteNonQuery();
                        }
                        GC.Collect();
                    }
                    else
                    {
                        using (
                                var command =
                                new MySqlCommand(
                                    "INSERT INTO LeagueQueue(channelName,twitchName, leagueName, regular, sub, summonerId, rank)VALUES(@c, @t, @l, @reg, @sub, @i, @r);",
                                    dbConnection))
                        {
                            command.Parameters.AddWithValue("@c", msg.FromChannel);
                            command.Parameters.AddWithValue("@t", msg.MsgSender);
                            command.Parameters.AddWithValue("@l", leagueName);
                            command.Parameters.AddWithValue("@i", summonerId);
                            command.Parameters.AddWithValue("@r", rank);
                            command.Parameters.AddWithValue("@reg", msg.Subscriber);
                            command.Parameters.AddWithValue("@sub", regular);
                            
                            command.ExecuteNonQuery();
                        }

                        GC.Collect();
                    }
                    using (var command = new MySqlCommand("select * From LeagueQueue WHERE channelName=@c", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", msg.FromChannel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                leagueQueue.Add(reader.GetString(2));
                            }
                        }

                    }

                }
                GC.Collect();
                return leagueQueue;
            }

            catch (MySqlException e)
            {
                WriteError(e);
                return null;
            }
        }


        public List<string> GetQueuePostion(TwitchMessage msg)
        {
            var leagueQueue = new List<string>();
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (var command = new MySqlCommand("select * From LeagueQueue WHERE channelName=@c", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", msg.FromChannel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                leagueQueue.Add(reader.GetString(2));
                            }
                        }

                    }

                }
                GC.Collect();
                return leagueQueue;
            }

            catch (MySqlException e)
            {
                WriteError(e);
                return null;
            }
        }

        public bool RemovePersonFromQueue(string fromChannel, string userToRemove)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                            var cmd = new MySqlCommand("SELECT * FROM LeagueQueue WHERE channelName=@c and twitchName=@t",
                                dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@c", fromChannel);
                        cmd.Parameters.AddWithValue("@t", userToRemove);
                        bool okToDelete = false;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (userToRemove != reader.GetString(2)) continue;
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
                                "DELETE FROM LeagueQueue WHERE channelName=@c AND twitchName=@t",
                                dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@c", fromChannel);
                        cmd.Parameters.AddWithValue("@t", userToRemove);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (MySqlException e)
            {
                WriteError(e);
                return false;
            }
            catch (Exception e)
            {
                WriteError(e);
                return false;
            }
        }


        public bool GetAnnounceFollowerStatus(string fromChannel)
        {
            var announceFollowerStatus = false;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                        var command = new MySqlCommand("select * From Channels WHERE channel_name=@c", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", fromChannel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                announceFollowerStatus = reader.GetBoolean(11);
                            }
                        }

                    }

                }
                GC.Collect();
                return announceFollowerStatus;
            }

            catch (TimeoutException e)
            {
                WriteError(e);
                return false;
            }

            catch (MySqlException e)
            {
                WriteError(e);
                return false;
            }
        }



        public bool GetRegularStatus(TwitchMessage msg)
        {
            var regularStatus = false;
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                        var command = new MySqlCommand("select * From Channels WHERE channel_name=@c", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", msg.FromChannel);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                regularStatus = reader.GetBoolean(10);
                            }
                        }

                    }

                }
                GC.Collect();
                return regularStatus;
            }

            catch (MySqlException e)
            {
                WriteError(e);
                return false;
            }

        }
        public bool ToggleRegularOnOff(TwitchMessage msg, bool toggle)
        {
            try
            {
                using (var dbConnection = new MySqlConnection(ConnectionString))
                {
                    dbConnection.Open();
                    using (
                        var command = new MySqlCommand("UPDATE Channels SET reg_status=@r WHERE channel_name=@c", dbConnection))
                    {
                        command.Parameters.AddWithValue("@c", msg.FromChannel);
                        command.Parameters.AddWithValue("@r", toggle);
                        command.ExecuteNonQuery();

                    }

                }
                GC.Collect();
                return true;
            }

            catch (MySqlException e)
            {
                WriteError(e);
                return false;
            }

        }




    }
}

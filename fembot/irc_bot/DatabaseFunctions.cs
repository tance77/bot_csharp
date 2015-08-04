using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace twitch_irc_bot
{
    internal class DatabaseFunctions
    {
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM Channels GROUP BY channel_name",
                            dbConnection))
                    {
                        using (var dr = command.ExecuteReader())
                        {
                            while (dr != null && dr.Read())
                                channelsToJoin.Add(dr.GetValue(0).ToString());
                        }
                        GC.Collect();
                        return channelsToJoin;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return channelsToJoin;
            }
        }


        public bool AddToChannels(string channel)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var inDb = new SQLiteCommand("select * from Channels where channel_name=@channel", dbConnection)
                        )
                    {
                        inDb.Parameters.AddWithValue("@channel", channel);
                        using (var reader = inDb.ExecuteReader())
                        {
                            if (reader == null || reader.Read())
                            {
                                return false;
                            }
                        }
                        GC.Collect();
                    }
                    using (var cmd =
                        new SQLiteCommand(
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
                        new SQLiteCommand(
                            "INSERT INTO Spotify(channel_name,is_setup,on_off)VALUES(@channel,@isSetup,@OnOff)",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.Parameters.AddWithValue("@isSetup", false);
                        cmd.Parameters.AddWithValue("@OnOff", false);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd =
                        new SQLiteCommand(
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
            catch (SQLiteException e)
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
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var inDb = new SQLiteCommand("select * from Channels where channel_name=@channel", dbConnection)
                        )
                    {
                        inDb.Parameters.AddWithValue("@channel", channel);
                        using (var reader = inDb.ExecuteReader())
                        {
                            if (reader == null || !reader.Read())
                            {
                                return false;
                            }
                        }
                        GC.Collect();
                    }
                    using (
                        var cmd = new SQLiteCommand("delete from Channels where channel_name=@channel", dbConnection)
                        )
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    using (
                        var cmd = new SQLiteCommand("delete from Spotify where channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    using (
                        var cmd = new SQLiteCommand("delete from League WHERE channel_name=@channel", dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public Tuple<string, bool> MatchCommand(string matchCommand, string channel)
        {
            matchCommand = matchCommand.Split('!')[1];
            string description = null;
            var toUser = false;
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var fetchedCommand = reader.GetString(2);
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
            catch (SQLiteException e)
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (var reader = cmd.ExecuteReader())
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
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool AddCommand(string command, string commandDescription, bool toUser, string channel)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        using (var reader = cmd.ExecuteReader())
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
                        new SQLiteCommand(
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
            catch (SQLiteException e)
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
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        var okToEdit = false;
                        using (var reader = cmd.ExecuteReader())
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
                        new SQLiteCommand(
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
                (SQLiteException e)
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel",
                            dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@channel", channel);
                        var okToDelete = false;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (command != reader.GetString(2)) continue;
                                okToDelete = true;
                                break;
                            }
                        }
                        GC.Collect(); //garbage collector bug with sqlite
                        if (!okToDelete)
                        {
                            return false;
                        }
                    }
                    using (var cmd =
                        new SQLiteCommand(
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
            catch (SQLiteException e)
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
            using (
                var dbConnection =
                    new SQLiteConnection(
                        @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
            {
                dbConnection.Open();
                using (
                    var command =
                        new SQLiteCommand("UPDATE Channels SET dicksize=@dick_size WHERE channel_name=@channel",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@dick_size", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (SQLiteException e)
                    {
                        Console.Write(e + "\r\n");
                        return false;
                    }
                }
            }
        }

        public bool UrlToggle(string channel, bool toggle)
        {
            using (
                var dbConnection =
                    new SQLiteConnection(
                        @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
            {
                dbConnection.Open();
                using (
                    var command = new SQLiteCommand("UPDATE Channels SET allow_urls=@urls WHERE channel_name=@channel",
                        dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@urls", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (SQLiteException e)
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                            dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        bool result;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader == null)
                            {
                                return false;
                            }
                            reader.Read();
                            result = reader.GetBoolean(1);
                        }
                        GC.Collect();
                        return result;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public string SummonerStatus(string channel)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM League WHERE channel_name=@channel",
                            dbConnection)
                        )
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        string result;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader == null)
                            {
                                return null;
                            }
                            reader.Read();
                            result = reader.GetValue(2).ToString();
                        }
                        GC.Collect();
                        return result;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool SetSummonerName(string channel, string summonerName)
        {
            summonerName = summonerName.Trim(' ');
            using (
                var dbConnection =
                    new SQLiteConnection(
                        @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
            {
                dbConnection.Open();

                using (
                    var command =
                        new SQLiteCommand("UPDATE League SET summoner_name=@summoner_name WHERE channel_name=@channel",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@summoner_name", summonerName);
                        command.ExecuteNonQuery();
                        return true;
                    }
                    catch (SQLiteException e)
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM League WHERE channel_name=@channel",
                            dbConnection)
                        )
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        string summonerId;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader == null)
                            {
                                return null;
                            }
                            reader.Read();
                            summonerId = reader.GetValue(1).ToString();
                        }
                        GC.Collect();
                        if (summonerId == "")
                        {
                            return null;
                        }
                        return summonerId;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool SetSummonerId(string channel, string summonerId)
        {
            using (
                var dbConnection =
                    new SQLiteConnection(
                        @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
            {
                dbConnection.Open();
                using (
                    var command =
                        new SQLiteCommand("UPDATE League SET summoner_id=@summoner_id WHERE channel_name=@channel",
                            dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@summoner_id", summonerId);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (SQLiteException e)
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
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                            dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        bool result;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader == null)
                            {

                                return false;
                            }
                            reader.Read();
                            result = reader.GetBoolean(3);
                        }
                        GC.Collect();
                        return result;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool GgToggle(string channel, bool toggle)
        {
            using (
                var dbConnection =
                    new SQLiteConnection(
                        @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
            {
                dbConnection.Open();
                using (
                    var command = new SQLiteCommand("UPDATE Channels SET gg=@gg WHERE channel_name=@channel",
                        dbConnection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        command.Parameters.AddWithValue("@gg", toggle);
                        command.ExecuteNonQuery();

                        return true;
                    }
                    catch (SQLiteException e)
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
            var randOne = randRange.Next(1, 4);
            var randTwo = randRange.Next(1, 13);
            var onOff = false;
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command = new SQLiteCommand("SELECT * FROM Channels WHERE channel_name=@channel",
                            dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", channel);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader != null)
                            {
                                reader.Read();
                                onOff = reader.GetBoolean(2);
                            }
                        }
                        GC.Collect();
                    }
                    if (!onOff)
                    {

                        return null;
                    }
                    using (
                        var command = new SQLiteCommand("select r" + randOne + " from DickSizes where rowid=" + randTwo,
                            dbConnection))
                    {
                        var values = new object[1];
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader != null)
                            {
                                reader.Read();
                                reader.GetValues(values);
                            }
                        }
                        GC.Collect();
                        Console.Write("{0}\r\n", values[0]);
                        var response = values[0].ToString();

                        return response;
                    }
                }
            }
            catch
                (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public bool CheckPermitStatus(string fromChannel, string user)
        {
            user = user.ToLower();
            user = user.Trim(' ');
            var startTime = "";
            var endTime = "";
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    var okToUpdate = true;
                    using (
                        var command =
                            new SQLiteCommand(
                                "SELECT * FROM ChannelUsers WHERE channel_name=@channel and user=@msg_sender",
                                dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@msg_sender", user);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader != null)
                            {
                                reader.Read();
                                if (reader.StepCount == 0)
                                {
                                    okToUpdate = false;
                                }
                                else
                                {
                                    startTime = reader.GetString(2);
                                    endTime = reader.GetString(3);
                                }
                            }
                        }
                        GC.Collect();
                    }

                    if (okToUpdate == false)
                    {
                        AddUserToPermitList(fromChannel, user);
                        using (var command = new SQLiteCommand("SELECT * FROM ChannelUsers WHERE channel_name=@channel and user=@msg_sender", dbConnection))
                        {
                            command.Parameters.AddWithValue("@channel", fromChannel);
                            command.Parameters.AddWithValue("@msg_sender", user);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader != null)
                                {
                                    reader.Read();
                                    startTime = reader.GetString(2);
                                    endTime = reader.GetString(3);
                                }
                            }
                            GC.Collect();
                        }
                    }


                }
            }
            catch (SQLiteException e)
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
            var minutesTillExpire = (permitEnds - permitedAt).Minutes;
            var secondsTillExpire = (permitEnds - permitedAt).Seconds;
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
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command =
                            new SQLiteCommand(
                                "SELECT * FROM ChannelUsers WHERE channel_name=@channel and user=@msg_sender",
                                dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@msg_sender", user);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader != null)
                            {
                                reader.Read();
                                if (reader.StepCount == 0)
                                {
                                    return false;
                                }
                            }
                        }
                        GC.Collect();
                        return true;
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public bool PermitUser(string fromChannel, string permitedUser)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command =
                            new SQLiteCommand(
                                "UPDATE ChannelUsers SET permitted_at=@permit, permit_expires_at=@expires WHERE channel_name=@channel AND user=@user",
                                dbConnection))
                    {
                        var permit = DateTimeSqLite(DateTime.Now);
                        var expires = DateTimeSqLite((DateTime.Now.AddMinutes(3)));
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", permitedUser);
                        command.Parameters.AddWithValue("@permit", permit);
                        command.Parameters.AddWithValue("@expires", expires);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;
            }
        }

        public void RemovePermit(string fromChannel, string user)
        {
                       try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command =
                            new SQLiteCommand(
                                "UPDATE ChannelUsers SET permitted_at=@permit, permit_expires_at=@expires WHERE channel_name=@channel AND user=@user",
                                dbConnection))
                    {
                        var expires = DateTimeSqLite((DateTime.Now));
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@permit", expires);
                        command.Parameters.AddWithValue("@expires", expires);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
            }
        } 

        public void AddUserToPermitList(string fromChannel, string user)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command =
                            new SQLiteCommand(
                                "INSERT INTO ChannelUsers(channel_name, user, permitted_at, permit_expires_at)VALUES(@channel, @user, @permit, @expires)", dbConnection))
                    {
                        var expires = DateTimeSqLite(DateTime.Now);
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@permit", expires);
                        command.Parameters.AddWithValue("@expires", expires);
                        command.ExecuteNonQuery();

                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
            }
        }
        public void RemoveUserToPermitList(string fromChannel, string user)
        {
            try
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var command =
                            new SQLiteCommand(
                                "delete from ChannelUsers where channel_name=@channl AND user=@user", dbConnection))
                    {
                        command.Parameters.AddWithValue("@channel", fromChannel);
                        command.Parameters.AddWithValue("@user", user);
                        command.ExecuteNonQuery();

                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
            }
        }
    }
}

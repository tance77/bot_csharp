﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace twitch_irc_bot
{
    class DatabaseFunctions
    {
        private readonly SQLiteConnection _dbConnection;

        public DatabaseFunctions()
        {
            try
            {
                _dbConnection = new SQLiteConnection(@"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp_v1\fembot.sqlite;Version=3;");
                _dbConnection.Open();
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
            }
        }

        public bool ConnectToDatabase()
        {
            try
            {
                _dbConnection.Open();
                return true;
            }
            catch (SQLiteException e)
            {
                Console.Write(e);
                Console.Write("\r\n");
                return false;
            }
        }

        public bool CloseConnection()
        {
            try
            {
                _dbConnection.Close();
                return true;
            }
            catch (SQLiteException e)
            {
                Console.Write(e + "\r\n");
                return false;

            }
        }

        public List<string> JoinChannels()
        {
            var channelsToJoin = new List<string>();
            using (var command = new SQLiteCommand("SELECT * FROM Channels GROUP BY channel_name", _dbConnection))
            {
                try
                {
                    var dr = command.ExecuteReader();
                    while (dr != null && dr.Read())
                        channelsToJoin.Add(dr.GetValue(0).ToString());

                    return channelsToJoin;
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return channelsToJoin;
                }
            }
        }
        

        public bool AddToChannels(string channel)
        {
            using (var inDb = new SQLiteCommand("select * from Channels where channel_name=@channel", _dbConnection))
            {
                inDb.Parameters.AddWithValue("@channel", channel);
                try
                {
                    var reader = inDb.ExecuteReader();
                    if (reader == null || reader.Read()) return false;
                    var cmmd = new SQLiteCommand("INSERT INTO Channels(channel_name)VALUES(@channel)", _dbConnection);
                    cmmd.Parameters.AddWithValue("@channel", channel);
                    cmmd.ExecuteNonQuery();
                    var cmd =
                        new SQLiteCommand(
                            "INSERT INTO Spotify(channel_name,is_setup,on_off)VALUES(@channel,@isSetup,@OnOff)",
                            _dbConnection);
                    cmd.Parameters.AddWithValue("@channel", channel);
                    cmd.Parameters.AddWithValue("@isSetup", false);
                    cmd.Parameters.AddWithValue("@OnOff", false);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return false;
                }
            }
        }

        public bool RemoveFromChannels(string channel)
        {
            using (var inDb = new SQLiteCommand("select * from Channels where channel_name=@channel", _dbConnection))
            {
                inDb.Parameters.AddWithValue("@channel", channel);
                try
                {
                    var reader = inDb.ExecuteReader();
                    if (reader == null || !reader.Read()) return false;
                    var cmmd = new SQLiteCommand("delete from Channels where channel_name=@channel", _dbConnection);
                    cmmd.Parameters.AddWithValue("@channel", channel);
                    cmmd.ExecuteNonQuery();
                    var cmd = new SQLiteCommand("delete from Spotify where channel_name=@channel", _dbConnection);
                    cmd.Parameters.AddWithValue("@channel", channel);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return false;
                }
            }
        }

        public Tuple<string,bool> MatchCommand(string matchCommand, string channel)
        {
            matchCommand = matchCommand.Split('!')[1];
            var toUser = false;
            using (var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel", _dbConnection))
            {
                cmd.Parameters.AddWithValue("@channel", channel);
                string description = null;
                try
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var fetchedCommand = reader.GetString(2);
                        if (fetchedCommand != matchCommand) continue;
                        description = reader.GetString(1);
                        toUser = reader.GetBoolean(3);
                        break;
                    }
                    return new Tuple<string, bool>(description, toUser);
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return null;
                }
            }
        }

        public List<string> GetChannelCommands(string channel)
        {
            using (var cmd = new SQLiteCommand("SELECT * FROM Commands WHERE channel_name=@channel", _dbConnection))
            {
                cmd.Parameters.AddWithValue("@channel", channel);
                try
                {
                    var reader = cmd.ExecuteReader();
                    var commands = new List<string>();
                    while (reader.Read())
                    {
                        commands.Add(reader.GetString(2));
                    }
                    return commands;
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return null;
                }
            }
        }

        public string DickSize()
        {
            var randRange = new Random();
            var randOne = randRange.Next(1, 4);
            var randTwo = randRange.Next(1, 13);
            using (
                var command = new SQLiteCommand("select r" + randOne + " from DickSizes where rowid=" + randTwo,
                    _dbConnection))
            {
                try
                {
                    var values = new object[1];
                    var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        reader.Read();
// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        reader.GetValues(values);
                    }
                    else
                    {
                    }
                    Console.Write("{0}\r\n", values[0]);
                    var response = values[0].ToString();
                    return response;
                }
                catch (SQLiteException e)
                {
                    Console.Write(e + "\r\n");
                    return "DataBase Error";
                }
            }
        }
    }
}

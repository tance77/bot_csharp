using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class Spotify : WebFunctions
    {
        public Spotify ()
        {
        }

        public string AddSongById (string songId, DatabaseFunctions db, string fromChannel, string messageSender)
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

            var response = RequestJson (requestUrl);
            if (response == "400" || response == "401" || response == "404" || response == "429" ||
                response == "500" || response == "503") {
                return "Spotify API is down please try again in a few minutes.";
            }
            JToken jsonArr = JObject.Parse (response);
            if (!jsonArr.HasValues) {
                return ", Song not found on Spotify.";
            }


            songAlbumUrl = jsonArr.SelectToken ("album").SelectToken ("images") [0].SelectToken ("url").ToString ();
            var artistAry = jsonArr.SelectToken ("artists").ToArray ();
            songArtists = "";
            for (var i = 0; i < artistAry.Length; i++) {
                if (i == artistAry.Length - 1) {
                    songArtists += artistAry [i].SelectToken ("name");
                } else {
                    songArtists += artistAry [i].SelectToken ("name") + " and ";
                }
            }

            var miliseconds = Int32.Parse (jsonArr.SelectToken ("duration_ms").ToString ());

            var a = miliseconds / 1000;
            var minutes = a / 60;
            a = a % 60;

            var seconds = a.ToString ();

            if (a < 10) {
                seconds = "0" + a;
            }

            songDuration = minutes + ":" + seconds;

            songUrl = jsonArr.SelectToken ("external_urls").SelectToken ("spotify").ToString ();
            songId = jsonArr.SelectToken ("id").ToString ();
            songTitle = jsonArr.SelectToken ("name").ToString ();
            foundSong = songTitle + " by " + songArtists + " was added to the playlist";
            Console.ForegroundColor = ConsoleColor.White;
            var succuess = db.AddSong (fromChannel, messageSender, songId, songDuration, songArtists, songTitle, songUrl, songAlbumUrl);
            if (succuess) {
                return foundSong;
            }
            return "song already exists or something went wrong on my end.";

        }

        public string MobileSongSearh (TwitchMessage msg, DatabaseFunctions db)
        {
            const string baseUrl = "https://api.spotify.com/v1";
            var songUrl = "";
            var songId = "";
            var songTitle = "";
            if (msg.Msg.StartsWith ("!msr")) {
                if (msg.UserType != "mod") {
                    if (db.GetRegularStatus (msg)) {
                        if (!db.RegularExist (msg.FromChannel, msg.MsgSender))
                            return null;
                    }
                }
                var msgAry = msg.Msg.Split (' ');
                var queryString = "";


                for (var i = 1; i < msgAry.Length; i++) {
                    if (msgAry [i] != "-") {
                        if (msgAry [i].Contains ("-")) {
                            queryString += msgAry [i].Trim ('-');
                        } else {
                            queryString += msgAry [i] + " ";
                        }
                    }
                }
                queryString = queryString.Trim (' ');

                //If we are searching for a track with words do below

                string requestUrl = baseUrl + "/search?type=track&offset=0&limit=20&market=US&q=" + queryString;


                var response = RequestJson (requestUrl);
                if (response == "400" || response == "401" || response == "404" || response == "429" ||
                    response == "500" || response == "503") {
                    return "Spotify API is down please try again in a few minutes.";
                }

                JToken jsonArr = JObject.Parse (response).SelectToken ("tracks").SelectToken ("items");

                //grab the first result

                if (!jsonArr.HasValues)
                    return ", I couldn't find that song on Spotify.";

                Console.WriteLine (jsonArr [0].SelectToken ("artists").ToString ());
                Console.WriteLine (jsonArr [0].SelectToken ("name").ToString ());

//                var songAlbumUrl = (jsonArr [0].SelectToken ("album").SelectToken ("images") [0].SelectToken ("url")).ToString ();
                var artistAry = jsonArr [0].SelectToken ("artists").ToArray ();
                var songArtists = "";

                for (var i = 0; i < artistAry.Length; i++) {
                    if (i == artistAry.Length - 1) {
                        songArtists += artistAry [i].SelectToken ("name");
                    } else {
                        songArtists += artistAry [i].SelectToken ("name") + " and ";
                    }
                }
                var miliseconds = Int32.Parse (jsonArr [0].SelectToken ("duration_ms").ToString ());

                var a = miliseconds / 1000;
                var minutes = a / 60;
                a = a % 60;

                var seconds = a.ToString ();

                if (a < 10) {
                    seconds = "0" + a;
                }

//                var songDuration = minutes + ":" + seconds;

                songUrl = jsonArr [0].SelectToken ("external_urls").SelectToken ("spotify").ToString ();
                songId = jsonArr [0].SelectToken ("id").ToString ();
                songTitle = jsonArr [0].SelectToken ("name").ToString ();

//                var foundSong = songTitle + " by " + songArtists + " was added to the playlist";
                return AddSongById (songId, db, msg.FromChannel, msg.MsgSender);



            }
            return null;
        }

        public Dictionary<string, KeyValuePair<string,string>> SearchSongByName (string queryString, TwitchMessage message, BlockingCollection<string> BlockingMessageQueue, BlockingCollection<string> BlockingWhisperQueue)
        {
            var foundSongs = new Dictionary<string, KeyValuePair<string,string>> ();

            //Console.WriteLine(queryString);


            var response = RequestJson (queryString);
            if (response == null) {
                BlockingWhisperQueue.Add ("PRIVMSG #jtv :/w " + message.MsgSender + " Spotify API is down please try again in a few minutes.");
                return null;
            }

            JToken jsonArr = JObject.Parse (response).SelectToken ("tracks").SelectToken ("items");

            //grab the first result

            if (!jsonArr.HasValues) {
                BlockingWhisperQueue.Add ("PRIVMSG #jtv :/w " + message.MsgSender + " Could not find the requested song on Spotify. Check your spelling and try again. Alternatively use the track ID.");
                return null;
            }

            var k = 0;
            foreach (var song in jsonArr) {
                if (k >= 3) {
                    break;
                }
                var songTitle = song.SelectToken ("name").ToString ();
                var songId = song.SelectToken ("id").ToString ();
                var songArtists = "";
                var artistAry = song.SelectToken ("artists").ToArray ();

                for (var i = 0; i < artistAry.Length; i++) {
                    if (i == artistAry.Length - 1) {
                        songArtists += artistAry [i].SelectToken ("name");
                    } else {
                        songArtists += artistAry [i].SelectToken ("name") + " and ";
                    }
                }

                var pair = new KeyValuePair<string, string> (songTitle, songArtists);
                foundSongs.Add (songId, pair);
                k++;

            }

            return foundSongs;
        }

        public List<string> SearchSong (DatabaseFunctions db, TwitchMessage msg, BlockingCollection<string> BlockingMessageQueue, BlockingCollection<string> BlockingWhisperQueue)
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
            var songList = new List<string> ();
            if (msg.Msg.StartsWith ("!songrequest ") || msg.Msg.StartsWith ("!sr ")) {
                if (msg.UserType != "mod") {
                    if (db.GetRegularStatus (msg)) {
                        if (!db.RegularExist (msg.FromChannel, msg.MsgSender))
                            return songList;
                    }
                }
                var msgAry = msg.Msg.Split (' ');
                var queryString = "";


                for (var i = 1; i < msgAry.Length; i++) {
                    if (msgAry [i] != "-") {
                        if (msgAry [i].Contains ("-")) {
                            queryString += msgAry [i].Trim ('-');
                        } else {
                            queryString += msgAry [i] + " ";
                        }
                    }
                }
                queryString = queryString.Trim (' ');


                //don't allow youtube requests
                const string pattern =
                    @"https:\/\/(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([\w\-]+)(&(amp;)?[\w\?=]*)?";

                if (Regex.Match (msg.Msg, pattern).Success) {
                    songList.Add ("Sorry we are using Spotify not YouTube.");
                    return songList;
                }

                //song track URL
                if (Regex.Match (queryString, @"^https:\/\/.*com\/track\/").Success) {
                    queryString = queryString.Split ('/') [4];
                }

                //song URI Request
                else if (queryString.Length == 36 && queryString.Contains ("spotify:track:")) {
                    queryString = queryString.Split (':') [2];
                }

                //don't allow request by album
                else if (queryString.Length == 36 && queryString.Contains ("spotify:album:") ||
                         Regex.Match (msg.Msg, @"^https:\/\/.*com\/album\/").Success) {
                    songList.Add ("Sorry you can't request a whole album.");
                    return songList;
                }

                //don't allow request by artist
                else if (queryString.Length == 37 && queryString.Contains ("spotify:artist:") ||
                         Regex.Match (msg.Msg, @"^https:\/\/.*com\/artist\/").Success) {
                    songList.Add ("Sorry you can't request an artist.");
                    return songList;
                }



                //track ID only look up by track id
                if (!queryString.Contains (" ") && queryString.Length == 22) {
                    songList.Add (AddSongById (queryString, db, msg.FromChannel, msg.MsgSender));
                    return songList;
                }
                //If we are searching for a track with words do below

                string requestUrl = baseUrl + "/search?type=track&offset=0&limit=20&market=US&q=" + queryString;
                var multipleResults = SearchSongByName (requestUrl, msg, BlockingMessageQueue, BlockingWhisperQueue);
                if (multipleResults == null || multipleResults.Count == 0) {
                    //songList.Add("Song not found.");
                    return songList;    
                }

                if (multipleResults.Count > 1) {


                    foreach (var song in multipleResults) {
                        //Console.WriteLine(song);
                        songList.Add (song.Value.Key + " - " + song.Value.Value + "" +
                        " [Track Id]: => " + song.Key);
                    }
                    return songList;
                }
                songList.Add (AddSongById (multipleResults.First ().Key, db, msg.FromChannel, msg.MsgSender));
                return songList;    
            }
            var succuess = db.AddSong (msg.FromChannel, msg.MsgSender, songId, songDuration, songArtists, songTitle, songUrl, songAlbumUrl);
            if (succuess) {
                songList.Add (foundSong);
                return songList;
            }
            songList.Add ("song already exists or something went wrong on my end.");
            return songList;
        }

        public string RemoveUserLastSong (DatabaseFunctions db, TwitchMessage msg)
        {
            var result = db.RemoveUserLastSong (msg.FromChannel, msg.MsgSender);
            return !string.IsNullOrEmpty (result) ? result : null;
        }
    }
}


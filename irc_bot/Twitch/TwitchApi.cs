using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions;

namespace twitch_irc_bot
{
    internal class TwitchApi : WebFunctions
    {
        public string GetStreamUptime(string fromChannel)
        {
            string url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            string jsonString = RequestJsonTwitch(url);
            if (jsonString == null) return "Stream is offline.";
            if (!JObject.Parse(jsonString).SelectToken("stream").HasValues)
            {
                return "Stream is offline.";
            }
            string createdAt = JObject.Parse(jsonString).SelectToken("stream").SelectToken("created_at").ToString();
            DateTime startedAt;
            if (!DateTime.TryParse(createdAt, out startedAt))
            {
                return "Could not reach Twitch API";
            }
            DateTime nowTime = DateTime.Now;
            nowTime = nowTime.ToUniversalTime();
            int onlineForHours = (nowTime - startedAt).Hours;
            int onlineForMinutes = (nowTime - startedAt).Minutes;
            int onlineForSeconds = (nowTime - startedAt).Seconds;

            return fromChannel + " has been online for " + onlineForHours + " hours " + onlineForMinutes + " minutes " +
                onlineForSeconds + " seconds";
        }

        //Returns weather the stream is online or not
        public bool StreamStatus(string fromChannel)
        {
            try
            {
                var url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
                var jsonString = RequestJsonTwitch(url);
                if (jsonString == null)
                {
                    return false;
                }
                var tokenArry = JObject.Parse(jsonString).SelectToken("stream");
                if (!tokenArry.ToString().HasValue())
                {
                    return false;
                }
                return true;
            }
            catch (JsonReaderException e)
            {
                return false;
            }
        }

        public Dictionary<string, DateTime> GetRecentFollowers(string fromChannel)
        {
            try
            {
                var r = new Random();
                int limit = r.Next(25, 100);
                string url = "https://api.twitch.tv/kraken/channels/" + fromChannel + "/follows?limit=" + limit;
                string jsonString = RequestJsonTwitch(url);
                var followsDictionary = new Dictionary<string, DateTime>();
                if (jsonString.Count() == 3) return null;
                if (!JObject.Parse(jsonString).HasValues ||
                    (!JObject.Parse(jsonString).SelectToken("follows").HasValues))
                {
                    return null;
                }
                JToken jsonArr = JObject.Parse(jsonString).SelectToken("follows");
                foreach (JToken item in jsonArr)
                {
                    string createdAt = JObject.Parse(item.ToString()).SelectToken("created_at").ToString();
                    DateTime followDate;
                    if (!DateTime.TryParse(createdAt, out followDate))
                    {
                        return null;
                    }
                    string displayName =
                        JObject.Parse(item.ToString()).SelectToken("user").SelectToken("display_name").ToString();
                    followsDictionary.Add(displayName, followDate);
                }
                return followsDictionary;
            }
            catch (JsonReaderException e)
            {
                return null;
            }
        }

        public Dictionary<string, DateTime> GetFirstHundredFollowers(string fromChannel)
        {
            try
            {
                string url = "https://api.twitch.tv/kraken/channels/" + fromChannel + "/follows?limit=100";
                string jsonString = RequestJsonTwitch(url);
                var followsDictionary = new Dictionary<string, DateTime>();
                if (jsonString == null) return null;
                if (!JObject.Parse(jsonString).HasValues ||
                    (!JObject.Parse(jsonString).SelectToken("follows").HasValues))
                {
                    return null;
                }
                JToken jsonArr = JObject.Parse(jsonString).SelectToken("follows");
                foreach (JToken item in jsonArr)
                {
                    string createdAt = JObject.Parse(item.ToString()).SelectToken("created_at").ToString();
                    DateTime followDate;
                    if (!DateTime.TryParse(createdAt, out followDate))
                    {
                        return null;
                    }
                    string displayName =
                        JObject.Parse(item.ToString()).SelectToken("user").SelectToken("display_name").ToString();
                    followsDictionary.Add(displayName, followDate);
                }
                return followsDictionary;
            }
            catch (JsonReaderException e)
            {
                return null;
            }
        }


        public string GetHowLongUserFollows(string fromChannel, string user)
        {
            try
            {
                var url = "https://api.twitch.tv/kraken/users/" + user + "/follows/channels/" + fromChannel;
                string jsonString = RequestJsonTwitch(url);
                if (jsonString == null) return null;
                if (jsonString == "404" || JObject.Parse(jsonString).SelectToken("status") != null)
                {
                    return "404";
                }
                var createdAt = JObject.Parse(jsonString).SelectToken("created_at").ToString();
                Console.WriteLine(createdAt);
                var followedAt = Convert.ToDateTime(createdAt).ToUniversalTime();
                var todaysDate = DateTime.UtcNow;

                var howlong = (todaysDate - followedAt);

                var years = Math.Floor(howlong.TotalDays/365);
                var months = Math.Floor(howlong.TotalDays%365/30);
                var days = Math.Floor(howlong.TotalDays%365%30);


                //Console.WriteLine(years);
                //Console.WriteLine(months);
                //Console.WriteLine(days);

                //Console.WriteLine(howlong);
                
                
                
                //Console.WriteLine(howlong.TotalDays);
                //Console.WriteLine(howlong.TotalDays%365);
                return years + " year(s) " + months + " month(s) and " + days + " day(s) Follow Date " + followedAt;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }



        public List<string> GetActiveUsers(string fromChannel) //via chatters json deprecated
        {
            var userList = new List<string>();
            string url = "http://tmi.twitch.tv/group/user/" + fromChannel + "/chatters";
            string jsonString = RequestJsonTwitch(url);
            if (jsonString == null) return null;

            //Line  87 is equivalent to line 89
            //JToken modsaksjdlsakd = JObject.Parse(jsonString)["chatters"]["moderators"];

            JToken mods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("moderators");
            JToken staff = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("staff");
            JToken admins = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("admins");
            JToken globalMods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("global_mods");
            JToken viewers = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("viewers");
            userList.AddRange(mods.Select(mod => (string)mod));
            userList.AddRange(staff.Select(user => (string)user));
            userList.AddRange(admins.Select(admin => (string)admin));
            userList.AddRange(globalMods.Select(globalMod => (string)globalMod));
            userList.AddRange(viewers.Select(viewer => (string)viewer));
            return userList;
        }

        public bool CheckAccountCreation(string user)
        {
            try
            {
                string url = "https://api.twitch.tv/kraken/users/" + user;
                string jsonString = RequestJsonTwitch(url);
                //know way of telling if the account was created today so just return false
                if (jsonString.Length == 3) return false;
                var creation_date = JObject.Parse(jsonString).SelectToken("created_at").ToString();
                //Was the account created before today
                var a = DateTime.Compare(DateTime.Parse(creation_date).Date, DateTime.UtcNow.Date.AddDays(-7));
                //.Date just compares the date
                //returns true if creation is > = 0 else false
                //you're to young for links
                if (Convert.ToInt32(a) <= 0)
                {
                    return true;
                    //You're an adult please continue
                }
                return false;

                
            }
            catch (Exception e)
            {
                Console.Write("Error in check account creation twitchapi.cs 158");
                return false;
            }
        }
    }
}

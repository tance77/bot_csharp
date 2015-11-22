using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions;
using SpotifyAPI.Web.Models;

namespace twitch_irc_bot
{
    internal class TwitchApi : WebFunctions
    {
        public string GetStreamUptime(string fromChannel)
        {
            string url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            string jsonString = RequestJson(url);
            if(jsonString == null) return "Stream is offline.";
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
            var url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            var jsonString = RequestJson(url);
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

        public Dictionary<string, DateTime> GetRecentFollowers(string fromChannel)
        {
            var r = new Random();
            int limit = r.Next(25, 100);
            string url = "https://api.twitch.tv/kraken/channels/" + fromChannel + "/follows?limit=" + limit;
            string jsonString = RequestJson(url);
            var followsDictionary = new Dictionary<string, DateTime>();
            if(jsonString == null) return null;
            if (!JObject.Parse(jsonString).HasValues || (!JObject.Parse(jsonString).SelectToken("follows").HasValues))
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


        public List<string> GetActiveUsers(string fromChannel) //via chatters json deprecated
        {
            var userList = new List<string>();
            string url = "http://tmi.twitch.tv/group/user/" + fromChannel + "/chatters";
            string jsonString = RequestJson(url);
            if(jsonString == null) return null;

            //Line  87 is equivalent to line 89
            //JToken modsaksjdlsakd = JObject.Parse(jsonString)["chatters"]["moderators"];

            JToken mods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("moderators");
            JToken staff = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("staff");
            JToken admins = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("admins");
            JToken globalMods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("global_mods");
            JToken viewers = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("viewers");
            userList.AddRange(mods.Select(mod => (string) mod));
            userList.AddRange(staff.Select(user => (string) user));
            userList.AddRange(admins.Select(admin => (string) admin));
            userList.AddRange(globalMods.Select(globalMod => (string) globalMod));
            userList.AddRange(viewers.Select(viewer => (string) viewer));
            return userList;
        }
        public bool CheckAccountCreation(string user)
        {
            string url = "https://api.twitch.tv/kraken/users/" + user;
            string jsonString = RequestJson(url);
            //know way of telling if the account was created today so just return false
            if(jsonString == null) return false;
            var creation_date = JObject.Parse(jsonString).SelectToken("created_at").ToString();
            //Was the account created before today
            var a = DateTime.Compare(DateTime.Parse(creation_date).Date.AddDays(-1) , DateTime.UtcNow.Date); //.Date just compares the date
            //returns true if creation is > = 0 else false
            //you're to young for links
            return a >= 0;
            //You're an adult please continue
        }
    }
}

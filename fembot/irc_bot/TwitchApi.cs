using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class TwitchApi : WebFunctions
    {
        public string GetStreamUptime(string fromChannel)
        {
            string url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            string jsonString = RequestJson(url);
            if (jsonString == "" || jsonString == "502" || jsonString == "404" || jsonString == "503" || jsonString == "422" || jsonString == "500") return "Stream is offline.";
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

        public bool StreamStatus(string fromChannel)
        {
            string url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            string jsonString = RequestJson(url);
            if (jsonString != "" || !JObject.Parse(jsonString).SelectToken("stream").HasValues)
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
            if (jsonString == "" || jsonString == "502" || jsonString == "404" || jsonString == "503" || jsonString =="422" ||
                jsonString == "500") return null;
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
            if (jsonString == "" || jsonString == "502") return null;
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
    }
}
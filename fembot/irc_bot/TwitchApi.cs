using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    class TwitchApi : WebFunctions
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

        public bool StreamStatus(string fromChannel)
        {
            var url = "https://api.twitch.tv/kraken/streams/" + fromChannel;
            var jsonString = RequestJson(url);
            if (!JObject.Parse(jsonString).SelectToken("stream").HasValues)
            {
                return false;
            }
            return true;
        }

        public Dictionary<string, DateTime> GetRecentFollowers(string fromChannel)
        {
            var r = new Random();
            var limit = r.Next(25, 100);
            var url = "https://api.twitch.tv/kraken/channels/" + fromChannel + "/follows?limit="+ limit;
            var jsonString = RequestJson(url);
            var followsDictionary = new Dictionary<string, DateTime>();
            if (jsonString == "" || jsonString == "502" || jsonString=="404" || jsonString =="503" || jsonString == "500") return null;
            if (!JObject.Parse(jsonString).HasValues || (!JObject.Parse(jsonString).SelectToken("follows").HasValues))
            {
                return null;
            }
            var jsonArr = JObject.Parse(jsonString).SelectToken("follows");
            foreach (var item in jsonArr)
            {
                var createdAt = JObject.Parse(item.ToString()).SelectToken("created_at").ToString();
                DateTime followDate;
                if (!DateTime.TryParse(createdAt, out followDate))
                {
                    return null;
                }
                var displayName =
                    JObject.Parse(item.ToString()).SelectToken("user").SelectToken("display_name").ToString();
                followsDictionary.Add(displayName, followDate);
            }
            return followsDictionary;
        }


        public List<string> GetActiveUsers(string fromChannel) //via chatters json deprecated
        {
            var userList = new List<string>();
            var url = "http://tmi.twitch.tv/group/user/" + fromChannel + "/chatters";
            var jsonString = RequestJson(url);
            if (jsonString == "" || jsonString == "502") return null;
            var mods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("moderators");
            var staff = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("staff");
            var admins = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("admins");
            var globalMods = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("global_mods");
            var viewers = JObject.Parse(jsonString).SelectToken("chatters").SelectToken("viewers");
            userList.AddRange(mods.Select(mod => (string) mod));
            userList.AddRange(staff.Select(user => (string) user));
            userList.AddRange(admins.Select(admin => (string) admin));
            userList.AddRange(globalMods.Select(globalMod => (string) globalMod));
            userList.AddRange(viewers.Select(viewer => (string) viewer));
            return userList;
        }
    }
}

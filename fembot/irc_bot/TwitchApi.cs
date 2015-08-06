using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace twitch_irc_bot
{
    class TwitchApi : WebFunctions
    {
        public TwitchApi() { }
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

        public Dictionary<string,DateTime> GetRecentFollowers(string fromChannel)
        {
            var url = "https://api.twitch.tv/kraken/channels/" + fromChannel + "/follows?limit=25";
            var jsonString = RequestJson(url);
            var followsDictionary = new Dictionary<string, DateTime>();
            if (!JObject.Parse(jsonString).SelectToken("follows").HasValues)
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
                var displayName = JObject.Parse(item.ToString()).SelectToken("user").SelectToken("display_name").ToString();
                followsDictionary.Add(displayName, followDate);
            }
            return followsDictionary;

        }
    }
}

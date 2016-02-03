using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class WebFunctions
    {
        public string RequestJson(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Timeout = 1000;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            return objReader.ReadToEnd(); //returns json string
                        }
                    }
                }
            }
            catch
                (WebException e)
                {
                    string errorCode = e.ToString().Split('(')[1].Split(')')[0];
                    Console.Write(e + "\r\n");
                    return errorCode;
                }
        }
        public List<string> GetGlobalEmotes()
        {
            var emoteList = new List<string>();
            var response = RequestJson("http://twitchemotes.com/api_cache/v2/global.json");
            if (response == null)
            {
                return null;
            }
            var jsonArr = JObject.Parse(response).SelectToken("emotes");
            foreach (var emote in jsonArr)
            {
                emoteList.Add(emote.ToString().Split('"')[1]);
            }
            return emoteList;
        }
    }
}

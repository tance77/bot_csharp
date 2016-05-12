using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class WebFunctions
    {
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
        public string RequestJson(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
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
                    WriteError(e);
                    var errorCode = e.ToString().Split('(')[1].Split(')')[0];
                    return errorCode;
                }
        }

        public string RequestJsonTwitch(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Headers.Add("Client-ID", "6z7z2mw029j3z6mmyur2y9r19r0qlob");
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
                WriteError(e);
                var errorCode = e.ToString().Split('(')[1].Split(')')[0];
                return errorCode;
            }
        }
        public List<string> GetGlobalEmotes()
        {
            try
            {
                var emoteList = new List<string>();
                var response = RequestJsonTwitch("http://twitchemotes.com/api_cache/v2/global.json");
                if (response.Length == 3)
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
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                WriteError(e);
                return null;
            }
        }
    }
}

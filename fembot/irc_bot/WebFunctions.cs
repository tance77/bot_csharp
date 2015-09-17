using System;
using System.IO;
using System.Net;

namespace twitch_irc_bot
{
    internal class WebFunctions
    {
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
                string errorCode = e.ToString().Split('(')[1].Split(')')[0];
                Console.Write(e + "\r\n");
                return errorCode;
            }
        }
    }
}
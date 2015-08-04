using System;
using System.IO;
using System.Net;

namespace twitch_irc_bot
{
    class WebFunctions
    {
        public string RequestJson(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            //returns json string
                            return objReader.ReadToEnd();
                        }
                    }
                }
            }
            catch
                (WebException e)
            {
                var errorCode = e.ToString().Split('(')[1].Split(')')[0];
                Console.Write(e + "\r\n");
                return errorCode;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    class RiotApi
    {
        private readonly string _apiKey;
        private readonly DatabaseFunctions _db = new DatabaseFunctions();
        public RiotApi(DatabaseFunctions db)
        {
            _apiKey = "59b21249-afb2-4484-ad4f-842536d31437";
            _db = db;
        }
        public Dictionary<string, int> GetMasteries(string fromChannel)
        {
            var summonerId = _db.GetSummonerId(fromChannel);
            var masteriesList = new Dictionary<string, int>();

            var request = WebRequest.Create("https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId + "/masteries?api_key=" + _apiKey);
            //check for reponse status
            try
            {
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            var jsonString = objReader.ReadToEnd();
                            var jsonArr = JObject.Parse(jsonString).SelectToken(summonerId).SelectToken("pages");

                            foreach (var page in jsonArr)
                            {
                                var current = JObject.Parse(page.ToString()).SelectToken("current").ToString();
                                if (current == "True")
                                {
                                    var masteriesArr = JObject.Parse(page.ToString()).SelectToken("masteries");
                                    foreach (var mastery in masteriesArr)
                                    {
                                        var cur = JObject.Parse(mastery.ToString()).SelectToken("id").ToString();
                                        var rank = JObject.Parse(mastery.ToString()).SelectToken("rank").ToString();
                                        int tmp;
                                        if (int.TryParse(rank, out tmp))
                                            masteriesList.Add(cur, tmp);
                                    }
                                }
                            }
                        }
                    }
                }

                var masterDictionary = new Dictionary<string, int> { { "Offense", 0 }, { "Defense", 0 }, { "Utility", 0 } };

                foreach (var masteryId in masteriesList)
                {
                    var request2 =
                        WebRequest.Create("https://global.api.pvp.net/api/lol/static-data/na/v1.2/mastery/" +
                                          masteryId.Key +
                                          "?masteryData=masteryTree&api_key=" + _apiKey);
                    //check for reponse status
                    using (var response = request2.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream == null) return null;
                            using (var objReader2 = new StreamReader(responseStream))
                            {
                                var masteryJson = objReader2.ReadToEnd();
                                var masteryTree = JObject.Parse(masteryJson).SelectToken("masteryTree").ToString();
                                masterDictionary[masteryTree] += masteryId.Value;
                            }
                        }
                    }
                }
                return masterDictionary;
            }
            catch (WebException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public string GetSummonerId(string summonerName)
        {
            try
            {
                summonerName = summonerName.Trim(' ');
                var request =
                    WebRequest.Create("https://na.api.pvp.net/api/lol/na/v1.4/summoner/by-name/" + summonerName +
                                      "?api_key=" + _apiKey);
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            var jsonString = objReader.ReadToEnd();
                            summonerName = summonerName.Replace(" ", "").ToLower();
                            //gets rids of spaces for json string riot removes spaces
                            return (JObject.Parse(jsonString).SelectToken(summonerName).SelectToken("id")).ToString();
                        }
                    }
                }
            }
            catch (WebException e)
            {
                var errorCode = e.ToString().Split('(')[1].Split(')')[0];
                Console.Write(errorCode + "\r\n");
                return errorCode;
            }
        }

        public Dictionary<string, int> GetRunes(string fromChannel)
        {
            var summonerId = _db.GetSummonerId(fromChannel);
            var runeDictionary = new Dictionary<string, int>();
            if (summonerId == null) return null;
            var request =
                WebRequest.Create("https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId +
                                  "/runes?api_key=" + _apiKey);
            try
            {
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            var jsonString = objReader.ReadToEnd();
                            var jsonArr = JObject.Parse(jsonString).SelectToken(summonerId).SelectToken("pages");
                            foreach (var item in jsonArr)
                            {
                                var current = JObject.Parse(item.ToString()).SelectToken("current").ToString();
                                if (current == "True")
                                {
                                    var runeArr = JObject.Parse(item.ToString()).SelectToken("slots");
                                    foreach (var rune in runeArr)
                                    {
                                        var cur = JObject.Parse(rune.ToString()).SelectToken("runeId").ToString();
                                        if (runeDictionary.ContainsKey(cur))
                                        {
                                            runeDictionary[cur] += 1;
                                        }
                                        else
                                        {
                                            runeDictionary.Add(cur, 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //getting the name of each rune
                var runeDictionaryNames = new Dictionary<string, int>();
                foreach (var id in runeDictionary)
                {
                    var request2 =
                        WebRequest.Create("https://global.api.pvp.net/api/lol/static-data/na/v1.2/rune/" + id.Key +
                                          "?api_key=" + _apiKey);
                    using (var response = request2.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream == null) return null;
                            using (var objReader2 = new StreamReader(responseStream))
                            {
                                var jsonString2 = objReader2.ReadToEnd();
                                var runeName = JObject.Parse(jsonString2).SelectToken("name").ToString();
                                runeDictionaryNames.Add(runeName, id.Value);
                            }
                        }
                    }
                }
                return runeDictionaryNames;
            }
            catch (WebException e)
            {
                Console.Write(e + "\r\n");
                return null;
            }
        }

        public string GetRank(string summonerId)
        {
            try
            {
                var request =
                    WebRequest.Create("https://na.api.pvp.net/api/lol/na/v2.5/league/by-summoner/" + summonerId +
                                      "/entry?api_key=" + _apiKey);
                using (var response = request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) return null;
                        using (var objReader = new StreamReader(responseStream))
                        {
                            var jsonString = objReader.ReadToEnd();
                            var division =
                                JObject.Parse(jsonString)
                                    .SelectToken(summonerId)
                                    .First.SelectToken("entries")
                                    .First.SelectToken("division")
                                    .ToString();
                            var tier =
                                JObject.Parse(jsonString).SelectToken(summonerId).First.SelectToken("tier").ToString();
                            return tier + " " + division;
                        }
                    }
                }
            }
            catch
                (WebException e)
            {
                var errorCode = e.ToString().Split('(')[1].Split(')')[0];
                Console.Write(errorCode + "\r\n");
                return errorCode;
            }
        }

    }
}

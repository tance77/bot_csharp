using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    class RiotApi : WebFunctions
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

            var url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId + "/masteries?api_key=" + _apiKey;
            var jsonString = RequestJson(url);
            if (jsonString == "400" || jsonString == "401" || jsonString == "404" || jsonString == "429" || jsonString == "500" || jsonString == "503")
            {
                return null;
            }
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

            var masterDictionary = new Dictionary<string, int> { { "Offense", 0 }, { "Defense", 0 }, { "Utility", 0 } };

            foreach (var masteryId in masteriesList)
            {
                var url2 = "https://global.api.pvp.net/api/lol/static-data/na/v1.2/mastery/" + masteryId.Key + "?masteryData=masteryTree&api_key=" + _apiKey;
                var masteryJson = RequestJson(url2);
                var masteryTree = JObject.Parse(masteryJson).SelectToken("masteryTree").ToString();
                masterDictionary[masteryTree] += masteryId.Value;
            }
            return masterDictionary;
        }

        public string GetSummonerId(string summonerName)
        {
            summonerName = summonerName.Trim(' ');
            var url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/by-name/" + summonerName + "?api_key=" + _apiKey;
            var jsonString = RequestJson(url);
            if (jsonString == "400" || jsonString == "401" || jsonString == "404" || jsonString == "429" ||
                jsonString == "500" || jsonString == "503")
            {
                return jsonString;
            }
            summonerName = summonerName.Replace(" ", "").ToLower();
            //gets rids of spaces for json string riot removes spaces
            return (JObject.Parse(jsonString).SelectToken(summonerName).SelectToken("id")).ToString();
        }

        public Dictionary<string, int> GetRunes(string fromChannel)
        {
            var summonerId = _db.GetSummonerId(fromChannel);
            var runeDictionary = new Dictionary<string, int>();
            if (summonerId == null) return null;
            var url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId + "/runes?api_key=" + _apiKey;
            var jsonString = RequestJson(url);
            if (jsonString == "400" || jsonString == "401" || jsonString == "404" || jsonString == "429" || jsonString == "500" || jsonString == "503")
            {
                return null;
            }
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
            //getting the name of each rune
            var runeDictionaryNames = new Dictionary<string, int>();
            foreach (var id in runeDictionary)
            {
                var url2 = "https://global.api.pvp.net/api/lol/static-data/na/v1.2/rune/" + id.Key + "?api_key=" + _apiKey;
                var jsonString2 = RequestJson(url2);
                var runeName = JObject.Parse(jsonString2).SelectToken("name").ToString();
                runeDictionaryNames.Add(runeName, id.Value);
            }
            return runeDictionaryNames;
        }

        public string GetRank(string summonerId)
        {
            var url = "https://na.api.pvp.net/api/lol/na/v2.5/league/by-summoner/" + summonerId + "/entry?api_key=" + _apiKey;
            var jsonString = RequestJson(url);
            if (jsonString == "400" || jsonString == "401" || jsonString == "404" || jsonString == "429" || jsonString == "500" || jsonString == "503")
            {
                return jsonString;
            }
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

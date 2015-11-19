using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class RiotApi : WebFunctions
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
            string summonerId = _db.GetSummonerId(fromChannel);
            var masteriesList = new Dictionary<string, int>();

            string url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId + "/masteries?api_key=" +
                _apiKey;
            string jsonString = RequestJson(url);
            if (jsonString == null)
            {
                return null;
            }
            JToken jsonArr = JObject.Parse(jsonString).SelectToken(summonerId).SelectToken("pages");

            foreach (JToken page in jsonArr)
            {
                string current = JObject.Parse(page.ToString()).SelectToken("current").ToString();
                if (current == "True")
                {
                    JToken masteriesArr = JObject.Parse(page.ToString()).SelectToken("masteries");
                    foreach (JToken mastery in masteriesArr)
                    {
                        string cur = JObject.Parse(mastery.ToString()).SelectToken("id").ToString();
                        string rank = JObject.Parse(mastery.ToString()).SelectToken("rank").ToString();
                        int tmp;
                        if (int.TryParse(rank, out tmp))
                            masteriesList.Add(cur, tmp);
                    }
                }
            }

            var masterDictionary = new Dictionary<string, int> {{"Ferocity", 0}, {"Cunning", 0}, {"Resolve", 0}};

            foreach (var masteryId in masteriesList)
            {
                string url2 = "https://global.api.pvp.net/api/lol/static-data/na/v1.2/mastery/" + masteryId.Key +
                    "?masteryData=masteryTree&api_key=" + _apiKey;
                string masteryJson = RequestJson(url2);
                string masteryTree = JObject.Parse(masteryJson).SelectToken("masteryTree").ToString();
                if (masteryTree == "Offense")
                {
                    masterDictionary["Ferocity"] += masteryId.Value;
                    
                }
                if (masteryTree == "Utility")
                {
                    masterDictionary["Resolve"] += masteryId.Value;
                }
                if (masteryTree == "Defense")
                {
                    masterDictionary["Cunning"] += masteryId.Value;
                }


            }
            return masterDictionary;
        }

        public string GetSummonerId(string summonerName)
        {
            summonerName = summonerName.Trim(' ');
            string url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/by-name/" + summonerName + "?api_key=" +
                _apiKey;
            string jsonString = RequestJson(url);
            if (jsonString == null)
            {
                return jsonString;
            }
            summonerName = summonerName.Replace(" ", "").ToLower();
            //gets rids of spaces for json string riot removes spaces
            return (JObject.Parse(jsonString).SelectToken(summonerName).SelectToken("id")).ToString();
        }

        public Dictionary<string, int> GetRunes(string fromChannel)
        {
            string summonerId = _db.GetSummonerId(fromChannel);
            var runeDictionary = new Dictionary<string, int>();
            if (summonerId == null) return null;
            string url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/" + summonerId + "/runes?api_key=" + _apiKey;
            string jsonString = RequestJson(url);
            if (jsonString == null)
            {
                return null;
            }
            JToken jsonArr = JObject.Parse(jsonString).SelectToken(summonerId).SelectToken("pages");
            foreach (JToken item in jsonArr)
            {
                string current = JObject.Parse(item.ToString()).SelectToken("current").ToString();
                if (current == "True")
                {
                    JToken runeArr = JObject.Parse(item.ToString()).SelectToken("slots");
                    foreach (JToken rune in runeArr)
                    {
                        string cur = JObject.Parse(rune.ToString()).SelectToken("runeId").ToString();
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
                string url2 = "https://global.api.pvp.net/api/lol/static-data/na/v1.2/rune/" + id.Key + "?api_key=" +
                    _apiKey;
                string jsonString2 = RequestJson(url2);
                string runeName = JObject.Parse(jsonString2).SelectToken("name").ToString();
                runeDictionaryNames.Add(runeName, id.Value);
            }
            return runeDictionaryNames;
        }

        public string GetRank(string summonerId)
        {
            string url = "https://na.api.pvp.net/api/lol/na/v2.5/league/by-summoner/" + summonerId + "/entry?api_key=" +
                _apiKey;
            string jsonString = RequestJson(url);
            if (jsonString == null)
            {
                return jsonString;
            }
            string division =
                JObject.Parse(jsonString)
                .SelectToken(summonerId)
                .First.SelectToken("entries")
                .First.SelectToken("division")
                .ToString();
            string lp =
                JObject.Parse(jsonString)
                .SelectToken(summonerId)
                .First.SelectToken("entries")
                .First.SelectToken("leaguePoints")
                .ToString();
            string tier =
                JObject.Parse(jsonString).SelectToken(summonerId).First.SelectToken("tier").ToString().ToLower();
            return tier + " " + division + " with " + lp + " league points";
        }
    }
}

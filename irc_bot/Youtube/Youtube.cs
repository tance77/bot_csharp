using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace twitch_irc_bot
{
    internal class Youtube : WebFunctions
    {
        public string Title { get; set; }

        public string Description { get; set; }

        private string ApiKey { get; set; }

        private string ResponseCode { get; set; }

        public Youtube(string apiKey)
        {
            ApiKey = apiKey;
            Title = null;
            Description = null;
        }

        public void GetYoutubeInfo(string videoId)
        {
            var url = "https://www.googleapis.com/youtube/v3/videos?id=" + videoId + "&key=" + ApiKey + "&part=snippet";
            var response = RequestJson(url);
            if (response == "400" || response == "401" || response == "404" || response == "429" ||
                response == "500" || response == "503")
            {
                ResponseCode = response;
                return;
            }
            JToken jsonArr = JObject.Parse (response);
            if (!jsonArr.HasValues)
            {
                return;
            }
            Title = jsonArr.SelectToken("items")[0].SelectToken("snippet").SelectToken("title").ToString();

            Description = jsonArr.SelectToken("items")[0].SelectToken("snippet").SelectToken("description").ToString();
            
        }

        public void SanatizeTitle()
        {
            if (Title == null) return;
            Title = Title.ToLower();

            //Title = Regex.Replace(Title, @"[^\u0000-\u007F]", string.Empty);
            //Title = Regex.Replace(Title, @"[^a-zA-Z0-9 -]", string.Empty);
            Title = Regex.Replace(Title, @"[[({].*?[})\]]", string.Empty);
            Title = Regex.Replace(Title, @"[:_!\&®]+?", string.Empty);
            Title = Regex.Replace(Title, @"\w\/\w", string.Empty);
            Title = Regex.Replace(Title, @"-", string.Empty);
            Title = Regex.Replace(Title, @"(music\s+?video|20\d{2}|audio\s+?original|official\s+?audio|lyrics|reggaeton)", string.Empty);
            Title = Regex.Replace(Title, @"(ft|feat(uring)?|hd|of{1,2}icial\w*|exclusiv[eo]|v[ií]deo\w*)", string.Empty);
            Title = Regex.Replace(Title, @"(\s+prod\.\s+|\s+prod\s+)", string.Empty);
            //Title = Regex.Replace(Title, @"(\s+by\s+)", string.Empty);

        }

        public string ExtractVideoId(string url)
        {
            const string pattern = @"(\/?:watch\?v=|\.be\/)\b(.+?)\b.*?";
            var matched = Regex.Matches(pattern, url);
            var result = matched[0].Groups[1].Value;
            return result;
        }


    }
}

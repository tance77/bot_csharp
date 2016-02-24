using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace twitch_irc_bot.Youtube
{
    internal class Youtube : WebFunctions
    {
        public Youtube()
        {

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

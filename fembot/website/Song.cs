using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fembot
{
    public class Song
    {
        public string Title { get; set; }
        public string Artist{ get; set; }
        public string Durration { get; set; }
        public string RequestedBy { get; set; }
        public string Url { get; set; }
    }
}
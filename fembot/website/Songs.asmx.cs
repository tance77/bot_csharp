using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace fembot
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Songs : System.Web.Services.WebService
    {
        [WebMethod]
        public List<Song> GetSongByChannelName(string channelName)
        {
            var song = new Song();
            var songList = new List<Song>();
            var connectionstring = ConfigurationManager.ConnectionStrings["dbconnect"].ConnectionString;
            using (var con = new SQLiteConnection(connectionstring) )
            {
                var cmd = new SQLiteCommand("SELECT * FROM Songs WHERE channel_name=@channel", con);
                cmd.Parameters.AddWithValue("@channel", channelName);
                con.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    song.RequestedBy = reader["channel_name"].ToString();
                    song.Durration = reader["durration"].ToString();
                    song.Artist = reader["artist"].ToString();
                    song.Title = reader["title"].ToString();
                    song.Url = reader["url"].ToString();
                    songList.Add(song);
                }
            }
            return songList;
        }
    }

}

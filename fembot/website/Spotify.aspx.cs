using System;
using System.Data;
using System.Data.SQLite;

namespace fembot
{
    public partial class Spotify : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated) // if the user is already logged in
            {
                Response.Redirect("~/Account/Login.aspx");
            }
            if (!IsPostBack)
            {
                GridViewBind();
            }
        }

        public void SongUpdateTimer(object sender, EventArgs e)
        {
            GridViewBind();
        }

        private void GridViewBind()
        {
            using (var ds = new DataSet())
            {
                using (
                    var dbConnection =
                        new SQLiteConnection(
                            @"Data Source=C:\Users\Lance\Documents\GitHub\bot_csharp\fembot.sqlite;Version=3;"))
                {
                    dbConnection.Open();
                    using (
                        var cmd = new SQLiteDataAdapter("SELECT * FROM Songs WHERE channel_name=jchinn", dbConnection))
                    {
                        cmd.Fill(ds, "Songs");
                        GridView1.DataSource = cmd;
                        GridView1.DataBind();
                    }
                }
            }
        }
    }
}
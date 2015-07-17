using System;
using System.Data.OleDb;
using System.Data.SQLite;

namespace fembot
{
    public partial class Spotify : System.Web.UI.Page 
    {
        private SQLiteConnection connection = new SQLiteConnection();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated) // if the user is already logged in
            {
                Response.Redirect("~/Account/Login.aspx");
            }
            
        }

        protected void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            songsGrid.DataBind();
            lastUpdated.Text = "Last Updated at " + DateTime.Now;

        }
    }
}
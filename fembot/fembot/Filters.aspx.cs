using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace fembot
{
    public partial class Filters : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated) // if the user is already logged in
            {
                Response.Redirect("~/Account/Login.aspx");
            }
        }
    }
}
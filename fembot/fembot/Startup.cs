using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(fembot.Startup))]
namespace fembot
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}

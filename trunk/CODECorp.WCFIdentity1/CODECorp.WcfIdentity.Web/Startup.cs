using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CODECorp.WcfIdentity.Web.Startup))]
namespace CODECorp.WcfIdentity.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

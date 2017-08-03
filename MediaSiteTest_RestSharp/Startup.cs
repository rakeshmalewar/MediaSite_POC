using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MediaSiteTest_RestSharp.Startup))]
namespace MediaSiteTest_RestSharp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

using System.Web;
using HostBridge.Core;
using HostBridge.Wcf;

namespace WcfService
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            var host = new LegacyHostBuilder().Build();

            HostBridgeWcf.Initialize(host);
        }
    }
}
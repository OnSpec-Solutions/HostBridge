using System;
using System.Web;
using HostBridge.Diagnostics;

namespace OwinComposite
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // OWIN Startup handles all wiring.
        }

#if DEBUG        
        private static volatile bool s_verified;
#endif
        protected void Application_BeginRequest()
        {
#if DEBUG
            if (s_verified) return;
            new HostBridgeVerifier()
                .Add(AspNetChecks.VerifyAspNet)
                .ThrowIfCritical();
            s_verified = true;
#endif
        }
    }
}
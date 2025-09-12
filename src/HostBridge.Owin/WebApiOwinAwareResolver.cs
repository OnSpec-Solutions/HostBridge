using HostBridge.Abstractions;
using HostBridge.AspNet;

namespace HostBridge.Owin
{
    public sealed class WebApiOwinAwareResolver : IDependencyResolver
    {
        public IDependencyScope BeginScope() => new Scope();

        public object? GetService(Type t) => GetCurrentProvider().GetService(t);
        public IEnumerable<object?> GetServices(Type t) => GetCurrentProvider().GetServices(t) ?? [];
        public void Dispose() { }

        private static IServiceProvider GetCurrentProvider()
        {
            // 1) Prefer an OWIN per-request scope (stored in the OWIN env)
            if (HttpContext.Current?.Items["owin.Environment"] is IDictionary<string, object> env &&
                env.TryGetValue(Constants.ScopeKey, out var v) &&
                v is IServiceScope owinScope)
            {
                return owinScope.ServiceProvider;
            }

            // 2) Fallback to ASP.NET IHttpModule request scope
            if (HttpContext.Current?.Items[Constants.ScopeKey] is IServiceScope aspScope)
            {
                return aspScope.ServiceProvider;
            }

            // 3) Last resort: the root provider
            return AspNetBootstrapper.RootServices
                   ?? throw new InvalidOperationException("No request scope and no root services available.");
        }

        private sealed class Scope : IDependencyScope
        {
            public object? GetService(Type t) => GetCurrentProvider().GetService(t);
            public IEnumerable<object?> GetServices(Type t) => GetCurrentProvider().GetServices(t) ?? [];
            public void Dispose() { }
        }
    }
}
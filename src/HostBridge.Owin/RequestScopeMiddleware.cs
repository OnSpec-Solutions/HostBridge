using HostBridge.Abstractions;
using HostBridge.AspNet;

namespace HostBridge.Owin;

public static class RequestScopeMiddleware
{
    public static void UseHostBridgeRequestScope(this IAppBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        app.Use(async (env, next) =>
        {
            var root = AspNetBootstrapper.RootServices
                       ?? throw new InvalidOperationException("HostBridge not initialized (AspNetBootstrapper.Initialize(host)).");

            using var scope = root.CreateScope();
            env.Set(Constants.ScopeKey, scope);
            try { await next().ConfigureAwait(false); }
            finally
            {
                env.Set<IServiceScope>(Constants.ScopeKey, value: null!);
                // ReSharper disable once DisposeOnUsingVariable
                scope.Dispose();
            }
        });
    }
}

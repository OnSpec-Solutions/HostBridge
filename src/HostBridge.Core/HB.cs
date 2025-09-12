using HostBridge.Abstractions;

namespace HostBridge.Core;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Provides a simple, static access point to the application service provider and handy helpers
/// for resolving services and creating ambient scopes.
/// </summary>
/// <remarks>
/// Before using any members, initialize the accessor by calling <see cref="Initialize(ILegacyHost)"/>
/// after building your host. Once initialized, <see cref="Root"/> points to the root service provider,
/// and <see cref="Current"/> returns the provider for the current ambient scope (or the root provider if
/// no ambient scope is active).
/// </remarks>
public static class HB
{
    private static IServiceProvider? s_root;
    private static readonly object Lock = new();
    private static readonly AsyncLocal<IServiceScope?> Ambient = new();

    /// <summary>
    /// Initializes the container accessor with the service provider from the specified <paramref name="host"/>.
    /// </summary>
    /// <param name="host">A built host that exposes the root <see cref="IServiceProvider"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the host has no service provider or when the accessor has already been initialized with a different provider.</exception>
    public static void Initialize(ILegacyHost host)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var sp = host.ServiceProvider ?? throw new InvalidOperationException("Host has no ServiceProvider.");

        lock (Lock)
        {
            if (s_root != null && !ReferenceEquals(s_root, sp))
            {
                throw new InvalidOperationException("HB has already been initialized.");
            }

            s_root = sp;
        }
    }

    /// <summary>
    /// Gets the root application service provider. Requires prior <see cref="Initialize(ILegacyHost)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    public static IServiceProvider Root =>
        s_root ?? throw new InvalidOperationException("HB not initialized. Call HB.Initialize(host) after Build().");

    /// <summary>
    /// Gets the current service provider for the ambient scope, or the root provider when no ambient scope is active.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization (see <see cref="Initialize(ILegacyHost)"/>).</exception>
    public static IServiceProvider Current => Ambient.Value?.ServiceProvider ?? Root;

    /// <summary>
    /// Creates a new dependency injection scope from the <see cref="Root"/> provider.
    /// </summary>
    /// <returns>A new <see cref="IServiceScope"/> that should be disposed when no longer needed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization (see <see cref="Initialize(ILegacyHost)"/>).</exception>
    public static IServiceScope CreateScope() => Root.CreateScope();

    /// <summary>
    /// Begins a new ambient scope and sets <see cref="Current"/> to the scope's service provider until the returned cookie is disposed.
    /// </summary>
    /// <remarks>
    /// This method is primarily intended for short, localized overrides (e.g., in tests or request handling) and should always be paired with <c>using</c>.
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> cookie that restores the previous ambient scope when disposed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization (see <see cref="Initialize(ILegacyHost)"/>).</exception>
    public static IDisposable BeginScope()
    {
        var scope = Root.CreateScope();
        var prev = Ambient.Value;
        Ambient.Value = scope;
        return new ScopeCookie(prev, scope);
    }

    /// <summary>
    /// Resolves a required service of type <typeparamref name="T"/> from the <see cref="Current"/> provider.
    /// </summary>
    /// <typeparam name="T">The service contract type.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    public static T Get<T>() where T : notnull => Current.GetRequiredService<T>();

    /// <summary>
    /// Attempts to resolve an optional service of type <typeparamref name="T"/> from the <see cref="Current"/> provider.
    /// </summary>
    /// <typeparam name="T">The service contract type.</typeparam>
    /// <returns>The resolved service instance, or <c>null</c> if the service is not registered.</returns>
    public static T? TryGet<T>() where T : class => Current.GetService<T>();

    private sealed class ScopeCookie(IServiceScope? prev, IServiceScope mine) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            try { mine.Dispose(); }
            finally { Ambient.Value = prev; }
        }
    }

#if DEBUG
    /// <summary>
    /// Resets the static state for test isolation. Intended for unit tests only.
    /// </summary>
    /// <remarks>
    /// Clears the captured root service provider and the current ambient scope.
    /// </remarks>
    internal static void _ResetForTests()
    {
        lock (Lock) { s_root = null; }

        Ambient.Value = null;
    }
#endif
}
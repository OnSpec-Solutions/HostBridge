namespace HostBridge.Core;

/// <summary>
/// Convenience helpers for "side-loading" services in legacy or edge code paths that cannot participate
/// in constructor injection. Prefer normal DI where possible.
/// </summary>
/// <remarks>
/// Usage guidance:
/// - Avoid resolving from the root provider inside request/operation pipelines. Use <see cref="InScope(System.Action{System.IServiceProvider})"/>
///   or <see cref="InScope{TResult}(System.Func{System.IServiceProvider,TResult})"/> to execute work within an ambient scope.
/// - <see cref="Singleton{TSingleton}"/> resolves once from <see cref="HB.Root"/> and caches the instance for the app domain lifetime.
/// - <see cref="Required{T}"/> and <see cref="Optional{T}"/> resolve from <see cref="HB.Current"/>, which flows with the current ambient scope.
///
/// Initialize the host via <see cref="HB.Initialize(Abstractions.ILegacyHost)"/> before using these helpers.
/// </remarks>
public static class HBSideLoad
{
    /// <summary>
    /// Resolves a required service of type <typeparamref name="T"/> from the current ambient scope
    /// (or the root provider if no scope is active).
    /// </summary>
    /// <typeparam name="T">The service contract type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered or when HostBridge is not initialized.</exception>
    public static T Required<T>() where T : notnull => HB.Current.GetRequiredService<T>();

    /// <summary>
    /// Attempts to resolve an optional service of type <typeparamref name="T"/> from the current ambient scope
    /// (or the root provider if no scope is active).
    /// </summary>
    /// <typeparam name="T">The service contract type to resolve.</typeparam>
    /// <returns>The resolved instance, or <c>null</c> if not registered.</returns>
    public static T? Optional<T>() where T : class => HB.Current.GetService<T>();

    /// <summary>
    /// Gets a singleton instance of <typeparamref name="TSingleton"/> from the root provider, cached for the
    /// lifetime of the application domain.
    /// </summary>
    /// <typeparam name="TSingleton">The singleton service contract type.</typeparam>
    /// <returns>The singleton instance.</returns>
    /// <remarks>
    /// This uses the root provider (<see cref="HB.Root"/>) to resolve the instance exactly once. Do not use for scoped or transient services.
    /// </remarks>
    public static TSingleton Singleton<TSingleton>() where TSingleton : notnull
        => LazySingleton<TSingleton>.Value;

    /// <summary>
    /// Executes the provided delegate within a new ambient DI scope and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The delegate's result type.</typeparam>
    /// <param name="work">A function that receives the scoped <see cref="IServiceProvider"/>.</param>
    /// <returns>The value returned by <paramref name="work"/>.</returns>
    /// <remarks>
    /// Always prefer resolving scoped services from the ambient scope passed into <paramref name="work"/>. The scope is disposed on completion.
    /// </remarks>
    public static TResult InScope<TResult>(Func<IServiceProvider, TResult> work)
    {
        using var cookie = HB.BeginScope();
        return work(HB.Current);
    }

    /// <summary>
    /// Executes the provided delegate within a new ambient DI scope.
    /// </summary>
    /// <param name="work">An action that receives the scoped <see cref="IServiceProvider"/>.</param>
    /// <remarks>The scope is disposed on completion.</remarks>
    public static void InScope(Action<IServiceProvider> work)
    {
        using var cookie = HB.BeginScope();
        work(HB.Current);
    }

    private static class LazySingleton<T> where T : notnull
    {
        internal static readonly T Value =
            HB.Root.GetRequiredService<T>();
    }
}

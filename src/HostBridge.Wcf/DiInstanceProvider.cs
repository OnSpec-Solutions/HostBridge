namespace HostBridge.Wcf;

/// <summary>
/// Resolves WCF service instances via Microsoft.Extensions.DependencyInjection with a fresh
/// <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> per call, and disposes that
/// scope when the <see cref="InstanceContext"/> is released.
/// </summary>
/// <remarks>
/// This provider is installed by <see cref="DiServiceHostFactory"/>/<see cref="DiServiceHost"/> which set
/// <see cref="DispatchRuntime.InstanceProvider"/>. It does not enforce any particular
/// <see cref="ServiceBehaviorAttribute.InstanceContextMode"/>; however, it ensures that each dispatch
/// has its own DI scope, so scoped dependencies behave per-operation.
/// </remarks>
/// <param name="serviceType">The concrete WCF service implementation type to resolve.</param>
internal sealed class DiInstanceProvider(Type serviceType) : IInstanceProvider, IContractBehavior
{
    /// <summary>
    /// Resolves a service instance for the given <see cref="InstanceContext"/>.
    /// </summary>
    /// <param name="instanceContext">The current WCF instance context.</param>
    /// <returns>The resolved service instance.</returns>
    public object GetInstance(InstanceContext instanceContext) =>
        GetInstance(instanceContext, null);

    /// <summary>
    /// Resolves a service instance for the given <see cref="InstanceContext"/>, creating a new DI scope
    /// for the call and attaching it to the context for later disposal.
    /// </summary>
    /// <param name="instanceContext">The current WCF instance context.</param>
    /// <param name="message">The incoming message (unused).</param>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="HostBridgeWcf.Initialize(HostBridge.Abstractions.ILegacyHost)"/> has not been called.
    /// </exception>
    public object GetInstance(InstanceContext instanceContext, Message? message)
    {
        var root = HostBridgeWcf.RootServices
                   ?? throw new InvalidOperationException(
                       "HostBridgeWcf not initialized. Call HostBridgeWcf.Initialize(host).");

        // Per-operation scope
        var scope = root.CreateScope();
        // Stick the scope on the InstanceContext so we can dispose it at ReleaseInstance
        instanceContext.Extensions.Add(new ScopeExtension(scope));

        // Resolve the service from the scoped provider
        return scope.ServiceProvider.GetRequiredService(serviceType);
    }

    /// <summary>
    /// Releases the service instance by disposing the DI scope that was created for the call.
    /// </summary>
    /// <param name="instanceContext">The current instance context.</param>
    /// <param name="instance">The service instance being released.</param>
    public void ReleaseInstance(InstanceContext instanceContext, object instance)
    {
        // Dispose the scope created for this call (also disposes any scoped IDisposable deps)
        var ext = instanceContext.Extensions.Find<ScopeExtension>();
        ext?.Dispose();
    }

    /// <summary>
    /// Installs this instance provider into the dispatch runtime. Does not alter configured
    /// instance context mode; solely ensures DI scoping per call.
    /// </summary>
    /// <param name="contract">The contract description.</param>
    /// <param name="endpoint">The service endpoint.</param>
    /// <param name="dispatchRuntime">The dispatch runtime to configure.</param>
    public void ApplyDispatchBehavior(ContractDescription? contract, ServiceEndpoint? endpoint,
        DispatchRuntime? dispatchRuntime)
    {
        // Ensure per-call semantics unless user explicitly overrides
        // (We don't force InstanceContextMode here; we just provide per-call scoping)
        dispatchRuntime!.InstanceProvider = this;
    }

    /// <summary>
    /// No binding parameters are required for this behavior.
    /// </summary>
    public void AddBindingParameters(ContractDescription? c, ServiceEndpoint? e, BindingParameterCollection? p) { }

    /// <summary>
    /// No client behavior is applied by this component.
    /// </summary>
    public void ApplyClientBehavior(ContractDescription? c, ServiceEndpoint? e, ClientRuntime? r) { }

    /// <summary>
    /// No validation is necessary for this behavior.
    /// </summary>
    public void Validate(ContractDescription? c, ServiceEndpoint? e) { }

    /// <summary>
    /// Stores the per-operation <see cref="IServiceScope"/> on the <see cref="InstanceContext"/> so it can be
    /// disposed when the operation completes.
    /// </summary>
    private sealed class ScopeExtension(IServiceScope scope) : IExtension<InstanceContext>, IDisposable
    {
        private bool _disposed;

        /// <inheritdoc />
        public void Attach(InstanceContext owner) { }

        /// <inheritdoc />
        public void Detach(InstanceContext owner) { }

        /// <summary>
        /// Disposes the stored scope once per instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            scope.Dispose();
        }
    }
}
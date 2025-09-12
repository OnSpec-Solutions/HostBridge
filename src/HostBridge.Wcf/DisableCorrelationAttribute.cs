namespace HostBridge.Wcf;

/// <summary>
/// Applied to a WCF contract interface or service implementation to opt out of automatic correlation behavior.
/// </summary>
/// <remarks>
/// When present on the contract type (or when the contract's configuration name resolves to a type annotated with
/// this attribute), <see cref="DiServiceHost"/> will not attach <see cref="CorrelationBehavior"/> even if
/// <see cref="CorrelationOptions.Enabled"/> is true and other filters would include the contract.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class DisableCorrelationAttribute : Attribute
{
}
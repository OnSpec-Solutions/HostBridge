using HostBridge.Core;

using Microsoft.Extensions.Logging;

namespace HostBridge.Wcf;

/// <summary>
/// WCF message inspector that extracts a correlation identifier from incoming requests and begins an ambient
/// logging correlation scope for the duration of the operation.
/// </summary>
/// <remarks>
/// The inspector probes first for a SOAP header with the configured name, and if not found, falls back to the
/// corresponding HTTP header (useful for REST endpoints hosted with webHttpBinding). The created scope is disposed
/// in <see cref="BeforeSendReply(ref Message, object)"/>.
/// </remarks>
/// <param name="headerName">The SOAP/HTTP header name to read the correlation id from.</param>
internal sealed class CorrelationDispatchInspector(string headerName) : IDispatchMessageInspector
{
    /// <summary>
    /// Called by WCF after receiving a request. Initializes a correlation scope using the correlation id, if any.
    /// </summary>
    /// <param name="request">The incoming message.</param>
    /// <param name="channel">The channel the message arrived on.</param>
    /// <param name="instanceContext">The current instance context.</param>
    /// <returns>An <see cref="IDisposable"/> cookie representing the started correlation scope to be disposed later.</returns>
    public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
    {
        var root = HostBridgeWcf.RootServices!;
        var logger = root.GetRequiredService<ILoggerFactory>().CreateLogger("Correlation");

        string? id = null;

        // 1) Try SOAP header
        var idx = request.Headers.FindHeader(headerName, string.Empty);
        if (idx >= 0)
        {
            id = request.Headers.GetHeader<string>(idx);
        }

        // 2) Fallback to HTTP header (webHttpBinding / REST)
        if (id is null &&
            request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var propObj) &&
            propObj is HttpRequestMessageProperty httpProp)
        {
            id = httpProp.Headers[headerName];
        }

        return Correlation.Begin(logger, id);
    }

    /// <summary>
    /// Called by WCF before sending the reply. Disposes the correlation scope created in
    /// <see cref="AfterReceiveRequest(ref Message, IClientChannel, InstanceContext)"/>.
    /// </summary>
    /// <param name="reply">The reply message.</param>
    /// <param name="correlationState">The object returned by <see cref="AfterReceiveRequest"/>.</param>
    public void BeforeSendReply(ref Message reply, object correlationState)
    {
        (correlationState as IDisposable)?.Dispose();
    }
}
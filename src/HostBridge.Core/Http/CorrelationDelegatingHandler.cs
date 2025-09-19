using System.Net.Http;
using HostBridge.Abstractions;

namespace HostBridge.Core.Http;

/// <summary>
/// Delegating handler that propagates the current correlation id to outbound HTTP requests.
/// </summary>
/// <remarks>
/// If the request already contains a correlation header, this handler does nothing.
/// Otherwise, it reads the current correlation id from <see cref="ICorrelationAccessor"/> and stamps it
/// using <see cref="Constants.CorrelationHeaderName"/> (or a provided header name).
/// </remarks>
public sealed class CorrelationDelegatingHandler : DelegatingHandler
{
    private readonly ICorrelationAccessor? _accessor;
    private readonly string _headerName;

    /// <summary>
    /// Creates a handler that resolves <see cref="ICorrelationAccessor"/> from the current HostBridge scope if available
    /// and uses the default header name from <see cref="Constants.CorrelationHeaderName"/>.
    /// </summary>
    public CorrelationDelegatingHandler() : this(null, null) { }

    /// <summary>
    /// Creates a handler with an optional accessor and custom header name.
    /// </summary>
    /// <param name="accessor">Accessor used to read the current correlation id. If <c>null</c>, the handler will attempt to resolve one from HostBridge.</param>
    /// <param name="headerName">Optional header name; defaults to <see cref="Constants.CorrelationHeaderName"/> when null or whitespace.</param>
    public CorrelationDelegatingHandler(ICorrelationAccessor? accessor, string? headerName = null)
    {
        _accessor = accessor;
        _headerName = string.IsNullOrWhiteSpace(headerName) ? Constants.CorrelationHeaderName : headerName!;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request != null && !request.Headers.Contains(_headerName))
        {
            ICorrelationAccessor? accessor = _accessor;
            if (accessor is null)
            {
                try { accessor = HB.TryGet<ICorrelationAccessor>(); }
                catch (InvalidOperationException)
                {
                    // HB not initialized; nothing to stamp.
                    accessor = null;
                }
            }

            var id = accessor?.CorrelationId;
            if (!string.IsNullOrWhiteSpace(id))
            {
                request.Headers.TryAddWithoutValidation(_headerName, id);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
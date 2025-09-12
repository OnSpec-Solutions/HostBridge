using HostBridge.Abstractions;

namespace HostBridge.Core;

/*
services.AddTransient<CorrelationHeaderHandler>();
services.AddHttpClient("default").AddHttpMessageHandler<CorrelationHeaderHandler>();
*/
public sealed class CorrelationHeaderHandler(ICorrelationAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = accessor.CorrelationId;
        if (!string.IsNullOrWhiteSpace(id) && !request.Headers.Contains(Constants.CorrelationHeaderName))
        {
            request.Headers.Add(Constants.CorrelationHeaderName, id);
        }

        return base.SendAsync(request, ct);
    }
}
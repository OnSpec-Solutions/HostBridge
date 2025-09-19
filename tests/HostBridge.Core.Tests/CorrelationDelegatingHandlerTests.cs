using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HostBridge.Abstractions;
using HostBridge.Core;
using HostBridge.Core.Http;
using Xunit;

namespace HostBridge.Core.Tests;

public class CorrelationDelegatingHandlerTests
{
    [Fact]
    public async Task Stamps_header_when_missing_and_correlation_present()
    {
        var handler = new CorrelationDelegatingHandler(new CorrelationAccessor());
        var stub = new CaptureHandler();
        handler.InnerHandler = stub;
        var client = new HttpClient(handler);

        var cid = "abc123";
        using (Correlation.Begin(logger: null, correlationId: cid, headerName: Constants.CorrelationHeaderName))
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
            await client.SendAsync(req);
        }

        Assert.True(stub.CapturedHeaders.TryGetValues(Constants.CorrelationHeaderName, out var values));
        Assert.Equal(cid, values!.First());
    }

    [Fact]
    public async Task Does_not_overwrite_existing_header()
    {
        var handler = new CorrelationDelegatingHandler();
        var stub = new CaptureHandler();
        handler.InnerHandler = stub;
        var client = new HttpClient(handler);

        var existing = "preexisting";
        using (Correlation.Begin(logger: null, correlationId: "ignored", headerName: Constants.CorrelationHeaderName))
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
            req.Headers.TryAddWithoutValidation(Constants.CorrelationHeaderName, existing);
            await client.SendAsync(req);
        }

        Assert.True(stub.CapturedHeaders.TryGetValues(Constants.CorrelationHeaderName, out var values));
        Assert.Equal(existing, values!.First());
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public System.Net.Http.Headers.HttpRequestHeaders CapturedHeaders { get; private set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedHeaders = request.Headers;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
    [Fact]
    public async Task Default_ctor_stamps_when_HB_initialized()
    {
        // Arrange HB with an accessor registered
        HB._ResetForTests();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
        var sp = services.BuildServiceProvider();
        HostBridge.Tests.Common.Fakes.FakeLegacyHost host = new(sp);
        HB.Initialize(host);

        var handler = new CorrelationDelegatingHandler();
        var stub = new CaptureHandler();
        handler.InnerHandler = stub;
        var client = new HttpClient(handler);

        var cid = "zzz999";
        using (Correlation.Begin(logger: null, correlationId: cid, headerName: Constants.CorrelationHeaderName))
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
            await client.SendAsync(req);
        }

        Assert.True(stub.CapturedHeaders.TryGetValues(Constants.CorrelationHeaderName, out var values));
        Assert.Equal(cid, values!.First());
        HB._ResetForTests();
    }
}

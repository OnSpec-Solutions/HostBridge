using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HostBridge.Core;

namespace HostBridge.Core.Tests;

public class CorrelationHeaderHandlerTests
{
    private sealed class FakeAccessor(string? id) : ICorrelationAccessor
    {
        public string? CorrelationId => id;
    }

    private sealed class TerminalHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task Adds_header_when_id_present_and_header_absent()
    {
        var accessor = new FakeAccessor("abc123");
        var terminal = new TerminalHandler();
        var handler = new CorrelationHeaderHandler(accessor) { InnerHandler = terminal };
        var invoker = new HttpMessageInvoker(handler);

        var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        await invoker.SendAsync(req, CancellationToken.None);

        terminal.LastRequest!.Headers.TryGetValues(HostBridge.Abstractions.Constants.CorrelationHeaderName, out var values)
            .Should().BeTrue();
        values!.Should().Contain("abc123");
    }

    [Fact]
    public async Task Does_not_overwrite_existing_header()
    {
        var accessor = new FakeAccessor("should-not-be-used");
        var terminal = new TerminalHandler();
        var handler = new CorrelationHeaderHandler(accessor) { InnerHandler = terminal };
        var invoker = new HttpMessageInvoker(handler);

        var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        req.Headers.Add(HostBridge.Abstractions.Constants.CorrelationHeaderName, "existing");
        await invoker.SendAsync(req, CancellationToken.None);

        terminal.LastRequest!.Headers.TryGetValues(HostBridge.Abstractions.Constants.CorrelationHeaderName, out var values)
            .Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("existing");
    }

    [Fact]
    public async Task Does_nothing_when_no_id()
    {
        var accessor = new FakeAccessor(null);
        var terminal = new TerminalHandler();
        var handler = new CorrelationHeaderHandler(accessor) { InnerHandler = terminal };
        var invoker = new HttpMessageInvoker(handler);

        var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        await invoker.SendAsync(req, CancellationToken.None);

        terminal.LastRequest!.Headers.Contains(HostBridge.Abstractions.Constants.CorrelationHeaderName)
            .Should().BeFalse();
    }
}

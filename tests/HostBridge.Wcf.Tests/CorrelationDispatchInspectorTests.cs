using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using FluentAssertions;
using HostBridge.Abstractions;
using HostBridge.Core;
using HostBridge.Tests.Common.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HostBridge.Wcf.Tests;

public class CorrelationDispatchInspectorTests
{
    private static (CorrelationDispatchInspector Inspector, ICorrelationAccessor Accessor) CreateSut()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { });
        services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
        var sp = services.BuildServiceProvider();
        HostBridgeWcf.Initialize(new FakeLegacyHost(sp));
        var inspector = new CorrelationDispatchInspector(Constants.CorrelationHeaderName);
        var accessor = sp.GetRequiredService<ICorrelationAccessor>();
        return (inspector, accessor);
    }

    [Fact]
    public void Given_SOAP_header_When_AfterReceiveRequest_Then_Id_set_and_cleared_on_BeforeSendReply()
    {
        var (inspector, accessor) = CreateSut();
        var req = Message.CreateMessage(MessageVersion.Soap11, "urn:action");

        var header = MessageHeader.CreateHeader(Constants.CorrelationHeaderName, string.Empty, "abc123");
        req.Headers.Add(header);

        var state = inspector.AfterReceiveRequest(ref req, channel: null!, instanceContext: null!);
        accessor.CorrelationId.Should().Be("abc123");

        var reply = Message.CreateMessage(MessageVersion.Soap11, "urn:reply");
        inspector.BeforeSendReply(ref reply, state);
        accessor.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Given_HTTP_header_When_AfterReceiveRequest_Then_Id_set_and_cleared_on_BeforeSendReply()
    {
        var (inspector, accessor) = CreateSut();
        var req = Message.CreateMessage(MessageVersion.Soap11, "urn:action");

        var http = new HttpRequestMessageProperty();
        http.Headers[Constants.CorrelationHeaderName] = "xyz789";
        req.Properties[HttpRequestMessageProperty.Name] = http;

        var state = inspector.AfterReceiveRequest(ref req, channel: null!, instanceContext: null!);
        accessor.CorrelationId.Should().Be("xyz789");

        var reply = Message.CreateMessage(MessageVersion.Soap11, "urn:reply");
        inspector.BeforeSendReply(ref reply, state);
        accessor.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Given_faulted_flow_When_BeforeSendReply_called_Then_scope_is_disposed_and_Id_cleared()
    {
        var (inspector, accessor) = CreateSut();
        var req = Message.CreateMessage(MessageVersion.Soap11, "urn:action");
        var header = MessageHeader.CreateHeader(Constants.CorrelationHeaderName, string.Empty, "fault-id");
        req.Headers.Add(header);

        var state = inspector.AfterReceiveRequest(ref req, channel: null!, instanceContext: null!);
        accessor.CorrelationId.Should().Be("fault-id");

        // Simulate a fault reply; disposal should still happen.
        var fault = Message.CreateMessage(MessageVersion.Soap11, MessageFault.CreateFault(new FaultCode("Server"), "boom"), "");
        inspector.BeforeSendReply(ref fault, state);
        accessor.CorrelationId.Should().BeNull();
    }
}
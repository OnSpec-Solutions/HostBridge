using System;
using HostBridge.Wcf;
using Xunit;

namespace HostBridge.Wcf.Tests;

public class HostBridgeWcfInitializeTests
{
    [Fact]
    public void Initialize_with_null_host_throws()
    {
        Assert.Throws<ArgumentNullException>(() => HostBridgeWcf.Initialize(null));
    }
}

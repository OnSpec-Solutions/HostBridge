using System;
using HostBridge.AspNet;
using Xunit;

namespace HostBridge.AspNet.Tests;

public class AspNetBootstrapperTests
{
    [Fact]
    public void Initialize_with_null_host_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AspNetBootstrapper.Initialize(null));
    }
}

using Xunit;

namespace HostBridge.Owin.Tests;

[CollectionDefinition("OwinNonParallel", DisableParallelization = true)]
public class OwinNonParallelCollection : ICollectionFixture<object>
{
}

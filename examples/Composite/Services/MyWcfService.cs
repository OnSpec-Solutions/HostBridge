using System.ServiceModel;
using HostBridge.Examples.Common;

namespace Composite.Services;

[ServiceContract]
public interface IMyWcfService
{
    [OperationContract]
    string DoWork();
}

public sealed class MyWcfService(IMyScoped scoped) : IMyWcfService
{
    public string DoWork()
    {
        return scoped.Id.ToString("B");
    }
}
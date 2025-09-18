using System.ServiceModel;

using HostBridge.Examples.Common;

namespace WcfService;

[ServiceContract]
public interface IMyService
{
    [OperationContract]
    string DoWork();
}

public sealed class MyService(IMyScoped scoped) : IMyService
{
    public string DoWork()
    {
        return scoped.Id.ToString("B");
    }
}
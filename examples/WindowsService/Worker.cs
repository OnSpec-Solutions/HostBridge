using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace WindowsService;

public interface IWorker
{
    Task RunAsync();
}

public sealed class Worker(ILogger<Worker> logger) : IWorker
{
    public Task RunAsync()
    {
        logger.LogInformation("Running...");
        
        return Task.CompletedTask;
    }
}
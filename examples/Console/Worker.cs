using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Examples.Console;

public interface IWorker { Task RunAsync(); }

public sealed class Worker(IOperation op, ILogger<Worker> log) : IWorker
{
    public async Task RunAsync()
    {
        log.LogInformation("Worker starting");
        await op.DoAsync();
        log.LogInformation("Worker done");
    }
}
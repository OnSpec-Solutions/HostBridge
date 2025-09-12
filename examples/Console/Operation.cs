using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Examples.Console;

public interface IOperation { Task DoAsync(); }

public sealed class Operation(ILogger<Operation> log) : IOperation
{
    public Task DoAsync()
    {
        log.LogInformation("Operation using scoped services.");
        return Task.CompletedTask;
    }
}
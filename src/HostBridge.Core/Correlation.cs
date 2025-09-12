using HostBridge.Abstractions;

namespace HostBridge.Core;

public static class Correlation
{
    public static IDisposable Begin(ILogger? logger, string? correlationId = null,
        string headerName = Constants.CorrelationHeaderName)
    {
        var id = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId!;
        var bucket = new List<IDisposable>(2)
        {
            CorrelationAccessor.Push(id)
        };
        if (logger is not null)
        {
            bucket.Add(
                logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = id, 
                    ["CorrelationHeader"] = headerName
                })!
            );
        }

        return new Composite(bucket);
    }

    private sealed class Composite(List<IDisposable> disposables) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }
}
namespace HostBridge.Core;

public interface ICorrelationAccessor
{
    string? CorrelationId { get; }
}

/*
services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
 */
public sealed class CorrelationAccessor : ICorrelationAccessor
{
    private static readonly AsyncLocal<string?> Current = new();
    public string? CorrelationId => Current.Value;

    internal static IDisposable Push(string id)
    {
        var prior = Current.Value;
        Current.Value = id;
        return new Pop(prior);
    }

    private sealed class Pop(string? prior) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }
            Current.Value = prior;
        }
    }
}
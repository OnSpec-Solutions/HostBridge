using HostBridge.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HostBridge.Tests.Common.Fakes;

// Test IHostedService that tracks lifecycle state and can optionally record calls.
// - If constructed with a calls list and name, it logs start/stop events as "{name}:start"/"{name}:stop".
// - It always sets Started/Stopped flags which can be asserted in tests.
public class TrackingHostedService(List<string> calls, string name) : IHostedService
{
    private readonly List<string>? _calls = calls;
    private readonly string? _name = name;

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_calls is not null && _name is not null)
        {
            _calls.Add(_name + ":start");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (_calls is not null && _name is not null)
        {
            _calls.Add(_name + ":stop");
        }
        return Task.CompletedTask;
    }
}

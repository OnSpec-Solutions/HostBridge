# HostBridge.Core

Lightweight helpers for working with Microsoft.Extensions.DependencyInjection in classic .NET apps. It exposes a simple static accessor (HB) for the root IServiceProvider, ambient scoping, and convenient resolution helpers.

## Install
- NuGet: `HostBridge.Core`
- Targets: net472, net48

## Quick start

1) Build a host and initialize HB once during application startup:

```csharp
// e.g., Global.asax.cs Application_Start, OWIN startup, or Program.Main
var services = new ServiceCollection();
services.AddOptions();
services.AddSingleton<IMyService, MyService>();
var sp = services.BuildServiceProvider();

var host = new LegacyHost(sp); // your ILegacyHost implementation
HostBridge.Core.HB.Initialize(host);
```

2) Resolve services where needed (after initialization):

```csharp
var svc = HB.Get<IMyService>();
var maybe = HB.TryGet<OptionalService>(); // returns null if not registered
```

3) Create explicit scopes or ambient scopes:

```csharp
using var scope = HB.CreateScope();
var scoped = scope.ServiceProvider.GetRequiredService<IMyScoped>();

using (HB.BeginScope())
{
    // HB.Current now points to a scoped provider
    var svc2 = HB.Get<IMyService>();
}
// ambient scope is restored
```

## Notes
- Call HB.Initialize(host) once per process after building your container.
- Accessing HB.Root/Current before initialization throws InvalidOperationException.
- Prefer explicit scoping around request/operation boundaries.

## See also
- Examples: ../examples
- API docs are shipped in XML with the package.
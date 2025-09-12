Coverage gaps and how to close them

This repository’s test suite drives source coverage very high across all 11 src projects. The remaining, small number of uncovered statements are either OS/event-bound or guarded branches that require elevated/hosted environments. Below is a per-area breakdown of what remains and how it can be covered if you choose to invest in higher-level or environment-coupled tests.

1) HostBridge.WindowsService
- Uncovered: The success path inside HostBridgeServiceBase.TryLog that calls EventLog.SourceExists/CreateEventSource/WriteEntry without throwing.
- Why: Writing to the Windows Event Log requires specific OS-level permissions and often administrator rights and registry access. In CI/dev without elevation, these calls are intentionally wrapped in try/catch and will hit the exception path (which we already cover indirectly by exercising stop/start failure handling).
- How to cover:
  - Run tests on a Windows agent with administrative permissions and a registry writeable environment.
  - Add an integration test marked [Trait("RequiresAdmin", "true")] that creates a derived service with a deterministic ServiceName, calls protected OnStop/OnShutdown via reflection to trigger TryLog, and then cleans up the event source.
  - Important: gate this test behind an environment variable (e.g., HB_ENABLE_EVENTLOG_TESTS=1) to avoid breaking normal dev/CI.

2) HostBridge.Core – LegacyHostRunExtensions.RunConsoleAsync
- Uncovered: The lines wiring and unwiring OS events (Console.CancelKeyPress += …, AppDomain.CurrentDomain.ProcessExit += …) and the branch where cancellation is triggered by those events.
- Why: Raising Console.CancelKeyPress or AppDomain.ProcessExit deterministically from a unit test is not supported; they are process-level events. The implementation is correct (symmetrical subscription/unsubscription and cancellation), but unit tests cannot easily drive them.
- How to cover:
  - Add an end-to-end console harness (e.g., in examples or an integration test project) that launches a child process running a tiny program calling RunConsoleAsync with a short shutdown timeout.
  - From the parent test, send CTRL+C to the child process (GenerateConsoleCtrlEvent on Windows) or terminate the process to trigger ProcessExit. Observe that the child exits cleanly within the timeout.
  - Alternatively, abstract the event source behind an injectable IConsoleLifetime to fake the events in tests, but that would be a public API change and is not recommended per the repo’s stability guidelines.

3) HostBridge.Diagnostics – HostBridgeVerifier.Log default switch arm
- Uncovered: The default arm in the severity switch throws ArgumentOutOfRangeException for unknown enum values.
- Why: All DiagnosticResult instances are created within our code using the Severity enum; hitting the default would require constructing an invalid enum value (e.g., (Severity)999) which is not produced by the library’s APIs.
- How to cover:
  - Add a negative test that intentionally instantiates DiagnosticResult via reflection with an invalid Severity value and passes it to HostBridgeVerifier.Add(() => new[]{ result }). Then assert that Log(logger) throws. Consider placing this in a separate test marked as a negative-case to avoid normal usage confusion. This does not add product value and is optional.

4) HostBridge.Options.Config – AppConfig provider corner cases
- Status: We now cover normal appSettings and connectionStrings mapping, including the skip branch for empty connection string names.
- Potentially uncovered: The guard that skips connection strings missing ElementInformation.Source (programmatic entries).
- How to cover (optional): Construct a Configuration object in-memory, add a ConnectionStringSettings with a valid name but without a backing configuration file element (so ElementInformation.Source is null), set it as current via ConfigurationManager.RefreshSection and ensure the provider’s Load sees and skips it. This requires a more elaborate configuration fixture and may not be worth the added complexity.

Summary
- Practically speaking, the only meaningful uncovered code paths require OS/event integration (Windows Event Log, console Ctrl+C/ProcessExit). We’ve documented how to cover them with environment-conditional integration tests. Everything else is either covered by the current suite or not valuable enough to warrant test indirection given the stability contract.

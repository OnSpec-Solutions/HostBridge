# Code coverage with JetBrains dotCover

This repository supports running code coverage with JetBrains dotCover both from Rider and from the command line. No changes to src projects are required.

## Rider / IDE

- Use the built-in Unit Tests tool window and select "Cover with dotCover".
- The repo already ignores `*.dotCover` artifacts (see .gitignore).
- Snapshots can be merged/exported from Rider. Prefer Cobertura XML if you want to publish in CI.

## Command line (Windows, PowerShell)

Prerequisites: dotCover must be installed and available on PATH, or set the `DOTCOVER_EXE` environment variable to the full path of `dotCover.exe`.

A helper script and an MSBuild target are provided:

- Script: `tools/dotCover/run-dotcover.ps1`
- Config: `tools/dotCover/dotCover.xml` (default filters)
- MSBuild target: `DotCover` in `Directory.Build.targets`

### Quick start

1) From the repository root, build the solution (optional):

   dotnet build

2) Run dotCover over the entire solution and export Cobertura XML:

   powershell -NoProfile -ExecutionPolicy Bypass -File tools/dotCover/run-dotcover.ps1 -Output coverage.dotCover.dcvr -Report coverage.dotcover.xml

This will:
- find `dotCover.exe`,
- run `dotnet test HostBridge.sln --no-build` under coverage,
- write a snapshot to `coverage.dotCover.dcvr`,
- export a Cobertura report to `coverage.dotcover.xml`.

You can change the framework or filters:

   powershell -File tools/dotCover/run-dotcover.ps1 -Framework net48 -Filters "+:module=*;-:module=*.Tests"

### MSBuild target

You can also invoke dotCover via MSBuild:

   dotnet build -t:DotCover

This uses the same script under the hood and writes outputs to the repo root.

### CI tips

- Cache the dotCover installation or use a build image that includes JetBrains dotCover.
- Publish the `coverage.dotcover.xml` artifact to your coverage service (supports Cobertura format).
- If you need to merge multiple runs, use `dotCover merge` and then `dotCover report`.

### Filters

Default filters exclude test and example modules and any `*.Generated*` classes. Adjust `tools/dotCover/dotCover.xml` or pass `-Filters` to the script for custom runs.

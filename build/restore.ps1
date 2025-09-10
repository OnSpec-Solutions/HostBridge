#Requires -Version 7

$ErrorActionPreference='Stop'

# 1) SDK-style (PackageReference)
dotnet restore .\HostBridge.sln

# 2) Legacy packages.config projects (examples/*)
# Requires nuget.exe available (winget install Microsoft.NuGet)
nuget restore .\examples\ -NonInteractive

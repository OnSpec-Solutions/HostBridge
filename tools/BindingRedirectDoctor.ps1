#Requires -Version 7

param(
    [string]$Bin = "$(Resolve-Path .)\bin"
)

Add-Type -AssemblyName 'System.Xml','System.IO.Compression.FileSystem'

$assemblies = Get-ChildItem $Bin -Filter *.dll -Recurse |
        ForEach-Object {
            try {
                $def = [System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
                [PSCustomObject]@{
                    Name=$def.Name; Version=$def.Version; PublicKeyToken=($def.GetPublicKeyToken() -join '').ToLower()
                }
            } catch {}
        } | Group-Object Name | ForEach-Object {
            $_.Group | Sort-Object Version -Descending | Select-Object -First 1
        }

"<!-- Add to <assemblyBinding> -->"
$assemblies | Sort-Object Name | ForEach-Object {
    $old = "0.0.0.0-{0}.{1}.{2}.{3}" -f $_.Version.Major,$_.Version.Minor,$_.Version.Build,$_.Version.Revision
    @"
<dependentAssembly>
  <assemblyIdentity name="$($_.Name)" publicKeyToken="$($_.PublicKeyToken)" culture="neutral" />
  <bindingRedirect oldVersion="$old" newVersion="$($_.Version)" />
</dependentAssembly>
"@
}

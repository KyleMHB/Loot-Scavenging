# Builds both game versions and copies the runtime files to the local RimWorld Mods folder.
# Replaces the deployed copies of these folders.
$ErrorActionPreference = "Stop"

# A git worktree lives outside the workspace, so RIMWORLD_SHARED_ROOT can point at the workspace's _Shared folder.
$sharedRoot = if ($env:RIMWORLD_SHARED_ROOT) { $env:RIMWORLD_SHARED_ROOT } else { Join-Path $PSScriptRoot "..\_Shared" }
. (Join-Path $sharedRoot "RimWorldModTools.ps1")

$project = Join-Path $PSScriptRoot "Source\LootScavenging.csproj"
Invoke-RimWorldDotNetBuild -ProjectPath $project -BuildArguments @('/p:UseSharedCompilation=false', '/p:RimWorldVersion=1.6')

# Runtime folders copied to the Mods folder.
$folders = @("About", "1.6", "1.7", "Defs", "Patches", "Languages", "Textures") |
    Where-Object { Test-Path -LiteralPath (Join-Path $PSScriptRoot $_) }

Invoke-RimWorldModDeploy `
    -ModName "Loot Scavenging" `
    -SourceRoot $PSScriptRoot `
    -BuildPath $project `
    -Configuration "Release" `
    -Folders $folders `
    -Files @("LoadFolders.xml") `
    -RemoveFilePatterns @("*.pdb")

# Builds both game versions and copies the runtime files to the local RimWorld Mods folder.
# Replaces the deployed copies of these folders.
$ErrorActionPreference = "Stop"

# A git worktree lives outside the workspace, so RIMWORLD_SHARED_ROOT can point at the workspace's _Shared folder.
$sharedRoot = if ($env:RIMWORLD_SHARED_ROOT) { $env:RIMWORLD_SHARED_ROOT } else { Join-Path $PSScriptRoot "..\_Shared" }
. (Join-Path $sharedRoot "RimWorldModTools.ps1")

$project = Join-Path $PSScriptRoot "Source\LootScavenging.csproj"

# Claim the shared RimWorld install for this checkout first, so this deploy cannot overwrite a tree another checkout is testing.
python (Join-Path $PSScriptRoot "tools\ls_test.py") lease take --purpose deploy
if ($LASTEXITCODE -ne 0) { throw "Another checkout holds the RimWorld install; see the message above, or run: python tools/ls_test.py status" }

Invoke-RimWorldDotNetBuild -ProjectPath $project -BuildArguments @('/p:UseSharedCompilation=false', '/p:RimWorldVersion=1.6')

# Runtime folders copied to the Mods folder. Keep this list equal to DevTests/devtests.json runtimeFolders.
$folders = @("About", "1.6", "1.7", "Defs", "Patches", "Languages", "Textures", "packs") |
    Where-Object { Test-Path -LiteralPath (Join-Path $PSScriptRoot $_) }

Invoke-RimWorldModDeploy `
    -ModName "Loot Scavenging" `
    -SourceRoot $PSScriptRoot `
    -BuildPath $project `
    -Configuration "Release" `
    -Folders $folders `
    -Files @("LoadFolders.xml") `
    -RemoveFilePatterns @("*.pdb")

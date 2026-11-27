# Deploys the working tree and opens RimWorld for playing by hand with Harmony, Core and this mod only.
# The game writes to work/userdata/minimal, so the player's own mod list, settings and saves are untouched.
# Usage: powershell -ExecutionPolicy Bypass -File tools/play.ps1 [-NoDeploy]
param([switch]$NoDeploy)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

if (Get-Process RimWorld-ErrorAction SilentlyContinue) { throw "RimWorld is already running; close it first (test sessions: python tools/ls_test.py close-session)." }

if (-not $NoDeploy) {
    & (Join-Path $root "deploy.ps1")
    # deploy takes the install lease; release it so the in-game tests are not blocked once the game closes.
    python (Join-Path $root "tools\ls_test.py") lease release
}

$local = Join-Path $root "..\RimWorldDevTests\rimtest.local.json"
$gameDir = if (Test-Path $local) { (Get-Content $local -Raw | ConvertFrom-Json).gameDir } else { "E:/Steam/steamapps/common/RimWorld" }

$userdata = Join-Path $root "work\userdata\minimal"
$modsConfig = Join-Path $userdata "Config\ModsConfig.xml"
if (-not (Test-Path $modsConfig)) {
    New-Item -ItemType Directory -Force (Split-Path $modsConfig) | Out-Null
    @"
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <activeMods>
    <li>brrainz.harmony</li>
    <li>ludeon.rimworld</li>
    <li>kylemhb.lootscavenging</li>
  </activeMods>
</ModsConfigData>
"@ | Set-Content -Encoding utf8 $modsConfig
}

Start-Process -FilePath (Join-Path $gameDir "RimWorld.exe") -ArgumentList "-savedatafolder=`"$((Resolve-Path $userdata).Path)`"" -WorkingDirectory $gameDir
Write-Host "Started RimWorld with the minimal profile at $userdata"

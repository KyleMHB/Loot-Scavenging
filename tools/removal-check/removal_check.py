"""Load a Loot Scavenging save with Loot Scavenging removed and report what the map kept.

  python tools/removal-check/removal_check.py [<save.rws>] [--timeout 600]

With no save named, it takes the newest save the lootscavenging-saves batch archived. Adapted from Spearfishing's
tools/removal-check (same author). The harness can't do this: its runner ships inside Loot Scavenging. Instead this builds a tiny developer-only probe mod, installs
it in RimWorld/Mods for the one launch, and runs RimWorld 1.7 with Harmony, Core and the probe in an isolated
save-data folder (work/removal-check/userdata), so the player's mod list and saves are never touched. The probe loads the
save, runs 600 ticks, writes work/removal-check/report.txt and quits. The probe is removed afterwards, and Player.log is
copied next to the report. Refuses while any RimWorld is running. Take the shared harness lock first when another agent may
be running tests.
"""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
GAME = Path(os.environ.get("RIMWORLD_DIR", r"E:\Steam\steamapps\common\RimWorld"))
SAVE_DATA = Path.home() / "AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios"
HARNESS_USERDATA = Path(os.environ.get("RIMWORLD_DEV_TESTS_ROOT", str(ROOT.parent / "RimWorldDevTests"))) / "work/userdata/loot"
WORK = ROOT / "work/removal-check"
PROBE_MOD = GAME / "Mods/LootScavengingRemovalProbe"
MODS = ["brrainz.harmony", "ludeon.rimworld", "kylemhb.lootscavenging.removalprobe"]


def rimworld_running():
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq RimWorld.exe", "/NH"], capture_output=True, text=True).stdout
    return "RimWorld.exe" in out


def known_expansions():
    ids = []
    for about in (GAME / "Data").glob("*/About/About.xml"):
        text = about.read_text(encoding="utf-8", errors="ignore")
        start = text.find("<packageId>")
        if start >= 0:
            ids.append(text[start + 11:text.find("</packageId>", start)].strip().lower())
    return [i for i in ids if i.startswith("ludeon.rimworld.")]


def needs_more_than_core(save):
    """Whether the save's header lists a mod or expansion the probe's Harmony + Core launch lacks."""
    with open(save, encoding="utf-8", errors="ignore") as f:
        head = f.read(4000)
    start, end = head.find("<modIds>"), head.find("</modIds>")
    if start < 0 or end < 0:
        return False
    ids = [li.split("</li>")[0].strip().lower() for li in head[start:end].split("<li>")[1:]]
    return any(i not in ("brrainz.harmony", "ludeon.rimworld", "kylemhb.lootscavenging") for i in ids)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("save", type=Path, nargs="?")
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()
    if args.save is None:
        saves = sorted((HARNESS_USERDATA / "LootScavengingTests").rglob("*.rws"), key=lambda f: f.stat().st_mtime)
        saves = [f for f in saves if "lootscavenging-saves" in str(f)] or saves
        # The probe runs Core only, so a save from a `--mods dlcs` run never finishes loading. The Full tier runs that set
        # after the default one, which would make it the newest.
        saves = [f for f in saves if not needs_more_than_core(f)] or saves
        if not saves:
            sys.exit("no archived save found; run the lootscavenging-saves batch first")
        args.save = saves[-1]
        print("save:", args.save, flush=True)
    if not args.save.is_file():
        sys.exit(f"save not found: {args.save}")
    if rimworld_running():
        sys.exit("refusing: RimWorld is running")

    subprocess.run(["dotnet", "build", str(HERE / "RemovalProbe.csproj"), "-c", "Release", "-v", "q"], check=True)
    userdata = WORK / "userdata"
    if userdata.exists():
        shutil.rmtree(userdata)
    (userdata / "Config").mkdir(parents=True)
    (userdata / "Saves").mkdir()
    prefs = HARNESS_USERDATA / "Config/Prefs.xml"
    shutil.copy2(prefs if prefs.is_file() else SAVE_DATA / "Config/Prefs.xml", userdata / "Config/Prefs.xml")
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<ModsConfigData>"]
    player_config = SAVE_DATA / "Config/ModsConfig.xml"
    if player_config.is_file():
        version = ET.parse(player_config).getroot().findtext("version")
        lines += [f"  <version>{version}</version>"] if version else []
    lines += ["  <activeMods>"]
    lines += [f"    <li>{m}</li>" for m in MODS] + ["  </activeMods>", "  <knownExpansions>"]
    lines += [f"    <li>{e}</li>" for e in known_expansions()] + ["  </knownExpansions>", "</ModsConfigData>"]
    (userdata / "Config/ModsConfig.xml").write_text("\n".join(lines) + "\n", encoding="utf-8")
    shutil.copy2(args.save, userdata / "Saves" / args.save.name)
    report = WORK / "report.txt"
    for old in (report, report.with_name("report.txt.error")):
        old.unlink(missing_ok=True)

    if PROBE_MOD.exists():
        shutil.rmtree(PROBE_MOD)
    try:
        (PROBE_MOD / "About").mkdir(parents=True)
        shutil.copy2(HERE / "About.xml", PROBE_MOD / "About/About.xml")
        shutil.copytree(WORK / "probe/1.7", PROBE_MOD / "1.7")
        command = [str(GAME / "RimWorld.exe"), f"-savedatafolder={userdata}", f"-removalProbeSave={args.save.stem}", f"-removalProbeOut={report}"]
        print("launching:", " ".join(command), flush=True)
        process = subprocess.Popen(command)
        deadline = time.time() + args.timeout
        while process.poll() is None and time.time() < deadline:
            time.sleep(2)
        if process.poll() is None:
            print("timed out; closing the probe's RimWorld", flush=True)
            process.kill()
            process.wait()
    finally:
        # Let the game release its files before removing the probe.
        for _ in range(10):
            try:
                shutil.rmtree(PROBE_MOD)
                break
            except OSError:
                time.sleep(1)
    log = SAVE_DATA / "Logs/Player.log"
    if log.is_file():
        shutil.copy2(log, WORK / "Player.log")
    for path in (report, report.with_name("report.txt.error")):
        if path.is_file():
            print(f"=== {path}\n" + path.read_text(encoding="utf-8"))
            if path != report:
                return 1
            return 1 if "FAIL" in path.read_text(encoding="utf-8") else 0
    print("no report written; see", WORK / "Player.log")
    return 1


if __name__ == "__main__":
    sys.exit(main())

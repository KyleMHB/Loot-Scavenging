"""Loot Scavenging adapter for the sibling RimWorldDevTests launcher: every command passes through with this mod's config.

  python tools/ls_test.py status
  python tools/ls_test.py run lootscavenging-smoke --seed 1
  python tools/ls_test.py close-session
"""
from pathlib import Path
import os
import runpy
import sys

ROOT = Path(__file__).resolve().parents[1]
HARNESS = Path(os.environ.get("RIMWORLD_DEV_TESTS_ROOT", str(ROOT.parent / "RimWorldDevTests")))
LAUNCHER = HARNESS / "rimtest.py"
CONFIG = ROOT / "DevTests" / "devtests.json"

if not LAUNCHER.is_file():
    sys.exit(f"shared harness not found: {LAUNCHER}; set RIMWORLD_DEV_TESTS_ROOT to its checkout")
sys.argv = [str(LAUNCHER), "--config", str(CONFIG)] + sys.argv[1:]
runpy.run_path(str(LAUNCHER), run_name="__main__")

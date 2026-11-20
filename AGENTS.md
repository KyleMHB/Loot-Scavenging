# Project instructions

Read and follow `E:\Coding\Templates\AGENTS.md`. For RimWorld game knowledge read `E:\Coding\Rimworld\AGENTS.md` and its `GAME.md`.

Loot Scavenging (packageId `kylemhb.lootscavenging`, Workshop title "Loot Scavenging (scrap, recycle, study)") is a RimWorld mod for 1.6 and 1.7. Harmony is the only hard dependency. The spec is `.scratch/loot-scavenging/spec.md`. No gameplay code exists yet.

## Originality

This mod is built from the spec and the game's own code. Looting Manager, Non uno Pinata and Technology Blueprints & Reverse Engineering state no licence. Never download, open or copy their source, text or art, and never keep a copy in this project. Where the spec is silent, read the game's code or ask.

## Layout

- `Source/`: the mod's C#. Intermediates go to `work/obj/<version>/`.
- `1.6/Assemblies`, `1.7/Assemblies`: build output, selected by `LoadFolders.xml`. There is no root `Assemblies/`.
- `Defs/`, `Patches/`, `Languages/English/Keyed/`, `Textures/`: created when first needed.
- `.scratch/<feature>/`: specs and tickets. `docs/agents/`: tracker conventions.

## Commands

- Build 1.7: `dotnet build Source/LootScavenging.csproj -c Release`
- Build 1.6: `dotnet build Source/LootScavenging.csproj -c Release -p:RimWorldVersion=1.6` (net472 against `Krafs.Rimworld.Ref` 1.6.4871; the installed game is 1.7, so 1.6 is compile-checked only)
- Translation check: runs after every build; by hand, `python ../RimWorldTranslate/rwtranslate.py --mod . check`
- Deploy to `RimWorld/Mods/Loot Scavenging` (both builds): `powershell -ExecutionPolicy Bypass -File deploy.ps1`

## Text

Player-facing C# text goes through `"LootScavenging_...".Translate()` with English in `Languages/English/Keyed/LootScavenging.xml`; mark developer-only literals `// not translated`.

## Posts

Development posts go to one thread in the Progression pack Discord's forum channel, as replies under the base post. Drafts live in `work/posts/<date>-<slug>/post.md`. Discord caps a message at 2,000 characters. Posts do not carry the AI disclosure (Kyle's choice, 2026-11-20).

## Agent skills

### Issue tracker

For specs and tickets, use local Markdown under `.scratch/<feature>/`. See [issue tracker](docs/agents/issue-tracker.md).

### Triage labels

For triage status, use the five canonical labels. See [triage labels](docs/agents/triage-labels.md).

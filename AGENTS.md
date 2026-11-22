# Project instructions

Read and follow `E:\Coding\Templates\AGENTS.md`. For RimWorld game knowledge read `E:\Coding\Rimworld\AGENTS.md` and its `GAME.md`.

Loot Scavenging (packageId `kylemhb.lootscavenging`, Workshop title "Loot Scavenging") is a RimWorld mod for 1.6 and 1.7. Harmony is the only hard dependency. The spec is `.scratch/loot-scavenging/spec.md`. No gameplay code exists yet.

## Originality

This mod is built from the spec and the game's own code. Looting Manager, Non uno Pinata and Technology Blueprints & Reverse Engineering state no licence. Never download, open or copy their source, text or art, and never keep a copy in this project. Where the spec is silent, read the game's code or ask.

## Layout

- `Source/`: the mod's C#. Intermediates go to `work/obj/<version>/`.
- `1.6/Assemblies`, `1.7/Assemblies`: build output, selected by `LoadFolders.xml`. There is no root `Assemblies/`.
- `Defs/`, `Patches/`, `Languages/English/Keyed/`, `Textures/`: created when first needed.
- `Tests/LootScavenging.Tests`: xunit tests of `Source/Model/`, run by `dotnet test`.
- `translation/`: the translation glossary and the record of the English each translation was made from. Committed, not shipped. `Languages/ChineseSimplified` and `Languages/Russian` ship.
- `packs/`: preset patches for packs to copy into their own `Patches/`. The game never loads this folder.
- `Extras/Loot-Scavenging-AutoConfig/`: an example Auto Mod Config preset, a separate mod for a modlist curator to copy into `RimWorld/Mods`. It is deployed inside the main mod, and `LoadFolders.xml` does not name it, so the game never loads it from there. Its two settings files, one named for the local folder and one for the Workshop id 3844457170, have the same content and list every saved key and value; change them together.
- `.scratch/<feature>/`: specs and tickets. `docs/agents/`: tracker conventions.

## Versioning

The mod version lives in three places that change together: `About/About.xml` `<modVersion>`, the `[b]Mod version:[/b]` line in `steam-description.md`, and the newest `CHANGELOG.md` heading (`## <version> - <date>`). Bump the patch (0.2.1) for fixes and the minor (0.3.0) for features, once per Workshop upload. None of the three touches the DLL, so a bump keeps the release-check perf run valid.

The example preset's `<version>` in `Extras/Loot-Scavenging-AutoConfig/Defs/Preset.xml` is a separate value. Set it to the mod version the preset was written for, and change it only when the preset's content changes, not with every upload: Auto Mod Config merges a changed version into the player's settings.

## Commands

- Build 1.7: `dotnet build Source/LootScavenging.csproj -c Release`
- Build 1.6: `dotnet build Source/LootScavenging.csproj -c Release -p:RimWorldVersion=1.6` (net472 against `Krafs.Rimworld.Ref` 1.6.4871; the installed game is 1.7, so 1.6 is compile-checked only)
- Unit tests: `dotnet test` from the repo root. `Tests/LootScavenging.Tests` compiles `Source/Model/` directly, so code there must stay plain C# with no RimWorld references.
- Translation check: runs after every build; by hand, `python ../RimWorldTranslate/rwtranslate.py --mod . check`
- Stale translations: `python ../RimWorldTranslate/rwtranslate.py --mod . status` lists untranslated and outdated texts per language. The build does not fail on them.
- Deploy to `RimWorld/Mods/Loot Scavenging` (both builds): `powershell -ExecutionPolicy Bypass -File deploy.ps1`

## Text

Player-facing C# text goes through `"LootScavenging_...".Translate()` with English in `Languages/English/Keyed/LootScavenging.xml`; mark developer-only literals `// not translated`.

When English text changes (Keyed or def text), update ChineseSimplified and Russian in the same change with the `rimworld-translate` skill and `translation/glossary.md`: run `status`, then `export` and `import` for both languages, which touch only the missing and outdated texts. After editing a translation by hand, run `baseline <Language>` so the record accepts it. Add a glossary row before using a new recurring term. Before a Workshop upload, `status` must show 0 untranslated and 0 outdated for both languages (see TESTING.md, Full tier).

## Posts

Development posts go to one thread in the Progression pack Discord's forum channel, as replies under the base post. Drafts live in `work/posts/<date>-<slug>/post.md`. Discord caps a message at 2,000 characters. Posts do not carry the AI disclosure (Kyle's choice, 2026-11-20).

## Agent skills

### Issue tracker

For specs and tickets, use local Markdown under `.scratch/<feature>/`. See [issue tracker](docs/agents/issue-tracker.md).

### Triage labels

For triage status, use the five canonical labels. See [triage labels](docs/agents/triage-labels.md).

# Handoff: Loot Scavenging

Written 2026-11-20 at the end of the design session. Read this, then `spec.md` beside it. Do not read the earlier chat.

## Where things stand

- **Design is finished.** Every decision is settled and recorded in `spec.md`, section "Decisions".
- **No gameplay code exists.** The repository holds scaffolding only: metadata, build, deploy script, docs.
- **The scaffolding and spec are committed** on `main` as the first commit. There is no remote.
- **Nothing has been tested in game.**

## What the mod is

Loot Scavenging (scrap, recycle, study), packageId `kylemhb.lootscavenging`, for RimWorld 1.6 and 1.7. When a non-colony pawn's gear changes hands, a rule decides what it becomes: keep, degrade, scrap, disassemble or delete. Gear beyond the colony's research becomes stackable scrap of its era. Scrap is recycled at existing benches, where a later-era bench is needed for later-era scrap, or studied at a research bench to advance the current project. It is aimed first at the Progression collection (Workshop 3521297585).

## The rule that must not be broken

The mod is original work. Looting Manager (2002001082), Non uno Pinata (1778821244) and Technology Blueprints & Reverse Engineering (2010527094) state no licence.

- Never download, open or copy their source, text or art.
- Work from `spec.md` and the game's own code. Where the spec is silent, read the game's code or ask Kyle.
- The design session read Looting Manager's source and the file and patch structure of Non uno Pinata. That is why implementation starts in a fresh session from the spec alone.

## Files

| Path | What it is |
| --- | --- |
| `.scratch/loot-scavenging/spec.md` | The spec. Status `needs-triage`. |
| `AGENTS.md` | Project instructions, commands, layout, originality rule. |
| `Source/LootScavenging.csproj` | Build for 1.7 (default, net9.0 against the installed game) and 1.6 (net472, Krafs reference). Same pattern as Spearfishing. |
| `About/About.xml` | Metadata. Declares Looting Manager incompatible. |
| `deploy.ps1` | Builds both versions and copies to `RimWorld/Mods/Loot Scavenging`. Uses `..\_Shared\RimWorldModTools.ps1`. |
| `docs/agents/` | Local Markdown tracker conventions, copied from Spearfishing. |
| `README.md`, `CHANGELOG.md`, `LICENSE` | Template-backed. Licence is GPLv3, copied from Spearfishing; Kyle has not confirmed that choice for this mod. |

The design doc the spec was written from: https://claude.ai/artifact/WFrwwPtCFzMNo7rbTxdsCK

## Next steps

1. Kyle reads `spec.md` and confirms it, including the GPLv3 licence.
2. Turn the spec into tickets under `.scratch/loot-scavenging/issues/`, following the nine-stage build order in the spec. This is a multi-session build, so the route is `to-tickets`, then `implement` per ready ticket.
3. Before the first ticket, confirm in a decompiler the exact signatures and call order of `Pawn.Kill`, `Pawn_HealthTracker.MakeDowned` and `Pawn.Strip` on both versions. The spec's hook plan depends on them.
4. Stages 1 to 5 make a playable mod in the default mode. Stage 6, original-item mode, is the costliest.

## Facts found during the session

- The installed game is RimWorld 1.7.5014 at `E:\Steam\steamapps\common\RimWorld`. It uses a new layout: `Assembly-CSharp.dll` sits in the install root, with no `RimWorldWin64_Data\Managed` folder.
- These names exist in that assembly, checked by string search only: `DropAndForbidEverything`, `DropAllEquipment`, `DropAllNearPawn`, `FactionUtility.HostileTo`, `SmeltProducts`, `ButcherProducts`, `questTags`, `CompUniqueWeapon`, `CompBladelinkWeapon`, `Precept_Relic`, `IsResearchFinished`, `ResearchManager`, `AddProgress`, `TechprintRequirementMet`, `JobDriver_ApplyTechprint`, `EquipmentUtility`, `CanEquip`, `WorkGiver_Researcher`. Signatures are not confirmed.
- A commenter on Non uno Pinata describes 1.7 as unstable. In the Progression collection, 863 of 892 mods are tagged 1.6 and 574 are tagged 1.7.
- Looting Manager and Non uno Pinata are not in the Progression collection. Combat Extended and Yayo are not either; the pack uses Progression: Ammunition.
- The Workshop evidence cache is at `C:\Users\Kyle\.codex\data\steam-workshop\steam-workshop.sqlite`. It holds the three reviewed mods with comments, and metadata for the collection.

## Unverified, carried from the spec

- Where the Progression pack stores the colony's era, and which benches and materials it has per era. The recycling tier table uses vanilla benches as a starting point.
- How the game treats points added to a project whose techprints are missing.
- How Research Papers and Semi Random Research react to a project completed by study.
- Whether a Workshop mod is already called Loot Scavenging. A web search found none.

## Open with Kyle

- Scrap art: one icon per era tier. Kyle makes or commissions it; use placeholders until then.
- The `handoff` skill named in the request was not installed, so this file was written by hand.

## Suggested first message for the new chat

> Read `.scratch/loot-scavenging/handoff.md` and `.scratch/loot-scavenging/spec.md`. Then break the spec into tickets under `.scratch/loot-scavenging/issues/`, following its build order.

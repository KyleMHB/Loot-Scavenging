# 10: Corpses and butchering

**What to build:** A corpse rule with its own chance and separate humanlike, animal and mechanoid toggles. The corpse's gear goes through the loot rules first, and the owner and place checks apply to corpses too. A separate option removes worn apparel when a corpse is butchered. See the spec's "Corpses and butchering" section.

**Blocked by:** 01

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] With humanlike removal on, a dead raider's corpse is removed after its gear has gone through the loot rules.
- [ ] A wild animal's corpse stays unless the "no faction" group is on.
- [ ] A pawn that dies without leaving a corpse, or is killed off-map, is skipped with no error.
- [ ] With the butchering option on, butchering a corpse leaves no worn apparel behind, while gear dropped at death is unaffected.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/10

**Built:** `Source/Model/CorpseEvaluator.cs` (plain C#: `CorpseRules`, `CorpseCandidate`, `CorpseEvaluator.Evaluate` with an injectable roll, `RemovesApparelOnButchering`), `Source/Loot/CorpseHooks.cs` (glue), `Source/Loot/CorpseDebugActions.cs`, and a `corpses` field plus Scribe lines in `LootScavengingSettings`. Tests: `CorpseEvaluatorTests`; `dotnet test` 40 passed. Both builds pass with no warnings; translation check passes.

**Decisions:**
- The corpse rule repeats the loot rules' owner and place checks (same toggles, same `homeMapOnly`) inside `CorpseEvaluator` instead of editing `LootEvaluator`, which tickets 03 and 09 are changing. If the checks change there, change them here too.
- Removal is a postfix on `Pawn.Kill`, after the existing prefix has sorted gear and the game has dropped equipment and inventory. The owner is read in a prefix because a prisoner's status may not survive the death.
- Removal calls `Strip(notifyFaction: false)` before destroying the corpse, so apparel the loot rules kept lands on the ground (Cleanup keeps Good or better armour) instead of vanishing. Dead pawns are not sorted again on strip.
- Corpses that are quest-tagged, not spawned (caravan, container) or absent are skipped. Entities and other pawns that are not humanlike, animal or mechanoid are kind `Other` and never removed.
- Butchering: a prefix and finalizer on `Toils_Recipe.CalculateIngredients` mark a butchery bill (recipe has `SpecialProductType.Butchery`); a `Pawn.Strip` prefix then destroys the dead pawn's worn apparel when the option is on and the owner and place checks pass. Player strip jobs and other bills are unaffected. The owner at butchering is read from the dead pawn, so a butchered former colony prisoner may count as no faction or its old faction.
- Code defaults: all corpse toggles and the butchering option off, chance 100% (a 0 to 1 float).

**In-game: pending** (dev actions under "Loot Scavenging": Log corpse rules, Toggle corpse removal humanlike/animal/mechanoid, Toggle butchering removes apparel):
- Humanlike on: kill a raider; the log shows `Corpse ... -> Remove`, the corpse is gone, and good apparel lies on the ground.
- Wild animal: kill one with animal on; the log shows `OwnerGroupOff` and the corpse stays. After "Toggle no-faction group" it is removed.
- Kill a pawn with no corpse (off-map or caravan): no error.
- Butchering on: butcher a raider corpse (humanlike removal off); no apparel is left, and the weapon dropped at death is still there.

### 2026-11-21: review fixes

- The two `Pawn.Kill` postfixes now run in a fixed order through `HarmonyPriority` (they share one Harmony id, so `HarmonyBefore`/`After` cannot order them): `ScrapOnBodyHook` (High) puts set-aside scrap in the dead pawn's inventory first, then `CorpseRemovalHook` (Low) strips and removes the corpse, and the strip drops that scrap to the ground. Before, the order was undefined, and scrap added after the corpse was destroyed was lost. The set-aside entry is now always used up: if the death was cancelled the pawn keeps the scrap in its inventory, and a pawn with no inventory gets it on the ground rather than destroyed. A finalizer forgets the entry when Kill throws. `CorpseRemovalHook`'s prefix and `ButcherStripHook` now check `__runOriginal` (at `Priority.Last`). Glue only; in-game: pending (scrap-on-body plus Cleanup corpse removal leaves the scrap on the ground).

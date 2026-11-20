# 05: Study at a research bench

**What to build:** A researcher carries scrap to a research bench and uses it up for research points, as a job in the Research work type. Points go to the project currently being researched if it is of the scrap's era or lower, through the game's research manager, and study can finish a project. When nothing is eligible the job waits and the scrap stays in storage. Settings cover which eras can be studied, recycle or study, and techprint projects. The scrap's inspect text shows the points per unit and whether it can be studied now. See the spec's "Study" section and the requirements learned from Technology Blueprints.

**Blocked by:** 03

**Status:** done

**Recommended model:** `opus` — A custom job and work giver, research-factor and techprint edge cases, and the spec's lessons from Technology Blueprints.

- [ ] Studying same-era scrap advances the current project, and finishing a study shows the gain and the project.
- [ ] Higher-era scrap is refused, and the inspect text says why.
- [ ] With no current project the job does not start.
- [ ] A storyteller research factor of zero, and one above 100%, write no invalid value into project progress.
- [ ] With the techprint setting off, projects that need techprints receive nothing.
- [ ] Study time scales with the points paid, research speed applies, and the researcher gains Intellectual experience.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/05, reworked as bills

The first version was a free-running work giver. It was reworked so that study is a bill with the usual bill controls (spec, "Requirements learned from Technology Blueprints") and "study first, then recycle" works. These notes describe the current code.

Built:
- `Source/Model/Study.cs` (plain C#, tested in `Tests/LootScavenging.Tests/StudyTests.cs`):
  - Settings enums: `StudyEraLimit` (ColonyEraAndBelow default, AnyEra, None) and `RecycleOrStudy` (OneOrTheOther default, StudyThenRecycle).
  - `StudyVerdict` and `ProjectFacts`.
  - `StudyRules` functions: `Check` (eligibility), `Payable`, `PaidPerUnit`, `Points`, `UnitsUsed`, `BatchUnits`, `WorkAmount`, `KeepsStudying` (keep-in-stock) and `StudiedScrapUnits`.
  - `Source/Model/StudiedYield.cs`: `StudiedYield.Reduce` scales a tier's yield lines by the studied factor and keeps the expected amount exact.
  - Seams tested: these functions.
- `Source/Study/Building_StudyBench.cs`:
  - Research benches are not bill givers in vanilla. In the implied-def pass, every def whose thingClass is exactly `Building_ResearchBench` gets `Building_StudyBench`, which subclasses it and adds `IBillGiver` with a saved `BillStack`. Research is unchanged.
  - The game's bills tab casts to a work table, so the bench gets its own `ITab_StudyBills`: the bill list plus an add-bill menu, with no clipboard paste.
  - Benches with their own subclass from another mod keep it and get no study bills.
- `Source/Study/StudyRecipes.cs`:
  - A postfix on `DefGenerator.GenerateImpliedDefs_PreResolve`, the same hook as ticket 04, converts the benches and generates one `LS_Study_<Tier>` RecipeDef per tier.
  - Each recipe's ingredient is a batch of that tier's scrap. Work is `WorkAmount(batch, points)`, workSpeedStat is ResearchSpeed and workSkill is Intellectual. The Research work type is required.
  - Recipes are attached through recipeUsers, and the bench list is written into `LS_DoBillsStudy`'s `fixedBillGiverDefs`.
- `LS_DoBillsStudy` (`Defs/Study.xml`): a `WorkGiver_DoBill` in Research at priorityInType 110. The list is sorted descending, so it runs ahead of vanilla research at 100.
- `RecipeWorker_Study`:
  - The game's consume step is a no-op for this recipe. On completion it uses up only the units the project still needs (`UnitsUsed`) and leaves the rest at the bench.
  - It pays the points through `ResearchManager.AddProgress` and shows a message with the points and the project. If the scrap is no longer eligible at completion (the project changed), nothing is used up.
  - In StudyThenRecycle it places one unit of studied scrap per studied unit.
- `StudyBillPatch`, a postfix on `Bill_Production.ShouldDoNow`:
  - A study bill does not start with no project, a project above the scrap's era, the techprint setting off for a techprint project, a zero research factor, study switched off, or a bench that cannot research the project.
  - The verdict is cached per tick and tier.
- Keep-in-stock: `RecipeWorkerCounter_Study` counts that tier's scrap on the map. On a study bill, "Do until you have X" means keep X units in stock: an iteration starts only when a whole batch fits above X (`KeepsStudying`).
  - The vanilla counter can only stop when the count rises to X. Study lowers the count, so the patch replaces the TargetCount test for study bills.
  - The bill's pause and unpause thresholds and the include-from-stockpile option are not used.
  - The shown count is the scrap on the map.
- Studied scrap (`Defs/Study.xml`): `LS_StudiedScrap_<Tier>` items. They have no study points, so `TryGetWorth` is false and they are never studied.
  - Ticket 04's `ScrapTierDef` gained `studiedScrap` and `studiedYieldFactor` (default 0.5), set in `Defs/ScrapTiers.xml`. `RecycleRecipes` generates `LS_RecycleStudied_<Tier>` for the same benches as the tier's scrap, so the bench gate holds.
  - Fixed yield comes from `StudiedYield.Reduce` (a whole part plus a chance line for the fraction). `ScrapThing.SmeltProducts` rolls the reduced chances.
  - Studied scrap keeps the base market value of 0.01; the scrap value setting applies only to fresh scrap.
- `Source/Study/ScrapStudy.cs`: the `StudyPoints` DefModExtension and `StudyWorth`. Ticket 07 extends two lookups:
  - `ScrapStudy.TryGetWorth` is the single "what is this worth" lookup. `StudyWorth.UnitsPerThing` lets one item count as several scrap units.
  - `ScrapStudy.IngredientDefs(scrap)` is what each tier's bill filter allows. Ticket 07 adds unfamiliar items there, so the bill filter lets the player choose them.
- `Source/Study/CompStudyInfo.cs`: inspect lines on scrap: points per unit, and either "can be studied now for X, with a study bill" or why not.
- `Defs/ThingDefs/Scrap.xml`: `StudyPoints` per def (neolithic 10, medieval 20, industrial 30, spacer 50, ultratech 70, archotech 100) and the inspect comp on `LS_ScrapBase`. These could later move into `ScrapTierDef`.
- Settings `studyEras`, `recycleOrStudy` and `studyTechprintProjects` (default off). No UI yet (ticket 11).
- Dev actions under "Loot Scavenging":
  - "Study: spawn 20 scrap of each tier" (map tool)
  - "Study: log eligibility per tier"
  - "Study: cycle study eras"
  - "Study: toggle recycle or study"
  - "Study: toggle techprint projects"
  - "Study: cycle storyteller research factor" (1, then 0, then 2.5, then 1). This changes the save's difficulty, so cycle back to 1.

Decisions the spec left open:
- **Era comparison.** Each tier compares as its nominal era, so spacer scrap does not pay into Ultra or Archotech projects; those take ultratech and archotech scrap. Neolithic scrap counts as Animal for the colony check. A project with no tech level takes any scrap.
- **Points.** Units x fixed points x the storyteller research factor, multiplied and never divided. A zero or invalid factor stops the bill (`NoGain`). Points are capped at the project's remaining cost, and no tech-level cost factor is applied.
- **Time.** Each iteration takes enough units for about 100 base points: neolithic 10, medieval 5, industrial 4, spacer 2, ultratech 2, archotech 1. Work is 30 ticks per base point at research speed 1, about four times faster than bench research. The bill system spends work at the pawn's research speed and grants Intellectual XP at the end, like any bill.
  - The game applies a bench speed stat only to work tables, so the bench's research speed factor does not apply.
  - The storyteller factor changes the points, not the time, as at the bench.
- **Bench.** The bench must be able to research the current project (`CanBeResearchedAt`, power included).
- **Special sources** never receive points: Anomaly knowledge projects, projects with no research cost, and projects whose analysed-item or grav-engine requirement is unmet. Unfinished prerequisites and finished projects are refused too.
- **Techprints.** With the setting off, a project with unapplied techprints receives nothing. With it on, study stops one point short of finishing, because `FinishProject` would hand over the missing techprints for free. In vanilla the current project always has its techprints, so this matters for mods such as Semi Random Research.
- **Recycle or study.** The default mode produces nothing besides points. StudyThenRecycle gives one studied-scrap unit per studied unit, recycled at its tier's own bench at half yield.
- **Saves and removal.** Every saved thing carries a `Class` attribute. Without the mod, an unknown class makes `ScribeExtractor.SaveableFromNode` fall back to the def's thingClass, so the bench would survive, but with a "Could not find class" error per bench. `Source/Study/StudyBenchSaveCompat.cs` avoids that:
  - A prefix on `ScribeSaver.WriteAttribute` writes `Building_StudyBench` as `Building_ResearchBench`.
  - A postfix on `BackCompatibility.GetBackCompatibleType` maps it back when the saved def uses `Building_StudyBench`.
  - Without the mod, the bench loads as a plain research bench and its `billStack` node is never read. Study bills are lost; nothing else is.
  - Saves made before this change carry the old class name. They load fine with the mod and log the fallback error once per bench without it.
- **Bench speed factor gap.** The game applies a bill's `workTableSpeedStat` only to `Building_WorkTable`. Study time therefore uses the pawn's research speed and ignores the bench's research speed factor (hi-tech bench, multi-analyzer). Closing the gap needs a patch on `Toils_Recipe.DoRecipeWork` or a custom pawn stat.

Tests: `dotnet test` 163 passed. Both builds 0 warnings, 0 errors; `rwtranslate check` passes.

In-game pending:
- A research bench shows a Bills tab offering the four study bills, and research still works at it.
- A same-era study bill advances the project and shows the message, and can finish a project.
- Higher-era scrap: the bill waits and the inspect text gives the reason.
- No current project: no study.
- Research factor 0: no study. Factor 2.5: progress is capped at the cost.
- With the techprint setting off, a techprint project gets nothing.
- Study time scales with the tier, and Intellectual XP is gained.
- "Do until you have X" keeps X scrap.
- StudyThenRecycle gives studied scrap, which only its tier's bench recycles, at half yield.
- A modded bench keeps its own bills.
- Save and load with study bills.

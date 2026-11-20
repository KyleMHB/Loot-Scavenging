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

### 2026-11-21: implemented on ls/05

Built:
- `Source/Model/Study.cs` (plain C#, tested in `Tests/LootScavenging.Tests/StudyTests.cs`): `StudyEraLimit` (ColonyEraAndBelow default, AnyEra, None), `RecycleOrStudy` (OneOrTheOther default, StudyThenRecycle), `StudyVerdict`, `ProjectFacts`, and `StudyRules`: `Check` (eligibility), `Payable`, `PaidPerUnit`, `Points`, `UnitsToTake`, `StudyTicks`, `MaxPointsPerTrip`, `RemainsAfterStudy`. Seams tested: these functions.
- `Source/Study/ScrapStudy.cs`: glue from the game into the rules. `StudyPoints` DefModExtension; `StudyWorth`; `ScrapStudy.TryGetWorth(Thing, out StudyWorth)` is the single "what is this worth" lookup and `ScrapStudy.Candidates(Map)` the candidate list; ticket 07 extends both for unfamiliar originals (`StudyWorth.UnitsPerThing` lets one item count as several scrap units). `StudyRemains.Handler` is the hand-off for StudyThenRecycle.
- `Source/Study/WorkGiver_StudyScrap.cs`, `JobDriver_StudyScrap.cs`, `Defs/Study.xml`: work giver in the Research work type (priorityInType 110, above vanilla research's 100; the list is sorted descending) scanning research benches; the job reserves bench, seat and scrap, carries the scrap to the bench, keeps it in hand while studying, then uses it up and calls `ResearchManager.AddProgress`. Completion message names the points and the project; the game's own completion dialog shows when it finishes.
- `Source/Study/CompStudyInfo.cs`: inspect lines on scrap (points per unit; can be studied now for X, or why not).
- `Defs/ThingDefs/Scrap.xml`: `StudyPoints` per def (neolithic 10, medieval 20, industrial 30, spacer 50) and the inspect comp on `LS_ScrapBase`. Ticket 04's tier table and this extension can be consolidated later.
- Settings `studyEras`, `recycleOrStudy`, `studyTechprintProjects` (default off), Scribe lines appended at the end. No UI (ticket 11).
- Dev actions under "Loot Scavenging": "Study: spawn 20 scrap of each tier" (map tool), "Study: log eligibility per tier", "Study: cycle study eras", "Study: toggle recycle or study", "Study: toggle techprint projects", "Study: cycle storyteller research factor" (1 -> 0 -> 2.5 -> 1; this changes the save's difficulty, so cycle back to 1).

Decisions the spec left open:
- Era comparison: each tier compares as its nominal era. Spacer scrap does not pay into Ultra or Archotech projects. Neolithic scrap counts as Animal for the colony check, so an Animal-era colony can study it. A project with no tech level takes any scrap.
- Points: units x fixed points x the storyteller research factor, multiplied and never divided. A zero or invalid factor gives `NoGain` and no job starts, so no scrap is used up for nothing. Points are capped at the project's remaining cost. No tech-level cost factor is applied, because the era rules already gate study.
- Time: 30 ticks per point at research speed 1, about four times faster than bench research (about 121 ticks per point). Research speed is pawn ResearchSpeed x bench ResearchSpeedFactor, floored at 0.05. One trip lasts at most 5000 ticks and takes no more units than the project needs. XP is 0.1 Intellectual per tick, as at the bench.
- The bench must be able to research the current project (`CanBeResearchedAt`, power included), as in vanilla research.
- Special sources that never receive points: Anomaly knowledge projects, projects with no research cost, projects whose analysed-item or grav-engine requirement is unmet. Unfinished prerequisites and finished projects are refused too.
- Techprints: with the setting off, a project with unapplied techprints receives nothing. With it on, study pays in but stops one point short of finishing, because `FinishProject` would hand over the missing techprints for free. In vanilla the research tab only lets a project become current when its techprints are applied, so this mostly matters for mods that set the project another way, such as Semi Random Research.
- Recycle or study: StudyThenRecycle passes ceil(units x 0.5) remaining units to `StudyRemains.Handler`. Until ticket 04 sets the handler, nothing remains (dev log line). Ticket 04 decides whether the remains become yields at once or a studied-scrap item.

Open: StudyThenRecycle does nothing yet beyond the dev log line, so it behaves like OneOrTheOther. Wire it before ticket 11 shows the setting. Do not just spawn `ScrapTierTable.Yields(tier)` at the research bench, because that skips the rule that the recycling bench is the gate. With AnyEra, a tribal colony would get plasteel with no fabrication bench. The two options are:
- the remains become a per-tier "studied scrap" item, recycled at that tier's bench at reduced yield;
- yields appear at once, but only when the map has a bench that can recycle the tier.

Either way, the remains must never come back as studyable scrap, because rounding up would let one unit be studied forever. The points in `StudyPoints` could also move into ticket 04's `ScrapTierDef`.

Open (spec): study has no keep-in-stock count. At priority 110 researchers study every eligible stack, even scrap the player meant to recycle. The player's controls are forbidding stacks and the study-eras setting.

Tests: `dotnet test` 150 passed after the merge. Both builds 0 warnings, 0 errors; `rwtranslate check` passes.

In-game pending: studying same-era scrap advances the project and shows the message; finishing a project through study; higher-era scrap refused, with the inspect reason; no job with no project; research factor 0 (no job, inspect says why) and 2.5 (progress capped at the cost); techprint setting off with a techprint project; study time and Intellectual XP; a save and load during a study job.

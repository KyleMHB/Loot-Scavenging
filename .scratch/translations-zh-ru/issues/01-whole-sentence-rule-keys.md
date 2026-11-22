# 01: Whole-sentence keys for the item-rule sentence

**What to build:** The sentence under each item rule on the settings page comes from one Keyed key per combination of condition and chance, so a translation controls its whole word order, joining words and punctuation. Today `RuleSentence` fills `RuleAct`'s four slots with fragments (`CondWhen`, `CondReach`, `CondQuality`, `CondHitPoints`, `CondAnd`, `CondOr`, `RuleChance`) and joins them, and `RuleOtherwiseKept`, with literal spaces in C#. See the spec's "Prep: fragment-joined sentences" and Decision 3.

**Blocked by:** None (can start immediately)

**Status:** resolved

**Recommended model:** `opus`: designing a key set that covers every combination without changing one byte of English needs care.

## Scope

- The combinations are: beyond reach on or off × limits (none, quality, hit points, quality or hit points) × chance below 100% or not. That gives 16 sentences for the non-Keep outcomes. `RuleKeep` stays as it is.
- Each key is a whole sentence with slots for values only: the category label, the outcome verb phrase (`Verb...` keys, which stay as they are), the quality name, the hit-point percentage and the chance. A sentence with a condition or a chance carries the "otherwise they drop as normal" tail inside the same key.
- No literal space, bracket or joining word is added in C# around translated text in this sentence.
- The fragment keys that nothing uses any more are removed from the English Keyed file. The build's translation check fails on an unused key, so it confirms this.

## Acceptance

- [x] For every combination of reach, limits and chance, and for every outcome, the settings page shows exactly the English sentence it showed before this change.
- [x] Each of the 16 sentences comes from one key. The C# that builds the sentence only chooses a key and passes values.
- [x] `RuleSentence` and its callers contain no literal separators around translated text.
- [x] Both builds pass, and the 1.7 build's translation check passes, so no key is unused or missing.
- [x] `dotnet test` passes.

## Checks

- `dotnet build Source/LootScavenging.csproj -c Release` and the 1.6 build.
- `dotnet test`.
- The quick tier for settings: the smoke, settings and ui batches in one launch (TESTING.md "Test tiers"). The ui batch records the drawn text, which confirms the English is unchanged.
- To confirm byte-identical English, compare every combination's sentence before and after the change. This can be a one-off comparison through the settings batch's calls; do not add a permanent harness for it.

## Comments

### 2026-11-22 - implemented (agent)

**Changed:**

- `Source/SettingsUI/SettingsPage.cs`: `RuleSentence` picks one of 16 keys from a table indexed by reach (+8), limits (+2 per step: none, quality, hit points, quality or hit points) and chance below 100% (+1), and makes one `Translate` call. Every key takes the same slots: `{0}` category, `{1}` verb phrase, `{2}` quality, `{3}` hit points, `{4}` chance. Each key uses only the slots it needs. The C# adds no separators. `RuleKeep` and the `Verb...` keys are unchanged.
- `Languages/English/Keyed/LootScavenging.xml`: `RuleAct` is now the no-condition, full-chance sentence. Fifteen `RuleAct<Reach><Limits><Chance>` keys are added, each carrying the "Otherwise they drop as normal." tail. A comment lists the slots for translators. Removed: `RuleChance`, `RuleOtherwiseKept`, `CondWhen`, `CondReach`, `CondQuality`, `CondHitPoints`, `CondAnd`, `CondOr`.

**Checks:**

- The 1.7 and 1.6 Release builds pass. The translation check passed after the build and again by hand.
- `dotnet test`: 291 passed.
- Quick tier in one launch, seed 1: smoke `20261122-020741-lootscavenging-smoke-s1-7db8ece0` (9/9), settings `20261122-020751-lootscavenging-settings-s1-0208f419` (11/11), ui `20261122-020754-lootscavenging-ui-s1-02616b95` (6/6).
- Byte-identical English: a temporary case in that settings run called the new `RuleSentence` by reflection. It compared each result with the old method, rebuilt from the removed keys' English through the game's `Formatted`. The run covered 144 sentences: six verb variants (degrade with and without the quality loss, scrap, both disassembly yields, delete) × the 16 combinations, with chances of 100%, 37% and 0%. All 144 matched. The temporary case was then reverted and DevTests rebuilt, so `SettingsBatch.cs` is unchanged.

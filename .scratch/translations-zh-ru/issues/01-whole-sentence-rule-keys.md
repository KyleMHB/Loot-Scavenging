# 01: Whole-sentence keys for the item-rule sentence

**What to build:** The sentence under each item rule on the settings page comes from one Keyed key per combination of condition and chance, so a translation controls its whole word order, joining words and punctuation. Today `RuleSentence` fills `RuleAct`'s four slots with fragments (`CondWhen`, `CondReach`, `CondQuality`, `CondHitPoints`, `CondAnd`, `CondOr`, `RuleChance`) and joins them, and `RuleOtherwiseKept`, with literal spaces in C#. See the spec's "Prep: fragment-joined sentences" and Decision 3.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `opus`: designing a key set that covers every combination without changing one byte of English needs care.

## Scope

- The combinations are: beyond reach on or off × limits (none, quality, hit points, quality or hit points) × chance below 100% or not. That gives 16 sentences for the non-Keep outcomes. `RuleKeep` stays as it is.
- Each key is a whole sentence with slots for values only: the category label, the outcome verb phrase (`Verb...` keys, which stay as they are), the quality name, the hit-point percentage and the chance. A sentence with a condition or a chance carries the "otherwise they drop as normal" tail inside the same key.
- No literal space, bracket or joining word is added in C# around translated text in this sentence.
- The fragment keys that nothing uses any more are removed from the English Keyed file. The build's translation check fails on an unused key, so it confirms this.

## Acceptance

- [ ] For every combination of reach, limits and chance, and for every outcome, the settings page shows exactly the English sentence it showed before this change.
- [ ] Each of the 16 sentences comes from one key. The C# that builds the sentence only chooses a key and passes values.
- [ ] `RuleSentence` and its callers contain no literal separators around translated text.
- [ ] Both builds pass, and the 1.7 build's translation check passes, so no key is unused or missing.
- [ ] `dotnet test` passes.

## Checks

- `dotnet build Source/LootScavenging.csproj -c Release` and the 1.6 build.
- `dotnet test`.
- The quick tier for settings: the smoke, settings and ui batches in one launch (TESTING.md "Test tiers"). The ui batch records the drawn text, which confirms the English is unchanged.
- To confirm byte-identical English, compare every combination's sentence before and after the change. This can be a one-off comparison through the settings batch's calls; do not add a permanent harness for it.

# 04: Knowledge-project and no-recipe gear eras

Status: ready-for-agent

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** Gear unlocked only by Anomaly knowledge projects comes into reach once they are done, and gear with no research no longer says it waits for research. Spec, finding 4.

## Scope

- `Source/Era/EraReader.cs`: when every prerequisite is finished and none has a tech level, the item is in reach.
- The inspect text for gear with no recipe and no research (psychic lances) drops "until its research is finished". New or changed Keyed text in all three languages.

## Done when

- With the shard projects finished, shard gear is not unfamiliar.
- A psychic lance's inspect text is accurate.

# 05: Sort walk-in joiners when they spawn

Status: done

**Recommended model:** `opus`: a new saved flag on pawns must load cleanly from saves without it.

**Blocked by:** None (can start immediately)

**What to build:** A joiner who joins off the map under "Other joiners: Apply" has their gear run through the item rules when they first spawn. Spec, finding 5.

## Scope

- In `Source/Loot/JoinerHooks.cs`, when `GearSorter.Sort` skips a joiner for having no map, set a saved one-shot flag on them.
- On their first spawn, sort their gear once and clear the flag. A save without the flag behaves as 0.6.0.
- Update the header comment that lists the quests of wanderers.

## Done when

- A walk-in wanderer's gear follows the item rules when they arrive.
- Saving and loading before arrival keeps the flag.

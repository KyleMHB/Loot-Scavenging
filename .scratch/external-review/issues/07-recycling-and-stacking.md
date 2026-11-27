# 07: Recycling and stacking

Status: done

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** Recycling an unfamiliar item cannot be re-rolled, and the unfamiliar mark survives stack splits. Spec, finding 7, "Stacking" and "Duplicate patch".

## Scope

- `OriginalItemRecipes.cs` ~L120-145: give the item back only when `ScrapEquivalent.TryGet` fails.
- `CompUnfamiliar`: copy the mark in `PostSplitOff`; refuse `AllowStackWith` when marks differ.
- `UnfamiliarHooks.cs`: drop the 2-argument `CanEquip` postfix.

## Done when

- An empty recycle roll uses up the item.
- Splitting a marked grenade stack keeps the mark on both.

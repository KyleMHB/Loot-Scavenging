# 02: Drop from a dead colonist's Gear tab

Status: done

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** The drop button on a dead colonist's Gear tab drops the item beside the corpse. Spec, finding 2.

## Scope

- A prefix on `ITab_Pawn_Gear.InterfaceDrop`: when the item's holder is a dead pawn's inventory and the corpse is spawned, drop it at the corpse and skip the original.
- Leave every other case to the game.

## Done when

- Clicking drop on a dead colonist's kept weapon puts it on the ground with no "null map" error.

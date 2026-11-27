# 01: Keep corpse loot when the corpse is destroyed

Status: ready-for-agent

**Recommended model:** `opus`: the loot on a body is the player's; a wrong patch loses or duplicates it in saves.

**Blocked by:** None (can start immediately)

**What to build:** Gear kept on a body survives a predator, fire or rot: it drops beside the corpse when the corpse is destroyed. Spec, finding 1 and decision 5.

## Scope

- A prefix on `Corpse.Destroy` in `Source/Loot/`, written from `reviewer-fix/CorpseLootFixes.cs` in this project's style, with try/catch and `Log.Error` marked `// not translated`.
- Drop only when the corpse is spawned and the inner pawn's inventory holds anything. Forbid the drop unless the pawn was a colonist.
- Skip the drop while a cremation bill destroys the corpse, so kept gear burns (decision 5).
- Confirm strip and butcher reach `Destroy` with the inventory already empty, so nothing duplicates.

## Done when

- A raider's corpse with kept gear, eaten by a predator or burned, leaves that gear on the ground.
- Stripping and butchering give the same items as in 0.6.0, once.
- Cremation behaves as decision 5 says.

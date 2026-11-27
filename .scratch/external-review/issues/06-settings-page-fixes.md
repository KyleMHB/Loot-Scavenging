# 06: Settings page fixes

Status: done

**Recommended model:** `opus`: the lock fix can change a modpack's locked behaviour.

**Blocked by:** None (can start immediately)

**What to build:** The settings page shows what the settings do and locked settings stay locked. Spec, finding 6 and "Settings page".

## Scope

- Finding 6: `SettingsPage.cs` L497 uses `v.rules.AnyLootRowMarks`.
- Locked boxes: pass `disabled` to the checkbox in `Toggle`, and grey the category boxes at L266, L268 and L270 under a locked category.
- Market value: the box shows the stored value after rounding.
- Disassemble: one text buffer for the shared efficiency value.
- 0.5.2 locks: `SettingLocks.cs` L68-69 lock the keep-gear boxes to the value `deathDrop` and `rules.downedGear` map to. Check against a 0.5.2 settings file that locks `deathDrop` at Ground.

## Done when

- On the defaults, the scope sentence says it is unused.
- A locked box does not flip when clicked.
- A pack that locked `deathDrop` at Ground loads with both keep-gear boxes locked off.

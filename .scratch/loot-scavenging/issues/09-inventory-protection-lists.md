# 09: Inventory groups, protection and lists

**What to build:** Category rules for the inventory groups: ammunition (recognised by item category, with no dependency on any ammo mod), drugs, medicine, food and other. Each category's conditions (beyond reach, below a quality, below a hit-point percentage) and chance work. Quest-tagged items, unique weapons, persona weapons, relics and items on the keep list are never touched; items on the remove list are targeted. Lists are stored by item name, and there is no picker yet. A whole dropped stack is one item, and a partial drop acts on the piece that left the pawn. Mechanoid reward materials outside the inventory are left alone. See the spec's "Loot rules" section.

**Blocked by:** 01

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A raider carrying medicine and a quest weapon drops both untouched with the default rules.
- [ ] An item on the keep list is never touched, and one on the remove list is acted on.
- [ ] A partial stack drop is never partly deleted.
- [ ] Each category's chance is rolled on its own.
- [ ] A list entry naming an item that no longer exists is ignored without an error.
- [ ] Both the 1.7 and 1.6 builds compile.

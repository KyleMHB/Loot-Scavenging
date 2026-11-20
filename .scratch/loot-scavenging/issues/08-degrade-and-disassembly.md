# 08: Degrade and instant disassembly

**What to build:** Two more outcomes for category rules. Degrade leaves the item at a random hit-point level inside a set range (10% to 40% by default), with an option to also lower its quality one step. Disassemble replaces the item on the spot with materials. A setting picks the yield: era-typical materials from the tier table, or the item's own cost list and material at a set efficiency, computed by the mod rather than the vanilla smelt function. Materials are placed nearby in full stacks and forbidden like other enemy drops. Loaded charges in reloadable gear follow their own setting: eject, or go with the item. See the spec's outcome table and "Code structure".

**Blocked by:** 04

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A degraded item lands inside the set range, and the quality option lowers its quality one step.
- [ ] Changing the efficiency setting visibly changes the item-based yield.
- [ ] The era-typical yield matches the tier table.
- [ ] No refund stack is lost when materials exceed one stack.
- [ ] Loaded charges eject or stay with the item according to the setting.
- [ ] Both the 1.7 and 1.6 builds compile.

Raiders stop handing your tribal colony assault rifles. Their gear becomes scrap you can recycle or study.

When a raider, visitor or other non-colony pawn dies, is downed or is stripped, Loot Scavenging decides what their gear becomes. By default, anything beyond your colony's research turns into scrap of its era. You recycle that scrap at benches you already have, or study it at a research bench for points toward your current project. Gear your colony could make drops as normal.

If you play for cleanup instead, a preset deletes damaged and low-quality gear from the hostile dead and removes their corpses, with no era handling at all.

[b]Mod version:[/b] 0.3.0

[b]Languages:[/b] English, 简体中文 (Simplified Chinese), Русский (Russian).

[h2]Features[/h2]

[list]
[*][b]Scrap per era.[/b] Six stackable scrap items, from neolithic to archotech. The amount depends on the gear's value, quality and condition, so a stone knife gives one unit and a charge rifle gives several.
[*][b]Recycling at benches you have.[/b] Recycle bills at the crafting spot, smithy, machining table and fabrication bench turn scrap into materials of its era. Each bench handles its own tier and every tier below it.
[*][b]Study for research.[/b] Study bills at a research bench pay a fixed number of points per unit into the current project, from 10 for neolithic scrap to 100 for archotech. The scrap's inspect text says whether it can be studied now, and why not.
[*][b]Four drop modes.[/b] Scrap out-of-reach gear, scrap everything, keep the original item but mark it unfamiliar until you research it, or off.
[*][b]A rule per item category.[/b] Weapons, apparel, ammunition, drugs, medicine, food and other inventory each get an outcome (keep, degrade, scrap, disassemble or delete), a chance, and optional limits by research, quality and hit points.
[*][b]Loot on the body.[/b] At death, loot can drop on the ground as usual, leave only the scrap on the body, or leave everything on the body (weapon, medicine, food, silver and scrap) until you strip or butcher the corpse.
[*][b]Who it affects.[/b] Hostile pawns, non-hostile factions, pawns with no faction and your prisoners each have their own toggle. Your colonists are never affected.
[*][b]Protection.[/b] Quest items are never touched by default, and neither are unique weapons (Odyssey), persona weapons (Royalty) or relics (Ideology). An always-keep list and an always-remove list are picked from a searchable item menu.
[*][b]Corpse cleanup.[/b] Optional removal of humanlike, animal and mechanoid corpses, each with its own toggle and a shared chance. A separate option destroys worn apparel when a corpse is butchered.
[/list]

Scrap is worth almost nothing by default, so a stockpile you keep for later does not raise colony wealth or sell for silver. Medicine is exempt from the drop mode by default, so a raid does not destroy medicine a tribal colony needs.

[h2]How to use[/h2]

The mod starts on the Progression preset and shows a short message on first load.

[list]
[*]Change the preset or any rule under [b]Options > Mod settings > Loot Scavenging[/b].
[*]Add a [b]recycle[/b] bill at a crafting bench to turn scrap into materials.
[*]Add a [b]study[/b] bill at a research bench to turn scrap into research. Study is Research work, so your researchers do it.
[/list]

[h2]Presets[/h2]

[list]
[*][b]Progression[/b] (default): gear beyond your research becomes scrap when any non-colony pawn dies, is downed or is stripped, prisoners included. Gear within reach drops as normal, and corpses are left.
[*][b]Cleanup[/b]: no era handling. From the hostile dead, weapons below normal quality, apparel below good quality, and either one below 50% hit points are deleted. Every hostile humanlike corpse is removed.
[*][b]Vanilla[/b]: loot and corpses behave as in the base game.
[/list]

The settings page shows which preset is active and whether you have changed it. Each row says in a sentence what it will do.

[h2]Requirements[/h2]

[list]
[*]RimWorld 1.6 or 1.7.
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url].
[*]No DLC is required. Royalty, Ideology and Odyssey items are protected when those DLCs are active.
[/list]

[h2]Compatibility[/h2]

Loot Scavenging is not compatible with Looting Manager.

The loot rules run only when a pawn dies, is downed or is stripped, not every tick.

Modpacks can patch the scrap tiers, their benches and materials, and the default settings, which are all XML. A pack can also lock chosen settings. A modlist can ship its own settings through Auto Mod Config, which is optional. The mod's folder has an example preset.

You can add Loot Scavenging to an existing save. Stockpiles you already have will not take scrap until you allow it in their filters.

You can remove it from a save. The save then loads without scrap, studied scrap, recycle bills and study bills, and logs errors for them. Research benches stay as normal research benches, and gear kept as unfamiliar items becomes usable. Use up or destroy your scrap and delete those bills first.

[h2]Credits[/h2]

Inspired by Looting Manager and Non uno Pinata. Loot Scavenging is a separate mod, written from scratch.

[h2]License[/h2]

Released under the GNU General Public License v3.0. Commercial use is allowed. Distributed modified versions must remain under GPLv3 and make corresponding source available as required by the license.

[h2]AI?[/h2]

Do I use AI to build my mods? [b]Yes.[/b]
Am I obsessive about how I use it and the quality of the results? [b]Also yes.[/b]

The Chinese and Russian text is machine-translated. If a line reads wrong, tell me and I will fix it.

I understand the scepticism.
I've seen the flood of AI-generated mods that look like someone typed a prompt, uploaded whatever came out, and called it done.

That bothers me too.

My workflow involves a lot of iteration, review, and testing. The same care I'd put into building everything by hand. I've spent hundreds of hours building mods. Using AI doesn't lower the standard I expect from my work.

[h2]Links[/h2]

Support me on Ko-fi.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]

When you report a bug, please include your [b]Player.log[/b] file and your mod list.

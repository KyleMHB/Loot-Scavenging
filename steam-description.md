Vanilla raids hand a tribal colony assault rifles long before it could research or craft one. Loot Scavenging turns gear beyond your research into scrap of its era, so better gear has to come from your own research and your own benches. You recycle the scrap for materials or study it for research points, and raids still pay you back without letting you skip ahead.

[img]https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/loot-scavenging-raid.gif[/img]

The rules run when a raider, visitor or other non-colony pawn dies, is stripped, is arrested or joins your colony. Gear your colony could already make drops as normal.

Every part of it is configurable: which groups of pawns the rules cover, an outcome and chance for each item category, keep and remove lists, and presets for progression, cleanup or vanilla. Modpacks can set and lock their own defaults.

For cleanup only, a preset deletes weak gear and all ammo from the hostile dead and removes their corpses.

[b]Mod version:[/b] 0.7.0

[b]Languages:[/b] English, 简体中文 (Simplified Chinese), Русский (Russian). The Chinese and Russian text is machine-translated; tell me if a line reads wrong.

[h2]Features[/h2]

[list]
[*][b]Scrap per era.[/b] Six stackable scrap items, from neolithic to archotech. The amount depends on the gear's value, quality and condition, so a stone knife gives one unit and a charge rifle gives several.
[*][b]Recycle or study.[/b] At benches you have, scrap becomes materials of its era or research points.
[*][b]A Who list.[/b] Hostile pawns, non-hostile pawns, wild people and animals, prisoners, recruited prisoners and other joiners each apply the item rules, are left alone, or keep their gear whole with gear beyond your research marked unfamiliar until you research it.
[*][b]A rule per item category.[/b] Weapons, apparel, ammunition, drugs, medicine, food and other inventory each get an outcome (scrap, disassemble on the spot, delete or drop as normal), a chance, and optional limits by research, quality and hit points. By default only weapons and apparel are scrapped, and only when they are beyond your research.
[*][b]Protection.[/b] Quest items are never touched by default, and neither are unique weapons (Odyssey), persona weapons (Royalty) or relics (Ideology). An always-keep list and an always-remove list are picked from a searchable item menu.
[*][b]Corpse cleanup.[/b] Optional removal of humanlike, animal and mechanoid corpses, each with its own box. Butchering a humanlike corpse strips it first, and what comes off follows the item rules.
[/list]

Scrap is worth almost nothing by default, so a stockpile does not raise colony wealth. Medicine, drugs, food and ammunition are left alone by default, so a raid does not destroy medicine a tribal colony needs.

[h2]Recycling at benches you have[/h2]

[img]https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/loot-scavenging-recycle.gif[/img]

Recycle bills at the crafting spot, smithy, machining table and fabrication bench turn scrap into materials of its era. Each bench recycles its own era and every era below it. A x10 bill handles ten units in one job.

[h2]Study for research[/h2]

[img]https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/loot-scavenging-study.gif[/img]

Study bills at a research bench pay a set number of points per unit into the current project, by default from 5 for neolithic scrap to 25 for archotech. The settings let you type each era's points. The scrap's inspect text says whether it can be studied now, and why not.

[h2]Loot on the body[/h2]

[img]https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/loot-scavenging-on-body.gif[/img]

By default a dead pawn keeps everything on the body (weapon, medicine, food, silver and scrap) until you strip or butcher the corpse. Gear on a body that burns or is eaten is lost with it. Tick [b]Strip corpses before cremating[/b] and cremation drops it first. Your colonists and animals keep their gear too. Untick the hostile or non-hostile box under [b]Keep gear on the body[/b] and that group's loot drops as in the base game, then follows the item rules.

[h2]Downed raiders keep their weapon[/h2]

[img]https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/loot-scavenging-downed.gif[/img]

A raider who goes down keeps their weapon in hand, and nothing drops. Their gear goes through the rules only once, when they are stripped, arrested, join your colony or die. If they get back up, they are still armed. Untick the Hostiles box and the weapon drops and follows the item rules at once.

[h2]How to use[/h2]

The mod starts on the Progression preset.

[list]
[*]Change the preset or any rule under [b]Options > Mod settings > Loot Scavenging[/b].
[*]Add a [b]recycle[/b] bill at a crafting bench to turn scrap into materials.
[*]Add a [b]study[/b] bill at a research bench to turn scrap into research. Study is Research work, so your researchers do it.
[/list]

[h2]Presets[/h2]

[list]
[*][b]Progression[/b] (default): weapons and apparel beyond your research become scrap when a hostile, non-hostile or wild pawn or a prisoner loses them. Other joiners keep their gear, with items beyond your research marked unfamiliar. Everything else drops as normal, and corpses are left.
[*][b]Cleanup[/b]: hostile pawns only, ignores research. From the hostile dead, weapons below normal quality, apparel below good quality, and either one below 50% hit points are deleted, along with all ammunition. Every hostile humanlike corpse is removed.
[*][b]Vanilla[/b]: every group is left alone and nothing stays on the body, so loot and corpses behave as in the base game.
[/list]

The settings page shows the active preset and whether you changed it.

[h2]Requirements[/h2]

[list]
[*]RimWorld 1.6 or 1.7.
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url].
[*]No DLC is required. Royalty, Ideology and Odyssey items are protected when those DLCs are active.
[/list]

[h2]Compatibility[/h2]

Loot Scavenging is not compatible with Looting Manager. Non uno Pinata works with it but overrides its [b]Keep gear on the body[/b] boxes.

Modpacks can patch the scrap eras, their benches and materials, and the default settings, which are all XML. A pack can also lock chosen settings. A modlist can ship its own settings through Auto Mod Config, which is optional. The mod's folder has an example preset.

You can add Loot Scavenging to an existing save. Stockpiles you already have will not take scrap until you allow it in their filters.

You can remove it from a save. The save then loads without scrap, studied scrap, recycle bills and study bills, and logs errors for them and for one value the mod keeps in the save. Research benches stay as normal research benches, and gear kept as unfamiliar items becomes usable. Use up or destroy your scrap and delete those bills first.

[h2]Credits[/h2]

Inspired by Looting Manager and Non uno Pinata. Loot Scavenging is a separate mod, written from scratch.

Scrap artwork by Morton.

[h2]License[/h2]

Released under the GNU General Public License v3.0 (GPLv3). Commercial use is allowed. Modified versions you distribute must stay under GPLv3 with the source available.

[h2]Links[/h2]
Support me on Ko-fi, or ask about my mods on Discord.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]
[url=https://discord.gg/jSdyTbSYWQ][img]https://img.shields.io/badge/Discord-Join_the_server-5865F2?style=for-the-badge&logo=discord&logoColor=white[/img][/url]
[url=https://github.com/KyleMHB/Loot-Scavenging][img]https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white[/img][/url]

When you report a bug, please include your [b]Player.log[/b] file and your mod list.

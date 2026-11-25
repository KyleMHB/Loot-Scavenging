# Loot Scavenging

A RimWorld mod that decides what non-colony pawns' gear becomes when they die, are stripped, are arrested or join your colony. By default, gear beyond your colony's research turns into scrap of its era, which you recycle for materials or study for research at benches you already have.

## Features

- **Scrap per era.** Six stackable scrap items, neolithic to archotech. The amount comes from the gear's value, quality and condition, so a stone knife gives one unit and a charge rifle several. Scrap is worth almost nothing by default, so a stockpile does not raise colony wealth.
- **Recycling at existing benches.** A recycle bill at the crafting spot, smithy, machining table or fabrication bench turns scrap into materials of its era. Each bench recycles its own era and every era below it. A x10 bill handles ten units in one job.
- **Study at research benches.** A study bill uses up scrap for a set number of research points per unit, by default from 5 for neolithic to 25 for archotech, paid into the current project if it is of the scrap's era or lower. The scrap's inspect text says when it can be studied and why not.
- **A Who list.** Hostile pawns, non-hostile pawns, wild people and animals, prisoners, recruited prisoners and other joiners each get a row: apply the item rules, leave the group alone, or keep its gear whole and mark gear beyond your research unfamiliar until researched. Colony gear never goes through the item rules.
- **Rules per item category.** Weapons, apparel, ammunition, drugs, medicine, food and other inventory each get an outcome (scrap, disassemble on the spot, delete or drop as normal), a chance, and optional limits by research, quality and hit points. By default only weapons and apparel are scrapped, and only when they are beyond your research.
- **Keep gear on the body.** Three boxes, for colonists, hostiles, and non-hostiles and wild people, all on by default. A downed pawn keeps their weapon in hand, and a dead one keeps everything on the body (weapon, medicine, food, silver and scrap) until the corpse is stripped, butchered or cremated. That gear goes through the rules once, when it changes hands. Untick a box and that group drops loot as in the base game, then the item rules apply to it.
- **Protection and lists.** Quest items, unique weapons, persona weapons and relics are protected by default. Always-keep and always-remove lists are picked from a searchable item menu.
- **Corpses.** Optional removal of humanlike, animal and mechanoid corpses, each with its own box. Butchering a humanlike corpse strips it first, and what comes off follows the item rules.
- **Presets.** Progression (the default), Cleanup and Vanilla. The settings page shows the active preset and whether you have changed it.
- **Patchable tables.** The scrap eras, their benches, value per unit and material pools, and the default settings are XML that a modpack can patch, with an option to lock chosen settings.

## Usage

The mod starts on the Progression preset. Change presets and rules under Options > Mod settings > Loot Scavenging.

Add recycle and study bills to benches as you would any other bill. Study is Research work.

### Auto Mod Config

A modlist curator can ship Loot Scavenging settings through [Auto Mod Config (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3586136174). This is optional, and the mod works without it. An example preset is in `Extras/Loot-Scavenging-AutoConfig`. Copy that folder into `RimWorld/Mods` as a separate mod and edit its settings files. There are two with the same content: `Mod_Loot Scavenging_LootScavengingMod.xml` for a local install and `Mod_3844457170_LootScavengingMod.xml`, which is expected to cover a Workshop subscription but is not confirmed yet. The comment in each file lists every key and its values. Importing a preset replaces the player's saved Loot Scavenging settings, and a setting a modpack locks keeps the pack's value.

## Requirements

- RimWorld 1.6 or 1.7
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)

## Building from source

The project file expects RimWorld 1.7 and Harmony at the paths set in `Source/LootScavenging.csproj`. Override `RimWorldPath` and `HarmonyPath` if yours differ.

Build for 1.7:

```
dotnet build Source/LootScavenging.csproj -c Release
```

Build for 1.6:

```
dotnet build Source/LootScavenging.csproj -c Release -p:RimWorldVersion=1.6
```

The build writes the assemblies to `1.7/Assemblies` and `1.6/Assemblies`.

## Credits

Scrap artwork by Morton.

## License

Released under the **GNU General Public License v3.0**. See [`LICENSE`](LICENSE) for the complete terms.

Commercial use is allowed. Distributed modified versions must remain under GPLv3 and make corresponding source available as required by the license.

## Links

Support me on Ko-fi, or ask about my mods on Discord.

[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/I7L525WMJ6)
[![Discord](https://img.shields.io/badge/Discord-Join_the_server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/jSdyTbSYWQ)

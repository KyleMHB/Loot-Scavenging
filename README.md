# Loot Scavenging

A RimWorld mod that decides what enemy gear becomes when it drops. Gear beyond your colony's research turns into scrap, which you recycle for materials or study for research.

The mod is in development. This repository holds the project setup and the design, and no gameplay yet.

## Planned features

- Rules per item category: keep, degrade, scrap, disassemble or delete, each with its own chance.
- Scrap in one stackable item per tech era, sized by the value of the gear it came from.
- Recycling as a bill at benches you already have. Later-era scrap needs a later-era bench.
- Study as research work: a researcher uses up scrap at a research bench to advance the current project.
- Presets for cleanup and for tech progression play.
- Corpse cleanup with separate settings for people, animals and mechanoids.

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

## License

Released under the **GNU General Public License v3.0**. See [`LICENSE`](LICENSE) for the complete terms.

Commercial use is allowed. Distributed modified versions must remain under GPLv3 and make corresponding source available as required by the license.

## AI?

Do I use AI to build my mods? **Yes.**  
Am I obsessive about how I use it and the quality of the results? **Also yes.**

I understand the scepticism.
I've seen the flood of AI-generated mods that look like someone typed a prompt, uploaded whatever came out, and called it done.

That bothers me too.

My workflow involves a lot of iteration, review, and testing. The same care I'd put into building everything by hand. I've spent hundreds of hours building mods. Using AI doesn't lower the standard I expect from my work.

## Links

Support me on Ko-fi.

[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/I7L525WMJ6)

# Pack survey: inventory item categories for per-category drop mode (2026-11-24)

Scope: all 1,559 downloaded Workshop mods under `E:\Steam\steamapps\workshop\content\294100` (collection membership not worked out, per the user: the pack is about 1,500 mods) plus the game's Core, Royalty, Ideology, Biotech, Anomaly and Odyssey defs. Looting Manager and Non uno Pinata were skipped by folder. No Technology Blueprints & Reverse Engineering folder was found among the downloads (matched on About.xml name only). Method: parsed every `ThingCategoryDef` and `ThingDef` XML in the root, `Common`, `1.6` and `1.7` `Defs` folders, resolved `ParentName` inheritance for `thingCategories`, de-duplicated by (mod, defName). About 15,800 unique item-like defs and 530 categories. Counts are unique defs / mods. The scan scripts are in the session scratchpad, not the repo.

## 1. Recommended new inventory categories

The ticket's raw-materials rule (stuff, `ResourcesRaw`, or `Manufactured` minus `MortarShells`, checked after gear, ammunition, medicine, drugs and food) is where most trouble lands: `Manufactured` is a catch-all holding many things that are not materials. The first three items are carve-outs the rule needs.

### Priority A (with ticket 17)

1. **Mortar shells and non-"Ammo"-named ammunition go to Ammunition, not Other or Raw materials.**
   - Detect: category tree contains `MortarShells` (21 defs, 8 mods: Core `Shell_*`, Biotech `Shell_Toxic`, Anomaly `Shell_Deadlife`, VFE Deserters `VFED_Shell_*`, Alpha Biomes `AB_Shell_Tar`, Ushankas Biological Warfare `USH_*Shell`, Ushankas Glittertech `USH_Shell_ADP`). Also def-name stragglers filed directly under `Manufactured`: Medieval Overhaul `DankPyon_ScorpioBolt`, `DankPyon_BallistaBolt`; Reel's Turret Pipeline `ReelTurretAmmo`. Optional extra signal: `thingClass` `CombatExtended.AmmoDef` (about 900 defs, all in "Ammo*" categories already).
   - Why: the ticket leaves shells in Other, so the Other switch would scrap them; a shell is ammunition. Traders stock `Shell_Deadlife` (6 trader kinds).
   - Default: Ammunition rule, drop-mode switch off.

2. **Serums and catalysts that are not `IsDrug`: classify as Drugs.**
   - Detect: category tree contains `Drugs` but `def.IsDrug` is false (no `CompProperties_Drug`). 13 defs: Core `MechSerumHealer`, `MechSerumResurrector`; Biotech `DeathrestCapacitySerum`; Anomaly `VoidsightSerum`, `MetalbloodSerum`, `JuggernautSerum`, `MindNumbSerum`, `GhoulResurrectionSerum`; Odyssey `SentienceCatalyst`; mods: VPE Anima `AnimaMoteSTNL`, `AnimaWispSTNL`, Mythic Ages `MA_AmineSerum`, Stoneborn `SD_RawShatterbrew`.
   - Why: `Drugs` is a child of `Manufactured`, so under the ticket's rule these become Raw materials, and the Raw materials switch would scrap resurrector serums. `DeathrestCapacitySerum` is in 6 trader kinds.
   - Default: Drugs rule, switch off.

3. **Special-use items filed under `Manufactured` should leave Raw materials.**
   - Detect: has `CompProperties_Usable`, any `CompProperties_UseEffect*`, `CompProperties_Studiable`, or a `thingClass` other than `ThingWithComps`/`Thing`. Examples: Alpha Random loot boxes `ARandom_*LootBox` (17, class `AlphaRandom.LootBox`) and `ARandom_FateDevices` (4); Alpha Armoury `AArmoury_WeaponKit*` (4); Alpha Animals `AA_HugeAnimaPearl`; Anomaly `Shard`; Ancient urban ruins `AM_KeyCard`, `AM_CreditCard`, `AM_WayBill`, `AM_IntelMap` (quest use effect); Vanilla Genetics Expanded and Alpha Genes tools (see 5).
   - Default: Other (or the category below), switch off.

### Priority B (a category and switch is worth it)

4. **Knowledge items: books, schematics, techprints, neurotrainers, research papers.**
   - Detect: `thingClass` ends in `Book` or comp `CompProperties_Book`, or category tree `Books`, `Techprints`, `Neurotrainers` (children `NeurotrainersSkill`, `NeurotrainersPsycast`). Books: 52 defs, 8 mods (Core `TextBook`, `Schematic`, `Novel`; Anomaly `Tome`; Odyssey `Map`; Research Papers `ResearchPaper`; Alpha Books `ABooks_*`; Medieval Overhaul `DankPyon_Schematic_*`, `DankPyon_Treatise_*`; Roo's `RBME_XenotypeIndex*`; Vanilla Cooking Variety `VCEV_CookingBook`).
   - Techprints (Royalty) and neurotrainers are generated at load (no XML defs, hundreds at runtime), filed under `Techprints` and `Neurotrainers` (children of `Items`). They fall to Other under the ticket's rules.
   - Why: the changelog says techprints stop being scrapped by default; Research Papers is a pack mod whose papers traders and raiders carry. Books and trainers have real value.
   - Default: Keep, switch off. One "Knowledge" category covers all.

5. **Genetic and biotech material.**
   - Detect: `thingClass` `Genepack` or `Xenogerm`; `HumanEmbryo`, `HumanOvum`; categories `GR_GeneticMaterial` tree, `GR_Boosters`, `GR_Genoframes`, `GR_Incubators`, `GR_GenomeExcavators`, `GR_GenetrainersCategory` (Vanilla Genetics Expanded), `AG_GeneTools` and defs `AG_Alphapack`, `AG_Mixedpack` (Alpha Genes). 67 defs, 4 mods.
   - Why: high-value trade goods (7 trader kinds stock `Genepack`; Roo's Faun xenotype pawns carry `Genepack`/`Xenogerm` in `fixedInventory`). Vanilla Genetics Expanded items sit under `Manufactured`, so they would be Raw materials.
   - Default: Keep, switch off. Can share a category with Knowledge as "Special items".

6. **Body parts, implants and prosthetics carried as items.**
   - Detect: category tree contains `BodyParts`: 529 defs, 32 mods (Core `BodyPartsProsthetic`, `Bionic`, `Archotech`, `Ultra`, `Natural`; mod trees `AI_ImplantCategory`, `GR_ImplantCategory`, `BM_BionicModules_*`, `VREA_BodyPartsAndroid`, `USH_BodyPartsHacker`, `AA_ImplantCategory`, `DV_Golem_Parts`).
   - Why: large, mod-heavy and very valuable; traders carry them (Expanded Prosthetics and Organ Engineering). They fall to Other.
   - Default: Keep, switch off.

7. **One-use items and mod upgrade modules** (Usable comp outside gear: 199 defs, 32 mods; `UseEffectInstallImplant` 29 defs, 7 mods; Mechanoid Upgrades `MU_Upgrades` 118 defs, 3 mods). Fine under Other with the switch off; no bespoke category needed. Relics and artifacts (`InertRelics`, `Artifacts`, `Romy_WrithingArtifacts`: 92 defs, 11 mods) are the same, provided the existing relic protection covers inert relics (not verified).

Not worth a category: eggs (302 defs, 25 mods) and raw foods (524 defs, 45 mods) are ingestible, so Food. Unfinished things (94 defs) are never carried. Stone chunks (`StoneChunks` under `Chunks`, not `ResourcesRaw`; Core `ChunkSlagSteel`, `ChunkMechanoidSlag`) land in Other; add `Chunks` to the raw-materials match if you want them there.

## 2. Misclassification risks

1. **Ammo by category name works for the sources found.** Categories with "Ammo" in the name or an ancestor name: Combat Extended (about 900 `CombatExtended.AmmoDef` defs across 244 categories), Yayo's Combat 3 (`yy_ammo_category`, 18 defs, no parent), Progression: Ammunition (`PA_AmmoPacks`: `PA_ArrowRefill`, `PA_AmmoRefill`, `PA_ChargeRefill`) and Medieval Overhaul `DankPyon_TrebuchetAmmo`. No false positive found: every category or ancestor containing "ammo" holds ammo.
   - CE ammo categories are children of `Weapons` (`AmmoRifle > AmmoRifles > Ammo > Weapons`). Gear detection must stay `EraReader.IsGear` (`IsWeapon || IsApparel`), not category ancestry, or all CE ammo would count as weapons. Keep the ammo check ahead of any category-based weapon test.

2. **Ammo without "Ammo" in its category** (see 1.1): `DankPyon_ScorpioBolt`, `DankPyon_BallistaBolt`, `ReelTurretAmmo` become Raw materials; `MortarShells` children become Other.

3. **`Manufactured` is wider than the spec's list.** Fine as Raw materials: direct `Manufactured` defs (about 105: components, chemfuel, neutroamine, Alpha Animals cores), `Textiles` with `Leathers` (97) and `Wools` (39), `Med_Pipes` and `Med_Cables` (Cables and Plumbing), `VVE_CarParts` (10), `AM_Electronics`, `AM_Tool`, `AM_SpecialComp`, `VBE_Musts` (brewing intermediates), Biotech subcores and chips, Odyssey `Gravcore`, `ShuttleEngine`. Wrongly caught: genetic items, loot boxes, fate devices, weapon kits, quest cards, serums and Anomaly `Shard` (see section 1, items 2, 3, 5). Alternative fix: restrict the `Manufactured` match to `Textiles` and defs directly in `Manufactured`, plus an exclusion list.

4. **Serums are not `IsDrug`** (see 1.2).

5. **Medicine edge cases.** `thingClass Medicine` items: Core `MedicineHerbal/Industrial/Ultratech`, Alpha Animals `AA_CactipineQuill`, Medieval Overhaul herbs (`DankPyon_Mindwort`, `Poppy`, `Fleawort`, `FlyAgaric`, `HerbalExtract`), Ancient urban ruins `AM_FirstAidKit`, `AM_Salewa` and similar. All are in `Medicine`, so caught. Two Ancient urban ruins items (`AM_AluminumSplint`, `AM_PelvicHammock`) sit in `AM_MedicalSupplies` without class `Medicine` (not confirmed whether they have potency) and could fall to Raw materials. Roo's `MedicineGuldenHerbal` is referenced in an inventory list but was not found in the scanned defs.

6. **Food and drug overlap.** Alcohol, tea, coffee and joints (Vanilla Brewing Expanded, Medieval Overhaul `Cider`/`Beer`) have a drug comp and nutrition; ordering medicine, drugs, food puts them in Drugs. Plant foods under `ResourcesRaw > PlantMatter` (flour, tea leaves) are ingestible, so Food wins before Raw materials. Fertilized eggs (hatchers) become Food.

7. **Stuff.** `stuffProps` appears on 59 defs in 25 mods; leathers and wools are stuff, so Raw materials as the spec intends.

## 3. What I could not check

- Generated defs: techprints, neurotrainers, psytrainers, per-animal leather, meat and corpses are not in XML. Their categories come from Core's `ThingCategoryDef` list; counts and `thingCategories` were not verified in game. A one-time in-game dump of `DefDatabase<ThingDef>` by the new classifier would settle it.
- Patches: `Patches/` was ignored, so categories added or removed by patches (including pack compatibility patches) are missing.
- Collection membership and load order: surveyed every downloaded mod. About 23 mods in the active list are not in the Workshop folder (local or removed). `ParentName` inheritance was only resolved among scanned mods. `LoadFolders.xml` conditions were not evaluated; only root, `Common`, `1.6`, `1.7` `Defs` folders were read.
- Runtime properties: `IsDrug`, `IsMedicine`, `IsNutritionGivingIngestible`, `IsStuff` were approximated from XML (`CompProperties_Drug`, `thingClass Medicine`, `ingestible` presence, `stuffProps`). `drugCategory` and `MedicalPotency` were not read.
- What pawns actually carry: only static sources (`PawnKindDef` `fixedInventory`/`inventoryOptions`: 68 defs, mostly Roo's xenotype kinds plus Core medicine and chemfuel; `TraderKindDef` stock generators). Quest rewards, `ThingSetMaker`s and runtime hauling were not traced.

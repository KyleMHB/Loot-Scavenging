# Loot Scavenging translation glossary

Use these terms in every translation. Add a row before using a new recurring term.

Rows under "From vanilla" copy the game's own ChineseSimplified and Russian text exactly. They were read on 2026-11-22 from the RimWorld 1.7 language archives in `Data/<Core|Royalty|Ideology|Biotech|Anomaly|Odyssey>/Languages`. The Source column names the Keyed key or the DefInjected path, so a later game update can be checked. Rows under "Mod terms" are this mod's own choices.

## From vanilla: eras

The game shows these words itself through `TechLevel.ToStringHuman()`, so scrap names and every era slot must use the same words.

| English | ChineseSimplified | Russian | Source |
| --- | --- | --- | --- |
| neolithic | 石器时代 | неолитический | Keyed `TechLevel_Neolithic` |
| medieval | 中世纪 | средневековый | Keyed `TechLevel_Medieval` |
| industrial | 工业时代 | индустриальный | Keyed `TechLevel_Industrial` |
| spacer | 太空时代 | космический | Keyed `TechLevel_Spacer` |
| ultratech (vanilla English says "ultra") | 极致时代 | ультратехнологичный | Keyed `TechLevel_Ultra` |
| archotech | 超凡时代 | архотехнологичный | Keyed `TechLevel_Archotech` |
| tech level | 技术级别 | технический уровень | Keyed `TechLevelTooLow`, `ResearchCostComparison` |

## From vanilla: benches and bills

| English | ChineseSimplified | Russian | Source |
| --- | --- | --- | --- |
| crafting spot | 手工加工点 | рабочее место | ThingDef `CraftingSpot.label` |
| smithy | 锻造台 (锻造台（燃料） / 锻造台（电力）) | кузня (топливная кузня / электрокузня) | ThingDef `FueledSmithy.label`, `ElectricSmithy.label` |
| machining table | 机械加工台 | верстак | ThingDef `TableMachining.label` |
| fabrication bench | 精密装配台 | высокоточный станок | ThingDef `FabricationBench.label` |
| research bench | 研究台 (基础研究台 / 高级研究台) | стол для исследований (простой / высокотехнологичный стол для исследований) | ThingDef `SimpleResearchBench.label`, `HiTechResearchBench.label` |
| butcher table | 屠宰台 | разделочный стол | ThingDef `TableButcher.label` |
| bill / Bills tab | 清单 / 「清单」选项卡 | задание / вкладка «Задания» | Keyed `TabBills`, `AddBill`; ConceptDef `BillsTab.label` |
| Do until you have X | 维持 X 个 | повторять до X шт | BillRepeatModeDef `TargetCount.label` (English def: "do until you have X") |
| Do X times / Forever | 做 X 次 / 不限次数 | cделать X шт / делать бесконечно | BillRepeatModeDef `RepeatCount.label`, `Forever.label` |

Quote the "Do until you have X" option exactly as above, spaces included. The Russian vanilla text starts lower-case; capitalise only its first letter (Повторять до X шт).

## From vanilla: items and quality

| English | ChineseSimplified | Russian | Source |
| --- | --- | --- | --- |
| quality | 品质 | качество | Keyed `Quality` |
| awful / poor / normal / good | 极差 / 较差 / 一般 / 良好 | ужасно / плохо / нормально / хорошо | Keyed `QualityCategory_Awful` … `_Good` |
| excellent / masterwork / legendary | 极佳 / 大师级 / 传奇级 | отлично / шедевр / легенда | Keyed `QualityCategory_Excellent` … `_Legendary` |
| hit points | 耐久度 | прочность | Keyed `HitPointsBasic`; StatDef `MaxHitPoints.label` (not `HitPoints`, which says Целостность) |
| item / items | 物品 | предмет / предметы | ThingCategoryDef `Items.label` |
| weapons | 武器 | оружие | ThingCategoryDef `Weapons.label` |
| apparel | 衣物 | одежда | ThingCategoryDef `Apparel.label` |
| drugs | 药物与成瘾品 | наркотики | ThingCategoryDef `Drugs.label` |
| medicine | 医疗药品 | медикаменты | ThingCategoryDef `Medicine.label` |
| food | 食物 | продовольствие | ThingCategoryDef `Foods.label` |
| gear (equipment) | 装备 | снаряжение (mod choice: vanilla `Equipment` says Оружие, which means weapons) | Keyed `Equipment` (Chinese only) |
| loot | 战利品 | добыча | Keyed `Difficulty_RaidLootPointsFactor_Info` |
| raw materials / materials | 原材料 | материалы | ThingCategoryDef `ResourcesRaw.label` |
| ingredients | 原料 | ингредиенты | Keyed `Ingredients` |
| components | 零部件 | компоненты | ThingDef `ComponentIndustrial.label` |
| silver | 白银 | серебро | ThingDef `Silver.label` |
| plasteel | 玻璃钢 | пласталь | ThingDef `Plasteel.label` |
| market value | 市场价值 | рыночная стоимость | StatDef `MarketValue.label` |
| colony wealth | 殖民地财富 | богатство поселения | Keyed `ThisMapColonyWealthTotal` |
| charges (loaded ammo) | 充能 | заряды | Keyed `ChargesRemaining` |
| ammunition | 弹药 | боеприпасы | Keyed `CommandExtractShellDesc` (Chinese); ThingDef `Uranium.description` (Russian). Vanilla has no category of this name |
| quest | 任务 | задание | Keyed `Quest` |
| unique weapon | 特化武器 | уникальное оружие | Keyed `UniqueWeapon` (Odyssey) |
| persona weapon | 人格武器 | оружие с личностью | ThingCategoryDef `WeaponsMeleeBladelink.label` (Royalty) |
| relic | 圣物 | реликвия | Keyed `Relic` (Ideology) |
| disassemble | 拆解 | разобрать | Keyed `DisassembleMech` (Biotech) |
| delete | 删除 | удалить | Keyed `Delete` |
| default | 默认 | по умолчанию | Keyed `Default` |

## From vanilla: research

| English | ChineseSimplified | Russian | Source |
| --- | --- | --- | --- |
| research | 研究 | исследование | Keyed `Research`; WorkTypeDef `Research.gerundLabel` |
| research points | 研究点数 | очки исследования | RecordDef `ResearchPointsResearched.label` |
| research project / project | 研究项目 | технология (исследование when no project is meant) | Keyed `NeedResearchProject`, `LetterTechprintAppliedPartIntro`, `ResearchUnknownProjectRevealed` |
| techprint | 科研蓝图 | чертёж | Keyed `TechprintLabel`, `LetterTechprintAppliedPartIntro` (Royalty) |
| prerequisites | 前置研究项目 | требования | Keyed `Prerequisites` |
| research speed | 研究速度 | скорость исследования | StatDef `ResearchSpeed.label` |
| study (verb) | 调查 | изучить / изучение | Keyed `Study`; WorkTypeDef `Study.label` (Anomaly) |

Vanilla uses "study" for Anomaly's entity study. This mod's study is the same act, so it uses the same word. Never render it 研究 in Chinese, which is "research".

## From vanilla: people and loot events

| English | ChineseSimplified | Russian | Source |
| --- | --- | --- | --- |
| colonist | 殖民者 | поселенец | Keyed `Colonist` |
| colony | 殖民地 | поселение | Keyed `Colony` |
| prisoner | 囚犯 | пленник | Keyed `Prisoner`, `PrisonerLower` |
| raider | 袭击者 | налётчик | Keyed `HibernateWarningStandalone`, `EscapeShipFound` |
| faction | 派系 | фракция | Keyed `Faction` |
| hostile | 敌对 | враждебный (враги as a noun) | Keyed `Hostile`, `HostileVisitorsPresent` |
| non-hostile | 非敌对 | невраждебный | mod form of the row above |
| pawn | 角色 | персонаж | Keyed `GravshipShieldOrderActivationDesc` (Chinese); vanilla Russian's usual word (35 Keyed uses against 4 for пешка) |
| visitors | 访客 | гости | Keyed `VisitorsLeaving` |
| wild animals | 野生动物 | дикие животные | Keyed `PenFoodTab_NaturalNutritionGrowthRateSeasonal` |
| wild man / wild people | 野人 | одичалый / одичалые | Keyed `StatsReport_WildMan` |
| animal | 动物 | животное | ThingCategoryDef `CorpsesAnimal.label` |
| mechanoid | 机械族 | механоид | Keyed `Mechanoid` |
| corpse | 尸体 | труп (туша for an animal, останки for a mechanoid) | Keyed `CorpseLabel`; ThingCategoryDef `Corpses.label`, `CorpsesAnimal.label`, `CorpsesMechanoid.label` |
| humanlike corpses | 人类尸体 | трупы людей | ThingCategoryDef `CorpsesHumanlike.label` |
| tainted apparel | 死者衣物 | одежда с трупов | SpecialThingFilterDef `AllowDeadmansApparel.label` |
| downed | 倒地 | обездвижен | Keyed `DownedLower` |
| strip / stripped | 剥光 / 被剥光 | раздеть / раздет | Keyed `Strip`, `GoodwillChangedReason_PawnStripped` |
| arrest / arrested | 拘捕 / 被拘捕 | арестовать / арестован | Keyed `Arrest`, `TryToArrest` |
| pack (a pawn's inventory) | 背包 | инвентарь | This mod's earlier Keyed texts (`DeathDropAllOnBodyExplain`) |
| butcher / butchered | 屠宰 | разделать / разделка | RecipeDef `ButcherCorpseFlesh.label`; Keyed `Difficulty_ButcherYieldFactor_Label` |
| storyteller | 故事叙述者 | рассказчик | Keyed `Storyteller` (Chinese 当前故事叙述者 = "current storyteller") |
| Options > Mod Settings | 选项 > Mod 设置 | Настройки > Настройки модов | Keyed `Options` (Menus_Main), `ModSettings` (Menu_Options) |
| caravan | 远行队 | караван | Keyed `Caravan` |
| outpost | 哨所 | лагерь | SitePartDef `Outpost.label` |
| OK (button) | 确定 | OK (Latin letters, as vanilla) | Keyed `OK` |
| recruit | 招募 | вербовать (вербовка) | Keyed `RecruitSuccess`; Russian `RecruitDifficulty` |
| enslave / slave | 奴役 / 奴隶 | поработить / раб | Keyed `OptionalEnslaveDesc`, `Slave` (Ideology) |
| wanderer / refugee | 流浪者 / 难民 | странник / беженец | Keyed `ChooseNewWanderers` (Russian), `Refugee` |

## Mod terms

| English | ChineseSimplified | Russian |
| --- | --- | --- |
| scrap | 废料 | лом |
| <era> scrap | <era>废料 (石器时代废料, 中世纪废料) | <era> лом (неолитический лом, космический лом) |
| studied scrap / studied <era> scrap | 已调查废料 / 已调查的<era>废料 | изученный лом / изученный <era> лом |
| fresh scrap | 未调查的废料 | неизученный лом |
| recycle / recycling | 回收 | переработать / переработка |
| unfamiliar | 陌生 | незнакомый |
| unfamiliar (item label marker) | 陌生 | незнакомая технология |
| unfamiliar technology | 陌生技术 | незнакомая технология |
| original item | 原物品 | исходный предмет |
| Who (settings section) | 对象 | Кого касается |
| Apply item rules (Who choice) | 应用物品规则 | Применять правила для предметов |
| Exclude (Who choice) | 排除 | Исключить |
| Mark unfamiliar (Who choice) | 标记为陌生 | Помечать как незнакомое |
| Don't drop your stuff (settings section) | 不要丢下装备 | Не бросать снаряжение |
| outcome: Vanilla (as in the base game) | 原版 | Ваниль |
| beyond the colony's reach / out of reach | 超出殖民地能力范围 | вне досягаемости поселения |
| within reach | 在殖民地能力范围内 | в пределах досягаемости |
| era | 时代 | технический уровень (эпоха only where no era word follows) |
| gear with no era of its own | 没有自身时代的装备 | снаряжение без собственного уровня |
| item rule / rule | 物品规则 / 规则 | правило для предметов / правило |
| outcome: Keep | 保留 | оставить |
| outcome: Degrade | 损坏 | повредить |
| outcome: Scrap | 变为废料 | в лом |
| outcome: Disassemble | 拆解 | разобрать |
| outcome: Delete | 删除 | удалить |
| chance | 几率 | шанс |
| degrade / degraded | 损坏 / 已损坏 | повредить / повреждённый |
| preset | 预设 | пресет |
| Progression (preset) | 渐进 | Прогрессия |
| Cleanup (preset) | 清理 | Очистка |
| Vanilla (preset) | 原版 | Ваниль |
| keep list / Always keep | 保留列表 / 始终保留 | список «Всегда оставлять» / Всегда оставлять |
| remove list / Always remove | 移除列表 / 始终移除 | список «Всегда удалять» / Всегда удалять |
| modpack | 整合包 | сборка |
| locked / lock | 已锁定 / 锁定 | заблокировано / блокировка |
| affected pawns / affected groups | 受影响的角色 / 受影响的群体 | затронутые персонажи / затронутые группы |
| ticked (checkbox) | 勾选 | отмечен |
| drops as normal | 照常掉落 | выпадает как обычно |
| left alone | 不受影响 | не затрагивается |
| never touched | 永远不会被处理 | никогда не затрагиваются |
| follows the rules below | 遵循下方规则 | подчиняется правилам ниже |
| inventory (carried items) | 随身物品 | инвентарь |
| unit (of scrap) | 单位 | единица (avoid with a count: see Style) |
| on the body / beside the body | 留在尸体上 / 尸体旁 | остаётся на трупе / рядом с трупом |
| worn (apparel) | 穿着的 | надетый |
| protect (quest items, relics) | 保护 | защищать |
| setting / settings | 设置 | настройка / настройки |
| the base game | 原版游戏 | базовая игра |
| cremated | 火化 | кремирован |
| disassembly efficiency | 拆解效率 | эффективность разборки |
| sort / sorted (the rules judge gear) | 处理 | обрабатывать / обрабатывается |
| drops whole | 完整掉落 | выпадает целым |
| sold (a pawn, by trade) | 被出售 | продажа (при аресте или продаже) |

## Style

- Chinese: full-width punctuation (，。：；（）「」), no spaces around numbers or placeholders. The quoted vanilla text keeps its own spaces: 维持 X 个 and Mod 设置.
- Chinese: an era slot already carries 时代 (石器时代, 工业时代) except 中世纪. Never write 时代 after `{0}`; write 达到{0}, not 达到{0}时代.
- Chinese: the unfamiliar marker uses full-width brackets: 物品名（陌生）.
- Russian: era words arrive as masculine nominative adjectives. Pair them with the masculine лом, or put the slot after a dash or colon as vanilla does: «технический уровень — {0}». Never write «{0} эпоха» or decline the slot.
- Russian: quality names are a mix of adverbs and nouns (нормально, шедевр) and cannot be declined. Put the slot after a colon or in quotes: «качество ниже «{0}»».
- Russian: avoid number agreement where a count is a placeholder. Use «ед.», «шт.» or a colon: «Очки исследования за единицу: {0}», «Предметов: {0}».
- Both: "Loot Scavenging" and DLC names (Royalty, Ideology, Biotech, Anomaly, Odyssey) stay in English.

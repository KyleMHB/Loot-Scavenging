using System;
using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace LootScavenging.SettingsUI;

/// <summary>
/// The mod settings page (ticket 25, decision 1): eight sections, each row followed by a sentence saying what will happen.
/// It edits <see cref="LootScavengingSettings"/> in place; the rules read those fields directly, so a change applies at once.
/// </summary>
internal sealed class SettingsPage
{
    private const float IndentWidth = 24f;
    private const float UnboundedHeight = 100000f;

    // Vanilla (Keep) last: the three that change an item first, as the ticket lists them.
    private static readonly Outcome[] Outcomes = { Outcome.Scrap, Outcome.Disassemble, Outcome.Delete, Outcome.Keep };

    private static readonly WhoRule[] WhoRules = { WhoRule.Apply, WhoRule.Exclude, WhoRule.Mark };

    private static readonly ItemQuality[] Qualities =
    {
        ItemQuality.Poor, ItemQuality.Normal, ItemQuality.Good, ItemQuality.Excellent, ItemQuality.Masterwork, ItemQuality.Legendary,
    };

    private static readonly PresetId[] PresetOrder = { PresetId.Progression, PresetId.Cleanup, PresetId.Vanilla };

    private static readonly ScrapTier[] Tiers = (ScrapTier[])Enum.GetValues(typeof(ScrapTier));

    private LootScavengingSettings s;

    // The text typed in each number box, kept between frames as the game's number field needs. Keyed by the box.
    private readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
    private Vector2 scroll;
    private float viewHeight = 4000f;

    public void Draw(Rect inRect, LootScavengingSettings settings)
    {
        s = settings;
        float[] defValuesBefore = DefValues();

        var view = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(inRect, ref scroll, view);
        var ls = new Listing_Standard();
        // The listing gets no height limit: a Listing starts a new column when a row passes its rect's bottom, so sizing it
        // from last frame's height wrapped the rows off to the right whenever the page grew (a preset switch adds rows),
        // and the shrunken CurHeight then collapsed the page to its first rows.
        ls.Begin(new Rect(view.x, view.y, view.width, UnboundedHeight));
        DrawPresets(ls);
        DrawKeepGear(ls);
        DrawWho(ls);
        DrawItemRules(ls);
        DrawScrapAndStudy(ls);
        DrawCorpses(ls);
        DrawLists(ls);
        DrawOther(ls);
        viewHeight = ls.CurHeight + 24f;
        ls.End();
        Widgets.EndScrollView();

        EnforceLocks();
        float[] defValuesAfter = DefValues();
        for (int i = 0; i < defValuesAfter.Length; i++)
        {
            if (defValuesAfter[i] != defValuesBefore[i])
            {
                LootScavengingMod.ApplySettingsToDefs();
                // The first values are the scrap market values; the rest are the research points the 0.5.2 note is about.
                if (i >= Tiers.Length)
                    s.studyPointsHalvedNote = false;
                break;
            }
        }
    }

    // The values copied onto defs (scrap market value and study points per era), compared each frame so the defs follow
    // the page.
    private float[] DefValues()
    {
        var values = new float[Tiers.Length * 2];
        for (int i = 0; i < Tiers.Length; i++)
        {
            values[i] = s.values.ScrapMarketValue(Tiers[i]);
            values[Tiers.Length + i] = s.values.StudyPoints(Tiers[i]);
        }
        return values;
    }

    // A preset or Reset changed the values under the page: drop the typed text so the number boxes show the new values.
    private void ValuesReplaced()
    {
        buffers.Clear();
        s.studyPointsHalvedNote = false;
        LootScavengingMod.ApplySettingsToDefs();
    }

    // The disabled rows already refuse input; this puts back any locked value a widget still managed to change, so a lock
    // holds whatever the row does.
    private void EnforceLocks()
    {
        PresetConfig config = Presets.Config;
        if (!config.Locks.Any)
            return;
        SettingValues fixedValues = s.Capture().Clone();
        config.Locks.Enforce(fixedValues, config.Progression);
        if (!fixedValues.SameAs(s.Capture()))
            s.Apply(fixedValues);
    }

    // Presets

    private void DrawPresets(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionPresets".Translate());
        string active = PresetName(s.activePreset);
        bool changed = s.PresetChanged;
        ls.Label(changed ? "LootScavenging_PresetActiveChanged".Translate(active) : "LootScavenging_PresetActive".Translate(active));
        Explain(ls, changed ? "LootScavenging_PresetChangedExplain".Translate(active) : PresetSentence(s.activePreset));

        if (Presets.Config.Locks.Any)
            Explain(ls, "LootScavenging_LockedSummary".Translate());

        Rect row = ls.GetRect(32f);
        float width = (row.width - 3 * 8f) / 4f;
        for (int i = 0; i < PresetOrder.Length; i++)
        {
            PresetId preset = PresetOrder[i];
            var button = new Rect(row.x + i * (width + 8f), row.y, width, row.height);
            TooltipHandler.TipRegion(button, PresetSentence(preset));
            if (Widgets.ButtonText(button, PresetName(preset)))
            {
                s.Apply(Presets.Apply(preset, s.Capture()));
                s.activePreset = preset;
                ValuesReplaced();
            }
        }
        var reset = new Rect(row.x + 3 * (width + 8f), row.y, width, row.height);
        if (Widgets.ButtonText(reset, "LootScavenging_PresetReset".Translate()))
        {
            LootScavengingSettings settings = s;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("LootScavenging_ResetConfirm".Translate(), () =>
            {
                settings.Apply(Presets.Defaults());
                settings.activePreset = PresetId.Progression;
                ValuesReplaced();
            }, destructive: true));
        }
        ls.Gap(6f);
    }

    private static string PresetName(PresetId preset)
    {
        switch (preset)
        {
            case PresetId.Cleanup: return "LootScavenging_PresetCleanup".Translate();
            case PresetId.Vanilla: return "LootScavenging_PresetVanilla".Translate();
            default: return "LootScavenging_PresetProgression".Translate();
        }
    }

    private static string PresetSentence(PresetId preset)
    {
        switch (preset)
        {
            case PresetId.Cleanup: return "LootScavenging_PresetCleanupExplain".Translate();
            case PresetId.Vanilla: return "LootScavenging_PresetVanillaExplain".Translate();
            default: return "LootScavenging_PresetProgressionExplain".Translate();
        }
    }

    // Don't drop your stuff

    private void DrawKeepGear(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionKeepGear".Translate());
        SettingValues v = s.values;
        Toggle(ls, "LootScavenging_ColonistsKeepGear".Translate(), ref v.colonistsKeepGear,
            "LootScavenging_ColonistsKeepGearOn".Translate(), "LootScavenging_ColonistsKeepGearOff".Translate(), SettingNames.ColonistsKeepGear);
        Toggle(ls, "LootScavenging_HostilesKeepGear".Translate(), ref v.hostilesKeepGear,
            "LootScavenging_HostilesKeepGearOn".Translate(), "LootScavenging_HostilesKeepGearOff".Translate(), SettingNames.HostilesKeepGear);
        Toggle(ls, "LootScavenging_NonHostilesKeepGear".Translate(), ref v.nonHostilesKeepGear,
            "LootScavenging_NonHostilesKeepGearOn".Translate(), "LootScavenging_NonHostilesKeepGearOff".Translate(), SettingNames.NonHostilesKeepGear);
        Toggle(ls, "LootScavenging_CremationStripsCorpses".Translate(), ref v.cremationStripsCorpses,
            "LootScavenging_CremationStripsCorpsesOn".Translate(), "LootScavenging_CremationStripsCorpsesOff".Translate(), SettingNames.CremationStripsCorpses);
        Explain(ls, "LootScavenging_KeepGearExplain".Translate());
    }

    // Who

    private void DrawWho(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionWho".Translate());
        LootRules r = s.values.rules;
        WhoRow(ls, OwnerGroup.Hostile, "LootScavenging_WhoHostile", "LootScavenging_WhoHostileApply", "LootScavenging_WhoHostileExclude", r.whoHostile, "LootScavenging_WhoMarkExplain");
        WhoRow(ls, OwnerGroup.NonHostileFaction, "LootScavenging_WhoNonHostile", "LootScavenging_WhoNonHostileApply", "LootScavenging_WhoNonHostileExclude", r.whoNonHostile, "LootScavenging_WhoMarkExplain");
        WhoRow(ls, OwnerGroup.NoFaction, "LootScavenging_WhoWild", "LootScavenging_WhoWildApply", "LootScavenging_WhoWildExclude", r.whoWild, "LootScavenging_WhoMarkExplain");
        WhoRow(ls, OwnerGroup.Prisoner, "LootScavenging_WhoPrisoners", "LootScavenging_WhoPrisonersApply", "LootScavenging_WhoPrisonersExclude", r.whoPrisoners, "LootScavenging_WhoMarkExplain");
        WhoRow(ls, OwnerGroup.RecruitedPrisoner, "LootScavenging_WhoRecruitedPrisoners", "LootScavenging_WhoRecruitedPrisonersApply", "LootScavenging_WhoRecruitedPrisonersExclude", r.whoRecruitedPrisoners, "LootScavenging_WhoJoinMarkExplain");
        WhoRow(ls, OwnerGroup.OtherJoiner, "LootScavenging_WhoOtherJoiners", "LootScavenging_WhoOtherJoinersApply", "LootScavenging_WhoOtherJoinersExclude", r.whoOtherJoiners, "LootScavenging_WhoJoinMarkExplain");

        bool anyAffected = false;
        foreach (OwnerGroup group in LootRules.RowGroups)
            anyAffected |= r.RowFor(group) != WhoRule.Exclude;
        if (!anyAffected)
            Explain(ls, "LootScavenging_NoGroupOn".Translate());
    }

    // One row: the group's label, its three choices, and a sentence for each. Mark has one sentence for the loot groups and
    // one for the joiners. The keys are passed whole so the translation check can see them.
    private void WhoRow(Listing_Standard ls, OwnerGroup group, string key, string applyKey, string excludeKey, WhoRule current, string markKey)
    {
        LootScavengingSettings settings = s;
        Choice(ls, key.Translate(), current, WhoRules, WhoRuleName, v => settings.values.rules.SetRow(group, v), SettingNames.Row(group));
        switch (current)
        {
            case WhoRule.Exclude: Explain(ls, excludeKey.Translate()); break;
            case WhoRule.Mark: Explain(ls, markKey.Translate()); break;
            default: Explain(ls, applyKey.Translate()); break;
        }
    }

    private static string WhoRuleName(WhoRule rule)
    {
        switch (rule)
        {
            case WhoRule.Exclude: return "LootScavenging_WhoExclude".Translate();
            case WhoRule.Mark: return "LootScavenging_WhoMark".Translate();
            default: return "LootScavenging_WhoApply".Translate();
        }
    }

    // Item rules

    private void DrawItemRules(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionItems".Translate());
        Explain(ls, "LootScavenging_ItemsNote".Translate());
        foreach (CategoryInfo category in ItemCategories.All)
            DrawCategory(ls, category);
    }

    private void DrawCategory(Listing_Standard ls, CategoryInfo info)
    {
        LootScavengingSettings settings = s;
        LootRules r = s.values.rules;
        CategoryRule rule = info.Rule(r);
        string lockName = SettingNames.Category(info);
        bool catLocked = IsLocked(lockName);
        ls.Gap(4f);
        Choice(ls, info.LabelKey.Translate(), rule.outcome, Outcomes, OutcomeName, v => info.Rule(settings.values.rules).outcome = v, lockName);

        Indent(ls);
        // Everything below belongs to the category, so a locked category disables it all. The shared disassembly
        // efficiency can also be locked on its own.
        ambientDisabled = catLocked;
        if (rule.outcome != Outcome.Keep)
        {
            // Shown beside the rule that uses it; the value is shared by every disassemble rule.
            if (rule.outcome == Outcome.Disassemble && r.disassemblyYield == DisassemblyYield.ItemBased)
                // One buffer for every box, since they all show the one value.
                r.disassemblyEfficiency = PercentBox(ls, "LootScavenging_DisassemblyEfficiency".Translate(), "efficiency", // not translated
                    r.disassemblyEfficiency, 5, 100, SettingNames.DisassemblyEfficiency);
            rule.chance = PercentBox(ls, ChanceLabel(rule.outcome), "chance." + info.Field, rule.chance, 0, 100); // not translated
            Checkbox(ls, "LootScavenging_OnlyBeyondReach".Translate(), ref rule.onlyBeyondReach, false);
            Checkbox(ls, "LootScavenging_LimitQuality".Translate(), ref rule.limitQuality, false);
            if (rule.limitQuality)
                Choice(ls, "LootScavenging_BelowQuality".Translate(), rule.belowQuality, Qualities, QualityName, v => info.Rule(settings.values.rules).belowQuality = v, lockName);
            Checkbox(ls, "LootScavenging_LimitHitPoints".Translate(), ref rule.limitHitPoints, false);
            if (rule.limitHitPoints)
                rule.belowHitPointFraction = PercentBox(ls, "LootScavenging_BelowHitPoints".Translate(), "hitPoints." + info.Field, // not translated
                    rule.belowHitPointFraction, 5, 100);
        }
        ambientDisabled = false;
        SetDisabled(false);
        Outdent(ls);
        Explain(ls, RuleSentence(info, rule));
        LockedNote(ls, catLocked);
    }

    private static string ChanceLabel(Outcome outcome)
    {
        switch (outcome)
        {
            case Outcome.Disassemble: return "LootScavenging_ChanceDisassemble".Translate();
            case Outcome.Delete: return "LootScavenging_ChanceDelete".Translate();
            default: return "LootScavenging_ChanceScrap".Translate();
        }
    }

    private string RuleSentence(CategoryInfo info, CategoryRule rule)
    {
        string what = info.LabelKey.Translate();
        if (rule.outcome == Outcome.Keep)
            return "LootScavenging_RuleKeep".Translate(what);

        int limits = (rule.limitQuality ? 1 : 0) + (rule.limitHitPoints ? 2 : 0);
        int index = (rule.onlyBeyondReach ? 8 : 0) + limits * 2 + (rule.chance < 1f ? 1 : 0);
        return RuleActKeys[index].Translate(what, OutcomeVerb(rule.outcome), QualityName(rule.belowQuality),
            rule.belowHitPointFraction.ToStringPercent(), rule.chance.ToStringPercent());
    }

    // One whole-sentence key for each combination of conditions and chance, so a translation controls the word order,
    // joining words and punctuation (translation spec, Decision 3). Indexed by beyond reach (+8), the limits (+2 each step:
    // none, quality, hit points, quality or hit points) and a chance below 100% (+1). Every key takes the same slots:
    // {0} category, {1} outcome verb phrase, {2} quality, {3} hit points, {4} chance.
    private static readonly string[] RuleActKeys =
    {
        "LootScavenging_RuleAct", "LootScavenging_RuleActChance",
        "LootScavenging_RuleActQuality", "LootScavenging_RuleActQualityChance",
        "LootScavenging_RuleActHitPoints", "LootScavenging_RuleActHitPointsChance",
        "LootScavenging_RuleActQualityOrHitPoints", "LootScavenging_RuleActQualityOrHitPointsChance",
        "LootScavenging_RuleActReach", "LootScavenging_RuleActReachChance",
        "LootScavenging_RuleActReachQuality", "LootScavenging_RuleActReachQualityChance",
        "LootScavenging_RuleActReachHitPoints", "LootScavenging_RuleActReachHitPointsChance",
        "LootScavenging_RuleActReachQualityOrHitPoints", "LootScavenging_RuleActReachQualityOrHitPointsChance",
    };

    private string OutcomeVerb(Outcome outcome)
    {
        LootRules r = s.values.rules;
        switch (outcome)
        {
            case Outcome.Scrap: return "LootScavenging_VerbScrap".Translate();
            case Outcome.Disassemble:
                return r.disassemblyYield == DisassemblyYield.ItemBased
                    ? "LootScavenging_VerbDisassembleItem".Translate(r.disassemblyEfficiency.ToStringPercent())
                    : "LootScavenging_VerbDisassembleEra".Translate();
            default: return "LootScavenging_VerbDelete".Translate();
        }
    }

    private static string OutcomeName(Outcome outcome)
    {
        switch (outcome)
        {
            case Outcome.Scrap: return "LootScavenging_OutcomeScrap".Translate();
            case Outcome.Disassemble: return "LootScavenging_OutcomeDisassemble".Translate();
            case Outcome.Delete: return "LootScavenging_OutcomeDelete".Translate();
            default: return "LootScavenging_OutcomeKeep".Translate();
        }
    }

    // ItemQuality mirrors the game's QualityCategory, so the game's own translated label is used.
    private static string QualityName(ItemQuality quality) => ((QualityCategory)(byte)quality).GetLabel();

    // Scrap and study

    private void DrawScrapAndStudy(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionScrap".Translate());
        LootScavengingSettings settings = s;
        SettingValues v = s.values;

        ls.Label("LootScavenging_StudyPointsPerTier".Translate());
        Indent(ls);
        foreach (ScrapTier tier in Tiers)
            v.SetStudyPoints(tier, NumberBox(ls, TierLabel(tier), "points." + tier, v.StudyPoints(tier), 0f, SettingValues.MaxStudyPoints, // not translated
                SettingNames.StudyPoints(tier)));
        Outdent(ls);
        Explain(ls, "LootScavenging_StudyPointsPerTierExplain".Translate());
        if (s.studyPointsHalvedNote)
            Explain(ls, "LootScavenging_StudyPointsHalvedNote".Translate());

        Toggle(ls, "LootScavenging_TechprintProjects".Translate(), ref v.studyTechprintProjects,
            "LootScavenging_TechprintProjectsOn".Translate(), "LootScavenging_TechprintProjectsOff".Translate(), SettingNames.StudyTechprintProjects);

        ls.Label("LootScavenging_ScrapValuePerTier".Translate());
        Indent(ls);
        foreach (ScrapTier tier in Tiers)
        {
            string id = "value." + tier; // not translated
            float value = NumberBox(ls, TierLabel(tier), id, v.ScrapMarketValue(tier),
                SettingValues.MinScrapMarketValue, SettingValues.MaxScrapMarketValue, SettingNames.ScrapMarketValue(tier));
            float rounded = (float)Math.Round(value, 2);
            // Two places are stored, so the box shows the rounded value rather than the extra digits typed.
            if (rounded != value)
                buffers[id] = rounded.ToString();
            v.SetScrapMarketValue(tier, rounded);
        }
        Outdent(ls);
        Explain(ls, "LootScavenging_ScrapValuePerTierExplain".Translate());

        Choice(ls, "LootScavenging_RecycleOrStudy".Translate(), v.recycleOrStudy,
            new[] { RecycleOrStudy.OneOrTheOther, RecycleOrStudy.StudyThenRecycle }, RecycleOrStudyName, x => settings.values.recycleOrStudy = x, SettingNames.RecycleOrStudy);
        Explain(ls, v.recycleOrStudy == RecycleOrStudy.StudyThenRecycle
            ? "LootScavenging_StudyThenRecycleExplain".Translate()
            : "LootScavenging_OneOrTheOtherExplain".Translate());
    }

    // The tier's scrap label, as the player sees it on the item.
    private static string TierLabel(ScrapTier tier)
    {
        ThingDef scrap = ScrapDefs.For(tier);
        return scrap != null ? scrap.LabelCap.Resolve() : tier.ToString();
    }

    private static string RecycleOrStudyName(RecycleOrStudy mode)
    {
        return mode == RecycleOrStudy.StudyThenRecycle
            ? "LootScavenging_StudyThenRecycle".Translate()
            : "LootScavenging_OneOrTheOther".Translate();
    }

    // Corpses

    private void DrawCorpses(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionCorpses".Translate());
        CorpseRules c = s.values.corpses;
        Toggle(ls, "LootScavenging_CorpseHumanlike".Translate(), ref c.humanlike,
            "LootScavenging_CorpseHumanlikeOn".Translate(), "LootScavenging_CorpseHumanlikeOff".Translate(), SettingNames.CorpseHumanlike);
        Toggle(ls, "LootScavenging_CorpseAnimal".Translate(), ref c.animal,
            "LootScavenging_CorpseAnimalOn".Translate(), "LootScavenging_CorpseAnimalOff".Translate(), SettingNames.CorpseAnimal);
        Toggle(ls, "LootScavenging_CorpseMechanoid".Translate(), ref c.mechanoid,
            "LootScavenging_CorpseMechanoidOn".Translate(), "LootScavenging_CorpseMechanoidOff".Translate(), SettingNames.CorpseMechanoid);
        Explain(ls, "LootScavenging_ButcherExplain".Translate());
    }

    // Lists

    private void DrawLists(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionLists".Translate());
        LootRules r = s.values.rules;
        Toggle(ls, "LootScavenging_ProtectQuest".Translate(), ref r.protectQuestItems,
            "LootScavenging_ProtectQuestOn".Translate(), "LootScavenging_ProtectQuestOff".Translate());
        Toggle(ls, "LootScavenging_ProtectUnique".Translate(), ref r.protectUniqueWeapons,
            "LootScavenging_ProtectUniqueOn".Translate(), "LootScavenging_ProtectUniqueOff".Translate());
        Toggle(ls, "LootScavenging_ProtectPersona".Translate(), ref r.protectPersonaWeapons,
            "LootScavenging_ProtectPersonaOn".Translate(), "LootScavenging_ProtectPersonaOff".Translate());
        Toggle(ls, "LootScavenging_ProtectRelics".Translate(), ref r.protectRelics,
            "LootScavenging_ProtectRelicsOn".Translate(), "LootScavenging_ProtectRelicsOff".Translate());

        ls.Gap(6f);
        ls.Label("LootScavenging_KeepList".Translate());
        Explain(ls, "LootScavenging_KeepListExplain".Translate());
        DrawItemList(ls, r.keepList, "LootScavenging_KeepList".Translate());

        ls.Gap(6f);
        ls.Label("LootScavenging_RemoveList".Translate());
        Explain(ls, "LootScavenging_RemoveListExplain".Translate());
        DrawItemList(ls, r.removeList, "LootScavenging_RemoveList".Translate());
    }

    // One list: its entries with a remove button each, then the button that opens the item picker for it.
    private static void DrawItemList(Listing_Standard ls, List<string> list, string title)
    {
        Indent(ls);
        DrawListEntries(ls, list);
        DrawAddButton(ls, list, title);
        Outdent(ls);
    }

    private static void DrawListEntries(Listing_Standard ls, List<string> list)
    {
        if (list.Count == 0)
        {
            ls.Label("LootScavenging_ListEmpty".Translate());
            return;
        }
        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            Rect row = ls.GetRect(26f);
            var buttonRect = new Rect(row.xMax - 90f, row.y + 1f, 90f, 24f);
            Widgets.Label(new Rect(row.x, row.y + 2f, row.width - 96f, row.height), EntryLabel(list[i]));
            if (Widgets.ButtonText(buttonRect, "LootScavenging_ListRemove".Translate()))
                removeAt = i;
        }
        if (removeAt >= 0)
            list.RemoveAt(removeAt);
    }

    private static void DrawAddButton(Listing_Standard ls, List<string> list, string title)
    {
        Rect row = ls.GetRect(28f);
        if (Widgets.ButtonText(new Rect(row.x, row.y, 200f, 28f), "LootScavenging_ListAddItem".Translate()))
            Find.WindowStack.Add(new Dialog_ItemPicker(title, list));
        Explain(ls, "LootScavenging_ListAddExplain".Translate());
    }

    // Entries are def names; an item from a removed mod keeps its entry and shows as not loaded.
    private static string EntryLabel(string defName)
    {
        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        return def != null ? def.LabelCap + " (" + defName + ")" : "LootScavenging_ListNotLoaded".Translate(defName).Resolve();
    }

    // Other

    private void DrawOther(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionOther".Translate());
        LootScavengingSettings settings = s;
        SettingValues v = s.values;

        Choice(ls, "LootScavenging_DisassemblyYield".Translate(), v.rules.disassemblyYield,
            new[] { DisassemblyYield.EraTypical, DisassemblyYield.ItemBased }, DisassemblyYieldName, x => settings.values.rules.disassemblyYield = x, SettingNames.DisassemblyYield);
        Explain(ls, v.rules.disassemblyYield == DisassemblyYield.ItemBased
            ? "LootScavenging_YieldItemExplain".Translate()
            : "LootScavenging_YieldEraExplain".Translate());

        Choice(ls, "LootScavenging_UnfamiliarScope".Translate(), v.unfamiliarScope,
            new[] { UnfamiliarScope.LootedOnly, UnfamiliarScope.AllOutOfReach }, UnfamiliarScopeName, x => settings.values.unfamiliarScope = x, SettingNames.UnfamiliarScope);
        // Whole-sentence keys, with and without the "only used" note, so a translation controls the separator.
        bool looted = v.unfamiliarScope == UnfamiliarScope.LootedOnly;
        // Only the loot rows decide it, as in Unfamiliar: joiner marks are saved on the item and hold under either scope.
        bool unused = !v.rules.AnyLootRowMarks;
        Explain(ls, (looted
            ? (unused ? "LootScavenging_UnfamiliarLootedExplainUnused" : "LootScavenging_UnfamiliarLootedExplain")
            : (unused ? "LootScavenging_UnfamiliarAllExplainUnused" : "LootScavenging_UnfamiliarAllExplain")).Translate());

        Toggle(ls, "LootScavenging_HomeMapOnly".Translate(), ref v.rules.homeMapOnly,
            "LootScavenging_HomeMapOnlyOn".Translate(), "LootScavenging_HomeMapOnlyOff".Translate(), SettingNames.HomeMapOnly);
    }

    private static string DisassemblyYieldName(DisassemblyYield yield)
    {
        return yield == DisassemblyYield.ItemBased
            ? "LootScavenging_YieldItem".Translate()
            : "LootScavenging_YieldEra".Translate();
    }

    private static string UnfamiliarScopeName(UnfamiliarScope scope)
    {
        return scope == UnfamiliarScope.LootedOnly
            ? "LootScavenging_UnfamiliarLooted".Translate()
            : "LootScavenging_UnfamiliarAll".Translate();
    }

    // Widgets

    private static void Section(Listing_Standard ls, string title)
    {
        ls.Gap(12f);
        Text.Font = GameFont.Medium;
        ls.Label(title);
        Text.Font = GameFont.Small;
        ls.GapLine(6f);
    }

    /// <summary>The "what will happen" sentence under a row.</summary>
    private static void Explain(Listing_Standard ls, string sentence)
    {
        Indent(ls);
        GUI.color = new Color(0.75f, 0.75f, 0.75f);
        Text.Font = GameFont.Tiny;
        ls.Label(sentence);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Outdent(ls);
        ls.Gap(4f);
    }

    // A locked row (a pack setting, see SettingLocks) is drawn disabled and followed by the reason.
    private static bool IsLocked(string lockName) => lockName != null && Presets.Config.Locks.IsLocked(lockName);

    // True while a locked category's whole block is drawn, so a row inside it stays disabled after its own lock check.
    private static bool ambientDisabled;

    private static void SetDisabled(bool disabled) => GUI.enabled = !(disabled || ambientDisabled);

    private static void LockedNote(Listing_Standard ls, bool locked)
    {
        if (locked)
            Explain(ls, "LootScavenging_LockedReason".Translate());
    }

    private static void Toggle(Listing_Standard ls, string label, ref bool value, string onSentence, string offSentence, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        SetDisabled(locked);
        Checkbox(ls, label, ref value, locked);
        SetDisabled(false);
        Explain(ls, value ? onSentence : offSentence);
        LockedNote(ls, locked);
    }

    // Listing_Standard.CheckboxLabeled laid out the same way, with the game checkbox's own disabled flag: GUI.enabled does not
    // stop that checkbox flipping, so a locked box needs the flag. A box in a locked category is disabled too.
    private static void Checkbox(Listing_Standard ls, string label, ref bool value, bool disabled)
    {
        Rect rect = ls.GetRect(Text.CalcHeight(label, ls.ColumnWidth));
        Widgets.CheckboxLabeled(rect, label, ref value, disabled || ambientDisabled);
        ls.Gap(ls.verticalSpacing);
    }

    private static void Choice<T>(Listing_Standard ls, string label, T current, T[] options, Func<T, string> name, Action<T> set, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        SetDisabled(locked);
        bool clicked = ls.ButtonTextLabeledPct(label, name(current), 0.6f);
        SetDisabled(false);
        LockedNote(ls, locked);
        if (locked || ambientDisabled || !clicked)
            return;
        var menu = new List<FloatMenuOption>();
        foreach (T option in options)
        {
            T picked = option;
            menu.Add(new FloatMenuOption(name(picked), () => set(picked)));
        }
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    // A number box clamped to [min, max] (ticket 25, decision 1). A locked box keeps its value whatever is typed.
    private float NumberBox(Listing_Standard ls, string label, string id, float value, float min, float max, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        buffers.TryGetValue(id, out string buffer);
        float typed = value;
        SetDisabled(locked);
        ls.TextFieldNumericLabeled(label, ref typed, ref buffer, min, max);
        SetDisabled(false);
        buffers[id] = buffer;
        LockedNote(ls, locked);
        return locked || ambientDisabled ? value : typed;
    }

    // A fraction typed as a whole percent from min to max. Whole percents let a value set back by hand match a preset again.
    private float PercentBox(Listing_Standard ls, string label, string id, float fraction, int min, int max, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        buffers.TryGetValue(id, out string buffer);
        int percent = Mathf.RoundToInt(fraction * 100f);
        int typed = percent;
        SetDisabled(locked);
        ls.TextFieldNumericLabeled(label, ref typed, ref buffer, min, max);
        SetDisabled(false);
        buffers[id] = buffer;
        LockedNote(ls, locked);
        if (locked || ambientDisabled || typed == percent)
            return fraction;
        return typed / 100f;
    }

    private static void Indent(Listing_Standard ls)
    {
        ls.Indent(IndentWidth);
        ls.ColumnWidth -= IndentWidth;
    }

    private static void Outdent(Listing_Standard ls)
    {
        ls.Outdent(IndentWidth);
        ls.ColumnWidth += IndentWidth;
    }
}

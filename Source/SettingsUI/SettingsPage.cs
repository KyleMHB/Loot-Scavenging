using System;
using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace LootScavenging.SettingsUI;

/// <summary>
/// The mod settings page (spec, "Settings page"): six sections, each row followed by a sentence saying what will happen.
/// It edits <see cref="LootScavengingSettings"/> in place; the rules read those fields directly, so a change applies at once.
/// </summary>
internal sealed class SettingsPage
{
    private const float IndentWidth = 24f;
    private const float UnboundedHeight = 100000f;

    private static readonly Outcome[] Outcomes = { Outcome.Keep, Outcome.Degrade, Outcome.Scrap, Outcome.Disassemble, Outcome.Delete };

    private static readonly ItemQuality[] Qualities =
    {
        ItemQuality.Poor, ItemQuality.Normal, ItemQuality.Good, ItemQuality.Excellent, ItemQuality.Masterwork, ItemQuality.Legendary,
    };

    private static readonly PresetId[] PresetOrder = { PresetId.Progression, PresetId.Cleanup, PresetId.Vanilla };

    private static readonly ScrapTier[] StudyTiers = (ScrapTier[])Enum.GetValues(typeof(ScrapTier));

    private LootScavengingSettings s;

    // The text typed in each tier's study points field, kept between frames as the number field needs.
    private readonly string[] pointBuffers = new string[StudyTiers.Length];
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
        DrawWhoAndWhere(ls);
        DrawItemRules(ls);
        DrawScrapAndStudy(ls);
        DrawCorpses(ls);
        DrawLists(ls);
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
                // Index 0 is the scrap value; the rest are the research points the 0.5.2 note is about.
                if (i > 0)
                    s.studyPointsHalvedNote = false;
                break;
            }
        }
    }

    // The values copied onto defs (scrap market value, study points), compared each frame so the defs follow the page.
    private float[] DefValues()
    {
        var values = new float[StudyTiers.Length + 2];
        values[0] = s.values.scrapMarketValue;
        values[1] = s.values.studyPointsMultiplier;
        for (int i = 0; i < StudyTiers.Length; i++)
            values[i + 2] = s.values.StudyPoints(StudyTiers[i]);
        return values;
    }

    // A preset or Reset changed the values under the page: drop the typed text so the number fields show the new values.
    private void ValuesReplaced()
    {
        Array.Clear(pointBuffers, 0, pointBuffers.Length);
        s.studyPointsHalvedNote = false;
        LootScavengingMod.ApplySettingsToDefs();
    }

    // The disabled rows already refuse input; this puts back any locked value a widget still managed to change (the
    // degrade range widget reads the mouse itself), so a lock holds whatever the row does.
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

    // Who and where

    private void DrawWhoAndWhere(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionWho".Translate());
        LootRules r = s.values.rules;
        Toggle(ls, "LootScavenging_Hostile".Translate(), ref r.hostile,
            "LootScavenging_HostileOn".Translate(), "LootScavenging_HostileOff".Translate(), SettingNames.Hostile);
        Toggle(ls, "LootScavenging_NonHostile".Translate(), ref r.nonHostileFaction,
            "LootScavenging_NonHostileOn".Translate(), "LootScavenging_NonHostileOff".Translate(), SettingNames.NonHostileFaction);
        Toggle(ls, "LootScavenging_NoFaction".Translate(), ref r.noFaction,
            "LootScavenging_NoFactionOn".Translate(), "LootScavenging_NoFactionOff".Translate(), SettingNames.NoFaction);
        Toggle(ls, "LootScavenging_Prisoners".Translate(), ref r.prisoners,
            "LootScavenging_PrisonersOn".Translate(), "LootScavenging_PrisonersOff".Translate(), SettingNames.Prisoners);
        Toggle(ls, "LootScavenging_HomeMapOnly".Translate(), ref r.homeMapOnly,
            "LootScavenging_HomeMapOnlyOn".Translate(), "LootScavenging_HomeMapOnlyOff".Translate(), SettingNames.HomeMapOnly);
        Toggle(ls, "LootScavenging_DownedAndStrip".Translate(), ref r.onDownedAndStrip,
            "LootScavenging_DownedAndStripOn".Translate(), "LootScavenging_DownedAndStripOff".Translate(), SettingNames.OnDownedAndStrip);
        if (!r.hostile && !r.nonHostileFaction && !r.noFaction && !r.prisoners)
            Explain(ls, "LootScavenging_NoGroupOn".Translate());
    }

    // Item rules

    private void DrawItemRules(Listing_Standard ls)
    {
        Section(ls, "LootScavenging_SectionItems".Translate());
        Explain(ls, DropModeNote(s.values.rules.dropMode));
        foreach (CategoryInfo category in ItemCategories.All)
            DrawCategory(ls, category);
        ls.Gap(6f);
        Toggle(ls, "LootScavenging_EjectCharges".Translate(), ref s.values.rules.ejectCharges,
            "LootScavenging_EjectChargesOn".Translate(), "LootScavenging_EjectChargesOff".Translate(), SettingNames.EjectCharges);
    }

    private void DrawCategory(Listing_Standard ls, CategoryInfo info)
    {
        LootScavengingSettings settings = s;
        LootRules r = s.values.rules;
        ItemCategory category = info.Category;
        CategoryRule rule = info.Rule(r);
        string lockName = SettingNames.Category(info);
        bool catLocked = IsLocked(lockName);
        ls.Gap(4f);
        Choice(ls, info.LabelKey.Translate(), rule.outcome, Outcomes, OutcomeName, v => info.Rule(settings.values.rules).outcome = v, lockName);

        Indent(ls);
        // Everything below belongs to the category, so a locked category disables it all. The shared degrade and
        // disassembly values can also be locked on their own.
        ambientDisabled = catLocked;
        SetDisabled(false);
        // The outcome's own sliders sit beside it and only show while a rule uses that outcome. The values are shared by
        // every category with the same outcome.
        if (rule.outcome == Outcome.Degrade)
        {
            bool rangeLocked = catLocked || IsLocked(SettingNames.DegradeMin) || IsLocked(SettingNames.DegradeMax);
            ls.Label("LootScavenging_DegradeRange".Translate(r.degradeMin.ToStringPercent(), r.degradeMax.ToStringPercent()));
            var range = new FloatRange(r.degradeMin, r.degradeMax);
            SetDisabled(rangeLocked);
            Widgets.FloatRange(ls.GetRect(28f), 0x15C0 + (int)category, ref range, 0.01f, 1f, null, ToStringStyle.PercentZero, 0.01f, GameFont.Small, null, 0.01f);
            SetDisabled(false);
            if (!rangeLocked)
            {
                r.degradeMin = range.min;
                r.degradeMax = range.max;
            }
            bool qualityLocked = catLocked || IsLocked(SettingNames.DegradeLowersQuality);
            SetDisabled(qualityLocked);
            ls.CheckboxLabeled("LootScavenging_DegradeLowersQuality".Translate(), ref r.degradeLowersQuality);
            SetDisabled(false);
        }
        if (rule.outcome == Outcome.Disassemble && r.disassemblyYield == DisassemblyYield.ItemBased)
        {
            bool efficiencyLocked = catLocked || IsLocked(SettingNames.DisassemblyEfficiency);
            SetDisabled(efficiencyLocked);
            float efficiency = Percent(ls, "LootScavenging_DisassemblyEfficiency".Translate(r.disassemblyEfficiency.ToStringPercent()), r.disassemblyEfficiency, 0.05f, 1f);
            SetDisabled(false);
            if (!efficiencyLocked)
                r.disassemblyEfficiency = efficiency;
        }
        if (rule.outcome != Outcome.Keep)
        {
            rule.chance = Percent(ls, "LootScavenging_Chance".Translate(rule.chance.ToStringPercent()), rule.chance, 0f, 1f);
            ls.CheckboxLabeled("LootScavenging_OnlyBeyondReach".Translate(), ref rule.onlyBeyondReach);
            ls.CheckboxLabeled("LootScavenging_LimitQuality".Translate(), ref rule.limitQuality);
            if (rule.limitQuality)
                Choice(ls, "LootScavenging_BelowQuality".Translate(), rule.belowQuality, Qualities, QualityName, v => info.Rule(settings.values.rules).belowQuality = v, lockName);
            ls.CheckboxLabeled("LootScavenging_LimitHitPoints".Translate(), ref rule.limitHitPoints);
            if (rule.limitHitPoints)
                rule.belowHitPointFraction = Percent(ls, "LootScavenging_BelowHitPoints".Translate(rule.belowHitPointFraction.ToStringPercent()), rule.belowHitPointFraction, 0.05f, 1f);
        }
        ambientDisabled = false;
        SetDisabled(false);
        Outdent(ls);
        Explain(ls, RuleSentence(info, rule));
        LockedNote(ls, catLocked);
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
            case Outcome.Degrade:
                return r.degradeLowersQuality
                    ? "LootScavenging_VerbDegradeQuality".Translate(r.degradeMin.ToStringPercent(), r.degradeMax.ToStringPercent())
                    : "LootScavenging_VerbDegrade".Translate(r.degradeMin.ToStringPercent(), r.degradeMax.ToStringPercent());
            case Outcome.Scrap: return "LootScavenging_VerbScrap".Translate();
            case Outcome.Disassemble:
                return r.disassemblyYield == DisassemblyYield.ItemBased
                    ? "LootScavenging_VerbDisassembleItem".Translate(r.disassemblyEfficiency.ToStringPercent())
                    : "LootScavenging_VerbDisassembleEra".Translate();
            default: return "LootScavenging_VerbDelete".Translate();
        }
    }

    private static string DropModeNote(DropMode mode)
    {
        switch (mode)
        {
            case DropMode.ScrapEverything: return "LootScavenging_ItemsNoteScrapEverything".Translate();
            case DropMode.KeepOriginal: return "LootScavenging_ItemsNoteKeepOriginal".Translate();
            case DropMode.Off: return "LootScavenging_ItemsNoteOff".Translate();
            default: return "LootScavenging_ItemsNoteScrapOutOfReach".Translate();
        }
    }

    private static string OutcomeName(Outcome outcome)
    {
        switch (outcome)
        {
            case Outcome.Degrade: return "LootScavenging_OutcomeDegrade".Translate();
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

        Choice(ls, "LootScavenging_DropMode".Translate(), s.values.rules.dropMode,
            new[] { DropMode.ScrapOutOfReach, DropMode.ScrapEverything, DropMode.KeepOriginal, DropMode.Off }, DropModeName,
            v => settings.values.rules.dropMode = v, SettingNames.DropMode);
        Explain(ls, DropModeSentence(s.values.rules.dropMode));

        // Only the drop mode reads these, so they are hidden while the drop mode is Off.
        if (s.values.rules.dropMode != DropMode.Off)
        {
            Toggle(ls, "LootScavenging_DropIncludesMedicine".Translate(), ref s.values.rules.dropIncludesMedicine,
                "LootScavenging_DropIncludesMedicineOn".Translate(), "LootScavenging_DropIncludesMedicineOff".Translate(), SettingNames.DropIncludesMedicine);
            Toggle(ls, "LootScavenging_DropIncludesAmmunition".Translate(), ref s.values.rules.dropIncludesAmmunition,
                "LootScavenging_DropIncludesAmmunitionOn".Translate(), "LootScavenging_DropIncludesAmmunitionOff".Translate(), SettingNames.DropIncludesAmmunition);
        }

        Choice(ls, "LootScavenging_StudyEras".Translate(), s.values.studyEras,
            new[] { StudyEraLimit.ColonyEraAndBelow, StudyEraLimit.AnyEra }, StudyErasName, v => settings.values.studyEras = v, SettingNames.StudyEras);
        Explain(ls, StudyErasSentence(s.values.studyEras));

        Choice(ls, "LootScavenging_RecycleOrStudy".Translate(), s.values.recycleOrStudy,
            new[] { RecycleOrStudy.OneOrTheOther, RecycleOrStudy.StudyThenRecycle }, RecycleOrStudyName, v => settings.values.recycleOrStudy = v, SettingNames.RecycleOrStudy);
        Explain(ls, s.values.recycleOrStudy == RecycleOrStudy.StudyThenRecycle
            ? "LootScavenging_StudyThenRecycleExplain".Translate()
            : "LootScavenging_OneOrTheOtherExplain".Translate());

        DrawStudyPoints(ls);

        bool valueLocked = IsLocked(SettingNames.ScrapMarketValue);
        SetDisabled(valueLocked);
        float value = ls.SliderLabeled("LootScavenging_ScrapValue".Translate(s.values.scrapMarketValue.ToString("0.##")), s.values.scrapMarketValue, 0.01f, 10f);
        SetDisabled(false);
        if (!valueLocked)
            s.values.scrapMarketValue = Mathf.Max(0.01f, (float)Math.Round(value, 2));
        LockedNote(ls, valueLocked);
        Explain(ls, s.values.scrapMarketValue < 0.5f
            ? "LootScavenging_ScrapValueLow".Translate()
            : "LootScavenging_ScrapValueHigh".Translate(s.values.scrapMarketValue.ToString("0.##")));

        Choice(ls, "LootScavenging_DeathDrop".Translate(), s.values.deathDrop,
            new[] { DeathDrop.Ground, DeathDrop.ScrapOnBody, DeathDrop.AllOnBody }, DeathDropName, v => settings.values.deathDrop = v, SettingNames.DeathDrop);
        Explain(ls, DeathDropSentence(s.values.deathDrop));

        Choice(ls, "LootScavenging_EraFallback".Translate(), s.values.eraFallback,
            new[] { EraFallback.CarrierFaction, EraFallback.ColonyEra, EraFallback.Components }, EraFallbackName, v => settings.values.eraFallback = v, SettingNames.EraFallback);
        Explain(ls, EraFallbackSentence(s.values.eraFallback));

        Choice(ls, "LootScavenging_DisassemblyYield".Translate(), s.values.rules.disassemblyYield,
            new[] { DisassemblyYield.EraTypical, DisassemblyYield.ItemBased }, DisassemblyYieldName, v => settings.values.rules.disassemblyYield = v, SettingNames.DisassemblyYield);
        Explain(ls, s.values.rules.disassemblyYield == DisassemblyYield.ItemBased
            ? "LootScavenging_YieldItemExplain".Translate()
            : "LootScavenging_YieldEraExplain".Translate());

        Toggle(ls, "LootScavenging_TechprintProjects".Translate(), ref s.values.studyTechprintProjects,
            "LootScavenging_TechprintProjectsOn".Translate(), "LootScavenging_TechprintProjectsOff".Translate(), SettingNames.StudyTechprintProjects);

        Choice(ls, "LootScavenging_UnfamiliarScope".Translate(), s.values.unfamiliarScope,
            new[] { UnfamiliarScope.AllOutOfReach, UnfamiliarScope.LootedOnly }, UnfamiliarScopeName, v => settings.values.unfamiliarScope = v, SettingNames.UnfamiliarScope);
        // Whole-sentence keys, with and without the "only used" note, so a translation controls the separator.
        bool looted = s.values.unfamiliarScope == UnfamiliarScope.LootedOnly;
        bool unused = s.values.rules.dropMode != DropMode.KeepOriginal;
        Explain(ls, (looted
            ? (unused ? "LootScavenging_UnfamiliarLootedExplainUnused" : "LootScavenging_UnfamiliarLootedExplain")
            : (unused ? "LootScavenging_UnfamiliarAllExplainUnused" : "LootScavenging_UnfamiliarAllExplain")).Translate());
    }

    // One multiplier for every tier, then each tier's own points, typed in directly.
    private void DrawStudyPoints(Listing_Standard ls)
    {
        SettingValues v = s.values;
        v.studyPointsMultiplier = Percent(ls, "LootScavenging_StudyPointsMultiplier".Translate(v.studyPointsMultiplier.ToStringPercent()),
            v.studyPointsMultiplier, 0f, SettingValues.MaxStudyPointsMultiplier, SettingNames.StudyPointsMultiplier);
        Explain(ls, v.studyPointsMultiplier > 0f
            ? "LootScavenging_StudyPointsMultiplierExplain".Translate(v.studyPointsMultiplier.ToStringPercent(),
                v.StudyPointsPerUnit(ScrapTier.Industrial).ToString("0.#"))
            : "LootScavenging_StudyPointsMultiplierOff".Translate());

        ls.Label("LootScavenging_StudyPointsPerTier".Translate());
        Indent(ls);
        for (int i = 0; i < StudyTiers.Length; i++)
        {
            ScrapTier tier = StudyTiers[i];
            string lockName = SettingNames.StudyPoints(tier);
            bool locked = IsLocked(lockName);
            float points = v.StudyPoints(tier);
            ThingDef scrap = ScrapDefs.For(tier);
            SetDisabled(locked);
            ls.TextFieldNumericLabeled(scrap != null ? scrap.LabelCap.Resolve() : tier.ToString(), ref points, ref pointBuffers[i], 0f, SettingValues.MaxStudyPoints);
            SetDisabled(false);
            if (!locked)
                v.SetStudyPoints(tier, points);
            LockedNote(ls, locked);
        }
        Outdent(ls);
        Explain(ls, "LootScavenging_StudyPointsPerTierExplain".Translate());
        if (s.studyPointsHalvedNote)
            Explain(ls, "LootScavenging_StudyPointsHalvedNote".Translate());
    }

    private static string DeathDropName(DeathDrop drop)
    {
        switch (drop)
        {
            case DeathDrop.ScrapOnBody: return "LootScavenging_DeathDropScrapOnBody".Translate();
            case DeathDrop.AllOnBody: return "LootScavenging_DeathDropAllOnBody".Translate();
            default: return "LootScavenging_DeathDropGround".Translate();
        }
    }

    private static string DeathDropSentence(DeathDrop drop)
    {
        switch (drop)
        {
            case DeathDrop.ScrapOnBody: return "LootScavenging_DeathDropScrapOnBodyExplain".Translate();
            case DeathDrop.AllOnBody: return "LootScavenging_DeathDropAllOnBodyExplain".Translate();
            default: return "LootScavenging_DeathDropGroundExplain".Translate();
        }
    }

    private static string DropModeName(DropMode mode)
    {
        switch (mode)
        {
            case DropMode.ScrapEverything: return "LootScavenging_DropModeScrapEverything".Translate();
            case DropMode.KeepOriginal: return "LootScavenging_DropModeKeepOriginal".Translate();
            case DropMode.Off: return "LootScavenging_DropModeOff".Translate();
            default: return "LootScavenging_DropModeScrapOutOfReach".Translate();
        }
    }

    private static string DropModeSentence(DropMode mode)
    {
        switch (mode)
        {
            case DropMode.ScrapEverything: return "LootScavenging_DropModeScrapEverythingExplain".Translate();
            case DropMode.KeepOriginal: return "LootScavenging_DropModeKeepOriginalExplain".Translate();
            case DropMode.Off: return "LootScavenging_DropModeOffExplain".Translate();
            default: return "LootScavenging_DropModeScrapOutOfReachExplain".Translate();
        }
    }

    private static string StudyErasName(StudyEraLimit limit)
    {
        switch (limit)
        {
            case StudyEraLimit.AnyEra: return "LootScavenging_StudyErasAny".Translate();
            default: return "LootScavenging_StudyErasColony".Translate();
        }
    }

    private static string StudyErasSentence(StudyEraLimit limit)
    {
        switch (limit)
        {
            case StudyEraLimit.AnyEra: return "LootScavenging_StudyErasAnyExplain".Translate();
            default: return "LootScavenging_StudyErasColonyExplain".Translate();
        }
    }

    private static string RecycleOrStudyName(RecycleOrStudy mode)
    {
        return mode == RecycleOrStudy.StudyThenRecycle
            ? "LootScavenging_StudyThenRecycle".Translate()
            : "LootScavenging_OneOrTheOther".Translate();
    }

    private static string EraFallbackName(EraFallback fallback)
    {
        switch (fallback)
        {
            case EraFallback.ColonyEra: return "LootScavenging_EraFallbackColony".Translate();
            case EraFallback.Components: return "LootScavenging_EraFallbackComponents".Translate();
            default: return "LootScavenging_EraFallbackCarrier".Translate();
        }
    }

    private static string EraFallbackSentence(EraFallback fallback)
    {
        switch (fallback)
        {
            case EraFallback.ColonyEra: return "LootScavenging_EraFallbackColonyExplain".Translate();
            case EraFallback.Components: return "LootScavenging_EraFallbackComponentsExplain".Translate();
            default: return "LootScavenging_EraFallbackCarrierExplain".Translate();
        }
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
        if (c.humanlike || c.animal || c.mechanoid)
        {
            c.chance = Percent(ls, "LootScavenging_CorpseChance".Translate(c.chance.ToStringPercent()), c.chance, 0f, 1f, SettingNames.CorpseChance);
            Explain(ls, "LootScavenging_CorpseChanceExplain".Translate(c.chance.ToStringPercent()));
        }
        Toggle(ls, "LootScavenging_ButcherApparel".Translate(), ref c.butcherRemovesApparel,
            "LootScavenging_ButcherApparelOn".Translate(), "LootScavenging_ButcherApparelOff".Translate(), SettingNames.ButcherRemovesApparel);
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
        ls.CheckboxLabeled(label, ref value);
        SetDisabled(false);
        Explain(ls, value ? onSentence : offSentence);
        LockedNote(ls, locked);
    }

    private static void Choice<T>(Listing_Standard ls, string label, T current, T[] options, Func<T, string> name, Action<T> set, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        SetDisabled(locked);
        bool clicked = ls.ButtonTextLabeledPct(label, name(current), 0.6f);
        SetDisabled(false);
        LockedNote(ls, locked);
        if (locked || !clicked)
            return;
        var menu = new List<FloatMenuOption>();
        foreach (T option in options)
        {
            T picked = option;
            menu.Add(new FloatMenuOption(name(picked), () => set(picked)));
        }
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    // Rounded to whole percent, so a slider set back by hand can match a preset again.
    private static float Percent(Listing_Standard ls, string label, float value, float min, float max, string lockName = null)
    {
        bool locked = IsLocked(lockName);
        SetDisabled(locked);
        float picked = (float)Math.Round(ls.SliderLabeled(label, value, min, max), 2);
        SetDisabled(false);
        LockedNote(ls, locked);
        return locked ? value : picked;
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

using System;
using System.Collections.Generic;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Makes study bench work with the usual bill controls. In the game's implied-def pass (as ticket 04's recycling recipes):
/// research benches become <see cref="Building_StudyBench"/> with a bills tab, the study work giver is pointed at them,
/// and one study RecipeDef per scrap tier is generated and attached through its own recipeUsers.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
internal static class StudyRecipes
{
    public const string DefNamePrefix = "LS_Study_"; // not translated
    // The ×10 bills (ticket 19), e.g. LS_Study_Bulk_Spacer.
    public const string BulkDefNamePrefix = "LS_Study_Bulk_"; // not translated
    public const string WorkGiverDefName = "LS_DoBillsStudy"; // not translated

    private static readonly Dictionary<RecipeDef, ScrapTier> tierOf = new Dictionary<RecipeDef, ScrapTier>();
    private static readonly HashSet<RecipeDef> bulk = new HashSet<RecipeDef>();

    /// <summary>The scrap tier a study recipe, ×1 or ×10, is for; false for every other recipe.</summary>
    public static bool TryGetTier(RecipeDef recipe, out ScrapTier tier)
    {
        if (recipe != null && tierOf.TryGetValue(recipe, out tier))
            return true;
        tier = default;
        return false;
    }

    /// <summary>How many ×1 batches one iteration of the recipe studies: 10 for a ×10 bill, otherwise 1.</summary>
    public static int Factor(RecipeDef recipe) => recipe != null && bulk.Contains(recipe) ? BulkBills.Factor : 1;

    private static void Postfix(bool hotReload)
    {
        try
        {
            List<ThingDef> benches = ConvertBenches();
            WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(WorkGiverDefName);
            if (giver != null)
            {
                giver.fixedBillGiverDefs ??= new List<ThingDef>();
                foreach (ThingDef bench in benches)
                {
                    if (!giver.fixedBillGiverDefs.Contains(bench))
                        giver.fixedBillGiverDefs.Add(bench);
                }
            }
            if (benches.Count == 0)
                return;

            foreach (ScrapTier tier in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
            {
                foreach (bool isBulk in new[] { false, true })
                {
                    string defName = (isBulk ? BulkDefNamePrefix : DefNamePrefix) + tier;
                    RecipeDef existing = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
                    if (existing != null)
                    {
                        Register(existing, tier, isBulk);
                        continue;
                    }
                    RecipeDef recipe = Build(tier, defName, benches, isBulk);
                    if (recipe == null)
                        continue;
                    DefGenerator.AddImpliedDef(recipe, hotReload);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Generating study bills failed: {e}"); // not translated
        }
    }

    /// <summary>
    /// Rewrites each study bill's description and nominal work from the current study points settings. The settings load
    /// before the defs and change on the settings page, so this runs again whenever they may have changed.
    /// </summary>
    public static void ApplyPoints()
    {
        foreach (KeyValuePair<RecipeDef, ScrapTier> entry in tierOf)
        {
            ThingDef scrap = ScrapFor(entry.Value);
            if (scrap != null)
                ApplyPoints(entry.Key, entry.Value, scrap);
        }
    }

    private static void ApplyPoints(RecipeDef recipe, ScrapTier tier, ThingDef scrap)
    {
        float pointsPerUnit = ScrapStudy.PointsPerUnit(tier);
        int factor = Factor(recipe);
        int batch = StudyRules.BatchUnits(pointsPerUnit) * factor;
        string description = "LootScavenging_StudyDescription".Translate(scrap.label, batch.ToString(), pointsPerUnit.ToString("0.#"));
        if (factor > 1)
            description += " " + "LootScavenging_BulkBillNote".Translate(BulkBills.MinUnits.ToString());
        recipe.description = description;
        // Nominal, one unit's worth per ×1 batch: the real work is set per iteration from the points it pays (BillWorkPatch).
        recipe.workAmount = Math.Max(1f, pointsPerUnit) * StudyRules.TicksPerPoint * factor;
    }

    private static void Register(RecipeDef recipe, ScrapTier tier, bool isBulk)
    {
        tierOf[recipe] = tier;
        if (isBulk)
            bulk.Add(recipe);
    }

    private static ThingDef ScrapFor(ScrapTier tier) =>
        ScrapTierTable.For(tier)?.scrap ?? DefDatabase<ThingDef>.GetNamedSilentFail(ScrapTiers.DefName(tier));

    // Defs whose class is exactly the vanilla research bench. Subclasses from other mods are left alone.
    private static List<ThingDef> ConvertBenches()
    {
        var benches = new List<ThingDef>();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.thingClass == typeof(Building_ResearchBench))
                def.thingClass = typeof(Building_StudyBench);
            if (def.thingClass != typeof(Building_StudyBench))
                continue;
            def.inspectorTabs ??= new List<Type>();
            if (!def.inspectorTabs.Contains(typeof(ITab_StudyBills)))
                def.inspectorTabs.Add(typeof(ITab_StudyBills));
            benches.Add(def);
        }
        return benches;
    }

    private static RecipeDef Build(ScrapTier tier, string defName, List<ThingDef> benches, bool isBulk)
    {
        // Built even when the tier's points are zero, so raising them in the settings needs no restart.
        ThingDef scrap = ScrapFor(tier);
        if (scrap == null)
            return null;

        // The ×10 bill names ten units, so it waits until ten are on hand and a smaller pile is left to the ×1 bill; the cap
        // patch then lets it take up to ten ×1 batches (BillIngredientCapPatch).
        RecipeDef recipe = BillRecipes.New(defName, new List<ThingDef>(benches), ScrapStudy.IngredientDefs(scrap),
            isBulk ? BulkBills.MinUnits : 1f, allowedByDefault: true);
        recipe.label = (isBulk ? "LootScavenging_StudyBulkLabel" : "LootScavenging_StudyLabel").Translate(scrap.label);
        recipe.jobString = "LootScavenging_StudyJob".Translate(scrap.label);
        Register(recipe, tier, isBulk);
        ApplyPoints(recipe, tier, scrap);
        // The ×1 bill takes a whole stack of any size, so a lone unit is studied too; an iteration uses at most a batch of it.
        recipe.ignoreIngredientCountTakeEntireStacks = !isBulk;
        recipe.workerClass = typeof(RecipeWorker_Study);
        recipe.workerCounterClass = typeof(RecipeWorkerCounter_Study);
        BillRecipes.LikeResearch(recipe);
        return recipe;
    }
}

/// <summary>
/// Pays an iteration's points through <see cref="ResearchManager.AddProgress"/> when the bill finishes. The ingredients
/// are not destroyed by the game's consume step; this uses up only the units the project still needs and leaves the rest
/// at the bench. In "study first, then recycle" each studied unit becomes a unit of studied scrap.
/// </summary>
public class RecipeWorker_Study : RecipeWorker
{
    public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
    {
        // Handled in Notify_IterationCompleted, which runs right after and knows the researcher.
    }

    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
    {
        Map map = billDoer.Map;
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        bool paid = false;
        string firstLabel = null;
        TaggedString whyNot = default;
        foreach (Thing ingredient in ingredients)
        {
            if (ingredient == null || ingredient.Destroyed)
                continue;
            if (TryPlan(ingredient, project, out StudyWorth worth, out StudyPlan plan))
            {
                Study(billDoer, ingredient, worth, project, plan);
                paid = true;
            }
            else if (firstLabel == null)
            {
                firstLabel = ingredient.LabelNoCount;
                whyNot = WhyNot(ingredient, project);
            }
            if (!ingredient.Destroyed && !ingredient.Spawned && map != null)
                GenPlace.TryPlaceThing(ingredient, billDoer.Position, map, ThingPlaceMode.Near);
        }
        if (!paid && firstLabel != null)
        {
            Messages.Message("LootScavenging_StudyNotPaid".Translate(billDoer.Named("PAWN"), firstLabel, whyNot), new LookTargets(billDoer),
                MessageTypeDefOf.RejectInput, historical: false);
        }
    }

    /// <summary>
    /// Whether an iteration with these ingredients pays anything now, read at the finish when the project may have changed. When
    /// it does not, <see cref="StudyIterationPatch"/> keeps the bill's "Do X times" count from being spent.
    /// </summary>
    internal static bool WouldPay(List<Thing> ingredients)
    {
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        foreach (Thing ingredient in ingredients)
        {
            if (ingredient != null && !ingredient.Destroyed && TryPlan(ingredient, project, out _, out _))
                return true;
        }
        return false;
    }

    // What this ingredient studies for the project now. The work start already trimmed the ingredient to the batch and the
    // stock allowance (BillWorkPatch), so only the project's remaining cost can lower it now.
    private static bool TryPlan(Thing ingredient, ResearchProjectDef project, out StudyWorth worth, out StudyPlan plan)
    {
        plan = default;
        worth = default;
        if (project == null || !ScrapStudy.TryGetWorth(ingredient, out worth)
            || ScrapStudy.Verdict(worth, project) != StudyVerdict.Eligible)
            return false;
        plan = ScrapStudy.Plan(worth, ingredient.stackCount, ingredient.stackCount, project);
        return plan.Things > 0 && plan.Points > 0f;
    }

    private static TaggedString WhyNot(Thing ingredient, ResearchProjectDef project)
    {
        if (!ScrapStudy.TryGetWorth(ingredient, out StudyWorth worth))
            return "LootScavenging_StudyWhyNotWorth".Translate();
        StudyVerdict verdict = ScrapStudy.Verdict(worth, project);
        if (verdict != StudyVerdict.Eligible)
            return CompStudyInfo.Reason(verdict, worth, project);
        return "LootScavenging_StudyWhyTooLittle".Translate(project.LabelCap);
    }

    private static void Study(Pawn pawn, Thing ingredient, StudyWorth worth, ResearchProjectDef project, StudyPlan plan)
    {
        int used = plan.Things;
        float points = plan.Points;
        string label = ingredient.LabelNoCount;
        if (used >= ingredient.stackCount)
            ingredient.Destroy();
        else
            ingredient.SplitOff(used).Destroy();

        string projectLabel = project.LabelCap;
        // As bench research does: the pawn's record counts what the research tab counts.
        pawn.records?.AddTo(RecordDefOf.ResearchPointsResearched, points);
        Find.ResearchManager.AddProgress(project, points, pawn);
        Messages.Message("LootScavenging_StudyDone".Translate(pawn.Named("PAWN"), used.ToString(), label, points.ToString("0.#"),
            projectLabel), new LookTargets(pawn), MessageTypeDefOf.PositiveEvent, historical: false);
        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {pawn} studied {used} {label}: +{points} to {project.defName}"); // not translated

        int studied = StudyRules.StudiedScrapUnits(used * worth.UnitsPerThing, LootScavengingMod.Settings.values.recycleOrStudy);
        ThingDef studiedDef = ScrapTierTable.For(worth.Tier)?.studiedScrap;
        if (studied > 0 && studiedDef != null && pawn.Map != null)
        {
            foreach (int count in ScrapAmounts.SplitIntoStacks(studied, studiedDef.stackLimit))
            {
                Thing remains = ThingMaker.MakeThing(studiedDef);
                remains.stackCount = count;
                GenPlace.TryPlaceThing(remains, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }
        }
    }
}

/// <summary>
/// "Do until you have X" on a study bill keeps X units of that tier's scrap in stock: the bill studies only while a whole
/// batch can be used without dropping below X (see <see cref="StudyBillPatch"/>). The count shown is the scrap on the map.
/// The bill's pause-and-resume thresholds are not used.
/// </summary>
public class RecipeWorkerCounter_Study : RecipeWorkerCounter
{
    public override bool CanCountProducts(Bill_Production bill) => StudyRecipes.TryGetTier(recipe, out _);

    public override int CountProducts(Bill_Production bill)
    {
        if (!StudyRecipes.TryGetTier(recipe, out ScrapTier tier) || bill.Map == null)
            return 0;
        int count = 0;
        ThingDef scrap = Loot.ScrapDefs.For(tier);
        ISlotGroup group = bill.GetIncludeSlotGroup();
        if (group != null)
        {
            // "Count in" a stockpile: only what lies there, as the game's counter does.
            foreach (Thing held in group.HeldThings)
            {
                if (CountValidThing(held, bill, scrap))
                    count += held.stackCount;
            }
            return count;
        }
        List<Thing> things = bill.Map.listerThings.ThingsOfDef(scrap);
        for (int i = 0; i < things.Count; i++)
        {
            if (CountValidThing(things[i], bill, scrap))
                count += things[i].stackCount;
        }
        return count;
    }

    // The base reads recipe.products[0]; a study recipe has no products, and its output does not go to a stockpile.
    public override bool CanPossiblyStore(Bill_Production bill, ISlotGroup slotGroup) => true;

    public override string ProductsDescription(Bill_Production bill)
    {
        return StudyRecipes.TryGetTier(recipe, out ScrapTier tier)
            ? "LootScavenging_StudyKeepInStock".Translate(Loot.ScrapDefs.For(tier).label).Resolve()
            : null;
    }
}

/// <summary>
/// A study bill starts only when its scrap can be studied for the current project at that bench: no project, a project
/// above the scrap's era, the techprint setting and a zero research factor all stop it. The scrap's inspect text says why.
/// </summary>
[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.ShouldDoNow))]
internal static class StudyBillPatch
{
    // ShouldDoNow runs for each bill on every scan and every frame the bill list is drawn; the verdict reads every
    // research project for the colony's era, so it is kept for the current tick.
    private static int cachedTick = -1;
    private static ResearchProjectDef cachedProject;
    private static readonly Dictionary<ScrapTier, StudyVerdict> cached = new Dictionary<ScrapTier, StudyVerdict>();

    private static void Postfix(Bill_Production __instance, ref bool __result)
    {
        try
        {
            if (OriginalItemRecipes.IsStudy(__instance.recipe) && !__instance.suspended)
            {
                // Original items: the bill waits when there is no project or this bench cannot research it. Whether an item
                // can be studied is decided per item when the bill looks for ingredients.
                ResearchProjectDef current = ScrapStudy.CurrentProject;
                if (current == null || (__instance.billStack?.billGiver is Building_ResearchBench originalBench
                    && !current.CanBeResearchedAt(originalBench, ignoreResearchBenchPowerStatus: false)))
                    __result = false;
                return;
            }
            if (!StudyRecipes.TryGetTier(__instance.recipe, out ScrapTier tier) || __instance.suspended)
                return;

            if (!CanStudyAt(__instance, tier))
            {
                __result = false;
                return;
            }
            if (__instance.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                // The vanilla pause test treats reaching X as done; for study, being above X is the working state.
                __instance.paused = false;
                int inStock = __instance.recipe.WorkerCounter.CountProducts(__instance);
                __result = StudyRules.KeepsStudying(inStock, __instance.targetCount);
            }
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"[Loot Scavenging] Checking a study bill failed: {e}", 0x4C5305); // not translated
        }
    }

    private static bool CanStudyAt(Bill_Production bill, ScrapTier tier)
    {
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        if (project == null)
            return false;
        if (bill.billStack?.billGiver is Building_ResearchBench bench
            && !project.CanBeResearchedAt(bench, ignoreResearchBenchPowerStatus: false))
            return false;
        return VerdictFor(tier, project) == StudyVerdict.Eligible;
    }

    public static StudyVerdict VerdictFor(ScrapTier tier, ResearchProjectDef project)
    {
        int tick = Find.TickManager?.TicksGame ?? 0;
        if (tick != cachedTick || project != cachedProject)
        {
            cached.Clear();
            cachedTick = tick;
            cachedProject = project;
        }
        if (!cached.TryGetValue(tier, out StudyVerdict verdict))
        {
            verdict = ScrapStudy.TryGetWorth(Loot.ScrapDefs.For(tier), out StudyWorth worth)
                ? ScrapStudy.Verdict(worth, project)
                : StudyVerdict.NoStudyPoints;
            cached[tier] = verdict;
        }
        return verdict;
    }
}

/// <summary>
/// Keeps a study bill's "Do X times" count when the iteration pays nothing (the project changed during the job): the game
/// spends the count before the recipe worker runs, so for that case the worker is called alone.
/// </summary>
[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Notify_IterationCompleted))]
internal static class StudyIterationPatch
{
    private static bool Prefix(Bill_Production __instance, Pawn billDoer, List<Thing> ingredients)
    {
        try
        {
            RecipeDef recipe = __instance.recipe;
            if ((!StudyRecipes.TryGetTier(recipe, out _) && !OriginalItemRecipes.IsStudy(recipe)) || ingredients == null
                || RecipeWorker_Study.WouldPay(ingredients))
                return true;
            recipe.Worker.Notify_IterationCompleted(billDoer, ingredients);
            return false;
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"[Loot Scavenging] Checking a finished study bill failed: {e}", 0x4C530A); // not translated
            return true;
        }
    }
}

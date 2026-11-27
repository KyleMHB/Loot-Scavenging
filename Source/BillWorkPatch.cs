using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging;

/// <summary>
/// Sets each iteration's work for the mod's bills once the worker starts, from what the iteration actually takes. Scrap bills
/// take a whole stack of any size (<c>ignoreIngredientCountTakeEntireStacks</c>); <see cref="BillIngredientCapPatch"/> caps
/// the haul to what one iteration uses, and the placed count is trimmed again here in case the project needs less by now (the
/// game splits a placed stack down to its count only at the finish). Recycling works per unit used; study
/// works for the points the iteration pays (spec: "Study time scales with the points paid"). The bills' own work amounts are
/// nominal values for the bill list.
/// </summary>
[HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.DoRecipeWork))]
internal static class BillWorkPatch
{
    private static void Postfix(Toil __result)
    {
        Toil toil = __result;
        Action original = toil.initAction;
        toil.initAction = delegate
        {
            original();
            try
            {
                Rescale(toil.actor);
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"[Loot Scavenging] Setting the work for a bill failed: {e}", 0x4C5308); // not translated
            }
        };

        // The game's progress bar divides by the recipe's nominal work. Its getter runs inside the toil's pre-tick actions, so
        // each is wrapped to say whose bar is being drawn (see BillBarTotalPatch).
        if (toil.preTickIntervalActions == null)
            return;
        for (int i = 0; i < toil.preTickIntervalActions.Count; i++)
        {
            Action<int> inner = toil.preTickIntervalActions[i];
            toil.preTickIntervalActions[i] = delegate(int delta)
            {
                BillBarTotalPatch.Drawing = toil.actor;
                try
                {
                    inner(delta);
                }
                finally
                {
                    BillBarTotalPatch.Drawing = null;
                }
            };
        }
    }

    // Sets the work left and remembers it as the bar's total.
    private static void SetWork(JobDriver_DoBill driver, float work)
    {
        driver.workLeft = work;
        BillBarTotalPatch.Totals.Remove(driver);
        BillBarTotalPatch.Totals.Add(driver, new BillBarTotalPatch.Total { Work = work });
    }

    private static void Rescale(Pawn actor)
    {
        Job job = actor?.jobs?.curJob;
        RecipeDef recipe = job?.RecipeDef;
        if (recipe == null || !(actor.jobs.curDriver is JobDriver_DoBill driver) || job.placedThings == null || job.placedThings.Count == 0)
            return;
        Thing first = job.placedThings[0].thing;
        if (first == null)
            return;

        if (RecycleRecipes.TryGetTier(recipe, out ScrapTierDef tierDef))
        {
            int units = Trim(job.placedThings, RecycleBatch.Units(Placed(job.placedThings), tierDef.batchSize * Factor(recipe)));
            SetWork(driver, RecycleBatch.Work(units, tierDef.workPerUnit));
        }
        else if (StudyRecipes.TryGetTier(recipe, out _))
        {
            // The ingredient choice already capped the count (BillIngredientCapPatch); this only lowers it further when
            // the project's remaining cost shrank since.
            if (!TryPlanStudy(job.bill, first, Placed(job.placedThings), out StudyPlan plan))
                return;
            Trim(job.placedThings, Math.Max(1, plan.Things));
            SetWork(driver, plan.Work);
        }
        else if (OriginalItemRecipes.IsRecycle(recipe))
        {
            if (ScrapEquivalent.TryGet(first, out ScrapTier tier, out int units))
                SetWork(driver, ScrapEquivalent.RecycleWork(tier, units));
        }
        else if (OriginalItemRecipes.IsStudy(recipe))
        {
            if (ScrapStudy.TryGetWorth(first, out StudyWorth worth))
                SetWork(driver, ScrapStudy.Plan(worth, 1, 1, ScrapStudy.CurrentProject).Work);
        }
    }

    /// <summary>
    /// The most units of <paramref name="thing"/> one iteration of a scrap bill uses, or false for any other bill: the
    /// recycling batch, or for study the batch, the stock above "do until you have X" and what the project still needs.
    /// </summary>
    internal static bool TryCap(Bill bill, Thing thing, int available, out int units)
    {
        units = 0;
        if (RecycleRecipes.TryGetTier(bill.recipe, out ScrapTierDef tierDef))
        {
            units = RecycleBatch.Units(available, tierDef.batchSize * Factor(bill.recipe));
            return true;
        }
        if (StudyRecipes.TryGetTier(bill.recipe, out _) && TryPlanStudy(bill, thing, available, out StudyPlan plan))
        {
            units = Math.Max(1, plan.Things);
            return true;
        }
        return false;
    }

    // The stock is counted wherever the scrap lies, in storage at the choice or at the bench at work start, and both count.
    private static bool TryPlanStudy(Bill bill, Thing thing, int available, out StudyPlan plan)
    {
        plan = default;
        if (!ScrapStudy.TryGetWorth(thing, out StudyWorth worth))
            return false;
        int max = StudyRules.BatchUnits(worth.PointsPerUnit) * Factor(bill.recipe);
        if (bill is Bill_Production production && production.repeatMode == BillRepeatModeDefOf.TargetCount)
            max = Math.Min(max, StudyRules.StockAllowance(bill.recipe.WorkerCounter.CountProducts(production), production.targetCount));
        plan = ScrapStudy.Plan(worth, available, max, ScrapStudy.CurrentProject);
        return true;
    }

    /// <summary>How many ×1 batches one iteration of a scrap bill uses: 10 for a ×10 bill (ticket 19), otherwise 1.</summary>
    internal static int Factor(RecipeDef recipe) => Math.Max(RecycleRecipes.Factor(recipe), StudyRecipes.Factor(recipe));

    private static int Placed(List<ThingCountClass> placed)
    {
        int total = 0;
        foreach (ThingCountClass p in placed)
            total += p.Count;
        return total;
    }

    // Lowers the placed counts to at most <paramref name="units"/>, dropping entries left at zero (a zero count is an error at
    // the finish). Returns the units kept.
    private static int Trim(List<ThingCountClass> placed, int units)
    {
        int left = units;
        for (int i = 0; i < placed.Count; i++)
        {
            if (left <= 0)
            {
                placed.RemoveAt(i--);
                continue;
            }
            if (placed[i].Count > left)
                placed[i].Count = left;
            left -= placed[i].Count;
        }
        return units - Math.Max(0, left);
    }
}

/// <summary>
/// Caps how much of a stack a scrap bill takes, when the ingredients are chosen. The bills take a whole stack of any size
/// (so a lone unit is used too), but an iteration uses at most a batch. Hauling the whole pile would leave the rest on the
/// bench, and the bill giver clears its ingredient cells before each job (<c>WorkGiverUtility.HaulStuffOffBillGiverJob</c>),
/// so the pile would be carried off and back every iteration. Only the units the iteration uses are hauled.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
internal static class BillIngredientCapPatch
{
    private static void Postfix(Bill bill, List<ThingCount> chosen, bool __result)
    {
        if (!__result || chosen == null || chosen.Count == 0 || bill?.recipe == null)
            return;
        try
        {
            if (BillWorkPatch.Factor(bill.recipe) > 1)
            {
                RaiseBulk(bill, chosen);
                return;
            }
            for (int i = 0; i < chosen.Count; i++)
            {
                ThingCount pick = chosen[i];
                if (pick.Thing != null && BillWorkPatch.TryCap(bill, pick.Thing, pick.Count, out int units) && units < pick.Count)
                    chosen[i] = pick.WithCount(units);
            }
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"[Loot Scavenging] Capping a scrap bill's ingredients failed: {e}", 0x4C5309); // not translated
        }
    }

    // A ×10 bill names ten units, so the game picks just ten, from as many stacks as it takes. The picks are raised to what
    // ten ×1 batches use (or what the stock allowance and the project allow), taken from the chosen stacks in order; a pick
    // the budget no longer reaches is dropped.
    private static void RaiseBulk(Bill bill, List<ThingCount> chosen)
    {
        var stacks = new int[chosen.Count];
        int total = 0;
        for (int i = 0; i < chosen.Count; i++)
        {
            stacks[i] = chosen[i].Thing?.stackCount ?? 0;
            total += stacks[i];
        }
        if (chosen[0].Thing == null || !BillWorkPatch.TryCap(bill, chosen[0].Thing, total, out int budget))
            return;
        int[] counts = BulkBills.Spread(stacks, budget);
        for (int i = chosen.Count - 1; i >= 0; i--)
        {
            if (counts[i] <= 0)
                chosen.RemoveAt(i);
            else
                chosen[i] = chosen[i].WithCount(counts[i]);
        }
    }
}

/// <summary>
/// Makes the work bar of the mod's bills total what the job really takes. The game's bar is
/// <c>1 - workLeft / recipe.WorkAmountTotal</c>, and the recipe holds only a nominal amount, so a bill whose real work differs
/// started part-filled or overflowed. While a wrapped pre-tick action of the bill's toil runs (<see cref="BillWorkPatch"/>),
/// the total is read from what the work start set.
/// </summary>
[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.WorkAmountTotal))]
internal static class BillBarTotalPatch
{
    internal sealed class Total
    {
        public float Work;
    }

    internal static readonly ConditionalWeakTable<JobDriver_DoBill, Total> Totals = new ConditionalWeakTable<JobDriver_DoBill, Total>();

    /// <summary>The pawn whose toil is running its pre-tick actions, or null.</summary>
    [ThreadStatic]
    internal static Pawn Drawing;

    private static void Postfix(RecipeDef __instance, ref float __result)
    {
        Pawn actor = Drawing;
        if (actor?.jobs?.curDriver is JobDriver_DoBill driver && actor.jobs.curJob?.RecipeDef == __instance
            && Totals.TryGetValue(driver, out Total total) && total.Work > 0f)
            __result = total.Work;
    }
}

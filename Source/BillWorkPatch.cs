using System;
using System.Collections.Generic;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging;

/// <summary>
/// Sets each iteration's work for the mod's bills once the worker starts, from what the iteration actually takes. Scrap bills
/// take a whole stack of any size (<c>ignoreIngredientCountTakeEntireStacks</c>), so the placed count is first trimmed to the
/// batch, and for study to the stock above "do until you have X" and to what the project still needs; the rest stays at the
/// bench, because the game splits a placed stack down to its count only at the finish. Recycling works per unit used; study
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
            int units = Trim(job.placedThings, RecycleBatch.Units(Placed(job.placedThings), tierDef.batchSize));
            driver.workLeft = RecycleBatch.Work(units, tierDef.workPerUnit);
        }
        else if (StudyRecipes.TryGetTier(recipe, out _))
        {
            if (!ScrapStudy.TryGetWorth(first, out StudyWorth worth))
                return;
            int max = StudyRules.BatchUnits(worth.PointsPerUnit);
            if (job.bill is Bill_Production bill && bill.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                // Counted now, while the whole stack still lies at the bench and counts as stock.
                max = Math.Min(max, StudyRules.StockAllowance(recipe.WorkerCounter.CountProducts(bill), bill.targetCount));
            }
            StudyPlan plan = ScrapStudy.Plan(worth, Placed(job.placedThings), max, ScrapStudy.CurrentProject);
            Trim(job.placedThings, Math.Max(1, plan.Things));
            driver.workLeft = plan.Work;
        }
        else if (OriginalItemRecipes.IsRecycle(recipe))
        {
            if (ScrapEquivalent.TryGet(first, out ScrapTier tier, out int units))
                driver.workLeft = ScrapEquivalent.RecycleWork(tier, units);
        }
        else if (OriginalItemRecipes.IsStudy(recipe))
        {
            if (ScrapStudy.TryGetWorth(first, out StudyWorth worth))
                driver.workLeft = ScrapStudy.Plan(worth, 1, 1, ScrapStudy.CurrentProject).Work;
        }
    }

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

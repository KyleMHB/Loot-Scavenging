using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging;

/// <summary>
/// Bills that recycle or study unfamiliar original items (spec, "Original-item mode"). Each is one recipe for all gear, taking
/// one item per iteration, whose ingredient filter starts with nothing ticked: the player ticks in the items they are willing
/// to use up, so nothing they want to keep is taken ("Never consume what the player wants to keep"). A bill then takes an
/// item only while it is unfamiliar, and, for recycling, only at a bench that recycles its tier; for study only while the
/// current project can take its tier. Yields and points come from <see cref="ScrapEquivalent"/>.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
internal static class OriginalItemRecipes
{
    public const string RecycleDefName = "LS_RecycleOriginal"; // not translated
    public const string StudyDefName = "LS_StudyOriginal"; // not translated

    private static RecipeDef recycleRecipe;
    private static RecipeDef studyRecipe;

    public static bool IsRecycle(RecipeDef recipe) => recipe != null && recipe == recycleRecipe;
    public static bool IsStudy(RecipeDef recipe) => recipe != null && recipe == studyRecipe;

    private static void Postfix(bool hotReload)
    {
        try
        {
            List<ThingDef> gear = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.category == ThingCategory.Item && (d.IsWeapon || d.IsApparel)).ToList();
            if (gear.Count == 0)
                return;

            recycleRecipe = Existing(RecycleDefName, hotReload) ?? AddRecycle(gear, hotReload);
            studyRecipe = Existing(StudyDefName, hotReload) ?? AddStudy(gear, hotReload);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Generating original-item bills failed: {e}"); // not translated
        }
    }

    private static RecipeDef Existing(string defName, bool hotReload) =>
        hotReload ? DefDatabase<RecipeDef>.GetNamedSilentFail(defName) : null;

    private static RecipeDef AddRecycle(List<ThingDef> gear, bool hotReload)
    {
        List<ThingDef> users = ScrapEquivalent.AllRecycleBenches();
        if (users.Count == 0)
            return null;

        RecipeDef recipe = NewRecipe(RecycleDefName, gear, users);
        recipe.label = "LootScavenging_RecycleOriginalLabel".Translate();
        recipe.jobString = "LootScavenging_RecycleOriginalJob".Translate();
        recipe.description = "LootScavenging_RecycleOriginalDescription".Translate();
        recipe.workAmount = 500f; // nominal: the real work is set per item from its units (OriginalWorkPatch)
        recipe.workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("SmeltingSpeed");
        recipe.effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Smelt");
        recipe.soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Smelt");
        recipe.workerClass = typeof(RecipeWorker_RecycleOriginal);
        DefGenerator.AddImpliedDef(recipe, hotReload);
        return recipe;
    }

    private static RecipeDef AddStudy(List<ThingDef> gear, bool hotReload)
    {
        // Same benches as the scrap study bills: the research benches, converted by StudyRecipes in this same pass.
        List<ThingDef> users = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(d => d.thingClass == typeof(Building_ResearchBench) || d.thingClass == typeof(Building_StudyBench)).ToList();
        if (users.Count == 0)
            return null;

        RecipeDef recipe = NewRecipe(StudyDefName, gear, users);
        recipe.label = "LootScavenging_StudyOriginalLabel".Translate();
        recipe.jobString = "LootScavenging_StudyOriginalJob".Translate();
        recipe.description = "LootScavenging_StudyOriginalDescription".Translate();
        recipe.workAmount = 3000f; // nominal, as above
        recipe.workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("ResearchSpeed");
        recipe.workSkill = DefDatabase<SkillDef>.GetNamedSilentFail("Intellectual");
        recipe.effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Research");
        recipe.requiredGiverWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Research");
        recipe.workerClass = typeof(RecipeWorker_Study);
        DefGenerator.AddImpliedDef(recipe, hotReload);
        return recipe;
    }

    // One item per iteration. The recipe may take any weapon or apparel def; the default filter allows none of them.
    private static RecipeDef NewRecipe(string defName, List<ThingDef> gear, List<ThingDef> users)
    {
        var recipe = new RecipeDef
        {
            defName = defName,
            recipeUsers = users,
            products = new List<ThingDefCountClass>(),
            ingredients = new List<IngredientCount>(),
            fixedIngredientFilter = new ThingFilter(),
            defaultIngredientFilter = new ThingFilter(),
        };
        var ingredient = new IngredientCount();
        ingredient.SetBaseCount(1f);
        foreach (ThingDef def in gear)
        {
            ingredient.filter.SetAllow(def, true);
            recipe.fixedIngredientFilter.SetAllow(def, true);
        }
        recipe.ingredients.Add(ingredient);
        return recipe;
    }

    /// <summary>The thing and its scrap equivalent that a bill of an original-item recipe may take; false for any other bill.</summary>
    public static bool Accepts(Bill bill, Thing thing)
    {
        RecipeDef recipe = bill.recipe;
        if (IsRecycle(recipe))
        {
            if (!ScrapEquivalent.TryGet(thing, out ScrapTier tier, out _))
                return false;
            return ScrapEquivalent.BenchRecycles((bill.billStack?.billGiver as Thing)?.def, tier);
        }
        if (IsStudy(recipe))
        {
            ResearchProjectDef project = ScrapStudy.CurrentProject;
            if (project == null || !ScrapEquivalent.TryGet(thing, out ScrapTier tier, out _))
                return false;
            return StudyBillPatch.VerdictFor(tier, project) == StudyVerdict.Eligible;
        }
        return true;
    }
}

/// <summary>
/// Recycles the original item the bill brought and puts what its scrap equivalent would return near the worker. The game's
/// own consume step is skipped so the item is only destroyed when it still counts as unfamiliar at the finish.
/// </summary>
public class RecipeWorker_RecycleOriginal : RecipeWorker
{
    public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
    {
        // Handled in Notify_IterationCompleted, which runs right after and knows where the worker stands.
    }

    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
    {
        Map map = billDoer.Map;
        foreach (Thing ingredient in ingredients)
        {
            if (ingredient == null || ingredient.Destroyed)
                continue;
            List<Thing> products = ScrapEquivalent.RecycleProducts(ingredient).ToList();
            if (products.Count > 0)
            {
                if (Prefs.DevMode)
                    Log.Message($"[Loot Scavenging] {billDoer} recycled {ingredient}: {string.Join(", ", products.Select(p => p.LabelCap))}"); // not translated
                ingredient.Destroy();
                foreach (Thing product in products)
                {
                    if (map != null)
                        GenPlace.TryPlaceThing(product, billDoer.Position, map, ThingPlaceMode.Near);
                    else
                        product.Destroy();
                }
            }
            else if (!ingredient.Spawned && map != null)
            {
                GenPlace.TryPlaceThing(ingredient, billDoer.Position, map, ThingPlaceMode.Near);
            }
        }
    }
}

/// <summary>A bill of an original-item recipe takes only the items its rules accept (see <see cref="OriginalItemRecipes.Accepts"/>).</summary>
[HarmonyPatch(typeof(Bill), nameof(Bill.IsFixedOrAllowedIngredient), new[] { typeof(Thing) })]
internal static class OriginalIngredientPatch
{
    private static void Postfix(Bill __instance, Thing thing, ref bool __result)
    {
        if (!__result)
            return;
        try
        {
            if (!OriginalItemRecipes.IsRecycle(__instance.recipe) && !OriginalItemRecipes.IsStudy(__instance.recipe))
                return;
            __result = OriginalItemRecipes.Accepts(__instance, thing);
        }
        catch (Exception e)
        {
            __result = false;
            Log.ErrorOnce($"[Loot Scavenging] Checking an original item for a bill failed: {e}", 0x4C5307); // not translated
        }
    }
}

/// <summary>
/// An original-item iteration takes one item, so its work is set from that item's units once the worker starts: the same work
/// the equivalent scrap would take. The bill's own work amount is only a nominal value for the bill list.
/// </summary>
[HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.DoRecipeWork))]
internal static class OriginalWorkPatch
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
                Log.ErrorOnce($"[Loot Scavenging] Setting the work for an original item failed: {e}", 0x4C5308); // not translated
            }
        };
    }

    private static void Rescale(Pawn actor)
    {
        Job job = actor?.jobs?.curJob;
        RecipeDef recipe = job?.RecipeDef;
        bool recycle = OriginalItemRecipes.IsRecycle(recipe);
        if (!recycle && !OriginalItemRecipes.IsStudy(recipe))
            return;
        if (!(actor.jobs.curDriver is JobDriver_DoBill driver) || job.placedThings == null || job.placedThings.Count == 0)
            return;
        if (!ScrapEquivalent.TryGet(job.placedThings[0].thing, out ScrapTier tier, out int units))
            return;
        driver.workLeft = recycle ? ScrapEquivalent.RecycleWork(tier, units) : ScrapEquivalent.StudyWork(tier, units);
    }
}

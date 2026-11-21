using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// The shared shape of the mod's generated bills (recycling, study, original items), built in the implied-def pass. Each
/// is attached to benches through its own recipeUsers, so no bench's recipe list is touched.
/// </summary>
internal static class BillRecipes
{
    /// <summary>
    /// A recipe with no products and one ingredient of <paramref name="count"/>, which may be any of <paramref name="allowed"/>.
    /// The default filter allows them too unless <paramref name="allowedByDefault"/> is false.
    /// </summary>
    public static RecipeDef New(string defName, List<ThingDef> users, IEnumerable<ThingDef> allowed, float count, bool allowedByDefault)
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
        // A Def hashes by defNameHash, which AddImpliedDef sets only later. The recipe is a dictionary key before then
        // (RecycleRecipes, StudyRecipes), and keyed with a hash of 0 it is never found again.
        recipe.ResolveDefNameHash();
        var ingredient = new IngredientCount();
        ingredient.SetBaseCount(count);
        foreach (ThingDef def in allowed)
        {
            ingredient.filter.SetAllow(def, true);
            recipe.fixedIngredientFilter.SetAllow(def, true);
            if (allowedByDefault)
                recipe.defaultIngredientFilter.SetAllow(def, true);
        }
        recipe.ingredients.Add(ingredient);
        return recipe;
    }

    /// <summary>Works like smelting: smelting speed, the smelter's effect and sound.</summary>
    public static void LikeSmelting(RecipeDef recipe)
    {
        recipe.workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("SmeltingSpeed");
        recipe.effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Smelt");
        recipe.soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Smelt");
    }

    /// <summary>
    /// Works like research: the pawn's research speed and Intellectual skill, done by the Research work type. The bill system
    /// applies a bench speed stat only to work tables, so the bench's research speed factor does not apply here.
    /// </summary>
    public static void LikeResearch(RecipeDef recipe)
    {
        recipe.workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("ResearchSpeed");
        recipe.workSkill = DefDatabase<SkillDef>.GetNamedSilentFail("Intellectual");
        recipe.effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Research");
        recipe.requiredGiverWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Research");
    }
}

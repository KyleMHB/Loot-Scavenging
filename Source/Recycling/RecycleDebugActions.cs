using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace LootScavenging;

internal static class RecycleDebugActions
{
    [DebugAction("Loot Scavenging", "Recycling: log recipes per bench", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)] // not translated
    private static void LogRecipesPerBench()
    {
        var sb = new System.Text.StringBuilder("[Loot Scavenging] Recycling recipes per bench (all recipes, ours, own recipes list):\n"); // not translated
        var benches = DefDatabase<RecipeDef>.AllDefs
            .Where(r => r.defName.StartsWith(RecycleRecipes.DefNamePrefix) && r.recipeUsers != null)
            .SelectMany(r => r.recipeUsers).Distinct();
        foreach (ThingDef bench in benches)
        {
            var ours = bench.AllRecipes.Where(r => r.defName.StartsWith(RecycleRecipes.DefNamePrefix))
                .Select(r => r.defName.Substring(RecycleRecipes.DefNamePrefix.Length));
            sb.AppendLine($"  {bench.defName}: {bench.AllRecipes.Count} ({string.Join(", ", ours)}); {(bench.recipes?.Count ?? 0)}"); // not translated
        }
        Log.Message(sb.ToString());
    }
}

using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Lets recycling bills keep a count in stock. The game only offers that for a recipe with one product and no special
/// products; a recycling recipe has several, so it counts the first listed yield.
/// </summary>
public class RecipeWorkerCounter_Recycle : RecipeWorkerCounter
{
    public override bool CanCountProducts(Bill_Production bill)
    {
        return recipe.products != null && recipe.products.Count > 0;
    }
}

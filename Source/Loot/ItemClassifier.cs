using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>Reads the game facts the evaluator's protection and category steps need from one item.</summary>
internal static class ItemClassifier
{
    /// <summary>The inventory group of an item carried in a pawn's inventory. Weapons and apparel carried there count by their group too.</summary>
    public static ItemCategory InventoryCategoryOf(Thing thing)
    {
        ThingDef def = thing.def;
        // A downed pawn's weapon waits in his pack (ticket 21), so it must be judged by the weapon rule there as it would be
        // in his hands.
        if (EraReader.IsGear(def))
            return def.IsApparel ? ItemCategory.Apparel : ItemCategory.Weapon;
        if (IsAmmunition(def))
            return ItemCategory.Ammunition;
        if (def.IsMedicine)
            return ItemCategory.Medicine;
        if (def.IsDrug)
            return ItemCategory.Drugs;
        if (def.IsNutritionGivingIngestible)
            return ItemCategory.Food;
        return ItemCategory.Other;
    }

    /// <summary>
    /// The scrap units a thing makes in <paramref name="tier"/>. Anything that stacks is rounded per stack, so 53 devilstrand do
    /// not become 53 scrap; single items (gear) get at least one unit each. Never worth more than the item or stack itself, so it
    /// is 0 for one worth less than a unit: the caller then makes no scrap.
    /// </summary>
    public static int ScrapUnits(Thing thing, ScrapTier tier)
    {
        float worth = ScrapDefs.WorthPerUnit(tier);
        return thing.def.stackLimit > 1
            ? ScrapAmounts.CappedStackUnits(thing.MarketValue, thing.stackCount, worth)
            : ScrapAmounts.CappedUnits(thing.MarketValue, thing.stackCount, worth);
    }

    // Ammo is recognised by item category name, so no ammo mod is referenced: vanilla-style mods and Combat Extended
    // both put their rounds under a category with "Ammo" in its def name (checked up the category tree).
    private static bool IsAmmunition(ThingDef def)
    {
        if (def.thingCategories == null)
            return false;
        foreach (ThingCategoryDef category in def.thingCategories)
        {
            if (NamesAmmo(category))
                return true;
            foreach (ThingCategoryDef parent in category.Parents)
            {
                if (NamesAmmo(parent))
                    return true;
            }
        }
        return false;
    }

    private static bool NamesAmmo(ThingCategoryDef category)
    {
        return category.defName.IndexOf("Ammo", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Fills the protection facts and the def name on a candidate.</summary>
    public static void DescribeProtection(Thing thing, DropCandidate candidate)
    {
        candidate.DefName = thing.def.defName;
        candidate.QuestTagged = !thing.questTags.NullOrEmpty();
        candidate.Relic = thing.IsRelic();
        if (thing is ThingWithComps withComps)
        {
            candidate.UniqueWeapon = withComps.TryGetComp<CompUniqueWeapon>() != null;
            candidate.PersonaWeapon = withComps.TryGetComp<CompBladelinkWeapon>() != null;
        }
    }
}

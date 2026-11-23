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

    /// <summary>The scrap units a stack is worth. Ammunition rounds per stack, everything else per item.</summary>
    public static int ScrapUnits(Thing thing)
    {
        return IsAmmunition(thing.def)
            ? ScrapAmounts.AmmoUnits(thing.MarketValue, thing.stackCount)
            : ScrapAmounts.Units(thing.MarketValue, thing.stackCount);
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

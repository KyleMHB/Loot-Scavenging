using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;

namespace LootScavenging.Loot;

/// <summary>Degrade and instant disassembly (ticket 08). Both act on an item still held by its pawn.</summary>
internal static class DisassembleApplier
{
    public static void Degrade(Thing thing)
    {
        LootRules rules = LootScavengingMod.Settings.values.rules;
        int before = thing.HitPoints;
        if (thing.def.useHitPoints && thing.MaxHitPoints > 0)
        {
            thing.HitPoints = Disassembly.DegradedHitPoints(thing.MaxHitPoints, thing.HitPoints, rules.degradeMin, rules.degradeMax, Rand.Value);
        }

        QualityCategory? lowered = null;
        if (rules.degradeLowersQuality && thing.TryGetQuality(out QualityCategory quality))
        {
            var next = (QualityCategory)(byte)Disassembly.StepDown((ItemQuality)(byte)quality);
            if (next != quality)
            {
                thing.TryGetComp<CompQuality>().SetQuality(next, ArtGenerationContext.Outsider);
                lowered = next;
            }
        }

        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] Degraded {thing}: hit points {before} -> {thing.HitPoints}{(lowered.HasValue ? ", quality " + lowered.Value : "")}"); // not translated
    }

    public static void Disassemble(Pawn pawn, Thing thing, EraResult era, DecisionReason reason)
    {
        LootRules rules = LootScavengingMod.Settings.values.rules;
        ScrapTier tier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
        int units = ScrapAmounts.Units(thing.MarketValue, thing.stackCount);

        List<Thing> made;
        string source;
        if (reason == DecisionReason.ComponentsFallback)
        {
            // Gear with no era: break it into components, scaled by what its scrap would have been.
            source = "components";
            made = TierFor(new[] { new YieldAmount<ThingDef>(ThingDefOf.ComponentIndustrial, Disassembly.ComponentsFor(units)) });
        }
        else
        {
            List<YieldAmount<ThingDef>> amounts = null;
            if (rules.disassemblyYield == DisassemblyYield.ItemBased)
                amounts = ItemBasedYield(thing, rules.disassemblyEfficiency);
            if (amounts != null && amounts.Count > 0)
            {
                source = "item-based";
                made = TierFor(amounts);
            }
            else
            {
                // Era-typical mode, or an item with no cost list and no material to take apart: one pool pick per scrap unit.
                source = "era-typical";
                made = ScrapTierTable.RollProducts(ScrapTierTable.For(tier), units).ToList();
            }
        }

        Map map = pawn.MapHeld;
        IntVec3 position = pawn.PositionHeld;
        OutcomeApplier.Delete(pawn, thing);

        foreach (Thing t in made)
            Place(pawn, t, map, position);

        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] Disassembled {thing} ({source}): {string.Join(", ", made.Select(t => t.LabelCap))}"); // not translated
    }

    // The mod's own calculation: the cost list plus the item's material (the game's count for it), never SmeltProducts.
    private static List<YieldAmount<ThingDef>> ItemBasedYield(Thing thing, float efficiency)
    {
        var costs = new List<YieldAmount<ThingDef>>();
        if (thing.def.CostList != null)
        {
            foreach (ThingDefCountClass c in thing.def.CostList)
            {
                if (c.thingDef != null && c.count > 0)
                    costs.Add(new YieldAmount<ThingDef>(c.thingDef, c.count));
            }
        }
        if (thing.def.MadeFromStuff && thing.Stuff != null && thing.def.CostStuffCount > 0)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(thing.def.CostStuffCount / thing.Stuff.VolumePerUnit));
            costs.Add(new YieldAmount<ThingDef>(thing.Stuff, count));
        }
        return Disassembly.FromCosts(costs, thing.stackCount, efficiency, () => Rand.Value).ToList();
    }

    private static List<Thing> TierFor(IEnumerable<YieldAmount<ThingDef>> amounts) => ScrapTierTable.MakeThings(amounts).ToList();

    // Loaded ammo of reloadable gear, as things. Vanilla never does this, so it is the mod's own feature.
    internal static IEnumerable<Thing> EjectedCharges(Thing thing)
    {
        if (!(thing is ThingWithComps withComps))
            yield break;
        foreach (ThingComp comp in withComps.AllComps)
        {
            if (!(comp is IReloadableComp reloadable) || reloadable.AmmoDef == null)
                continue;
            int perCharge = 0, toRefill = 0;
            if (comp.props is CompProperties_ApparelReloadable a)
            {
                perCharge = a.ammoCountPerCharge;
                toRefill = a.ammoCountToRefill;
            }
            else if (comp.props is CompProperties_EquippableAbilityReloadable e)
            {
                perCharge = e.ammoCountPerCharge;
                toRefill = e.ammoCountToRefill;
            }
            else
            {
                toRefill = reloadable.MaxAmmoAmount(); // another mod's comp: assume a full load refills it
            }

            int ammo = Disassembly.EjectedAmmo(reloadable.RemainingCharges, reloadable.MaxCharges, perCharge, toRefill);
            foreach (int stack in ScrapAmounts.SplitIntoStacks(ammo, reloadable.AmmoDef.stackLimit))
            {
                Thing t = ThingMaker.MakeThing(reloadable.AmmoDef);
                t.stackCount = stack;
                yield return t;
            }
        }
    }

    // Near the pawn, forbidden like other enemy drops, or held for the body when everything stays on it. Stacks were
    // already split at the stack limit.
    internal static void Place(Pawn pawn, Thing thing, Map map, IntVec3 position)
    {
        if (BodyLoot.KeepsAll(pawn))
        {
            BodyLoot.Hold(pawn, thing);
            return;
        }
        thing.SetForbidden(true, false);
        if (map != null && position.IsValid && GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near))
            return;
        // Nowhere to put it on the map: keep it with the pawn rather than lose it.
        if (pawn.inventory == null || !pawn.inventory.innerContainer.TryAdd(thing, true))
            Log.Warning($"[Loot Scavenging] No room to place {thing} from the disassembly near {pawn}; it is lost."); // not translated
    }
}

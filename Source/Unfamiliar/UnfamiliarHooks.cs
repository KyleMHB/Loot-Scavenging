using HarmonyLib;
using RimWorld;
using Verse;

namespace LootScavenging;

// The equip block. CanEquip feeds the right-click menu (a greyed "cannot equip" entry with a reason) and the outfit and
// dress paths. Only the 4-argument overload is patched: the 2-argument one calls it. Colonists choosing their own apparel never call it, so the apparel score is patched too. Pawn generation
// and raiders are left alone. Postfixes only, so nothing here can stop another mod's own check.

[HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
    new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
internal static class CanEquipWithReasonHook
{
    private static void Postfix(Thing thing, Pawn pawn, ref bool __result, ref string cantReason)
    {
        if (__result && Unfamiliar.Blocks(thing, pawn))
        {
            __result = false;
            cantReason = "LootScavenging_CannotEquipUnfamiliar".Translate().Resolve();
        }
    }
}

// Colonists put on the best apparel by score; a score below 0.05 is skipped.
[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreGain))]
internal static class ApparelScoreGainHook
{
    private static void Postfix(Pawn pawn, Apparel ap, ref float __result)
    {
        if (Unfamiliar.Blocks(ap, pawn))
            __result = -1000f;
    }
}

// Finishing research changes what is out of reach.
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
internal static class ResearchFinishedHook
{
    private static void Postfix()
    {
        Unfamiliar.ResetCache();
        EraReader.ResetColonyEraCache();
    }
}

using System;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Keeps saves loadable without the mod. Every saved thing carries a Class attribute. Without the mod, a bench saved as
/// "LootScavenging.Building_StudyBench" would load as the vanilla class through the game's def fallback, but with a
/// "Could not find class" error per bench. So the bench is saved under the vanilla class name, and with the mod loaded
/// that name is mapped back to <see cref="Building_StudyBench"/> when the thing's def uses it. Without the mod the bench
/// loads as a plain research bench and its unread billStack node is skipped.
/// </summary>
[HarmonyPatch(typeof(ScribeSaver), nameof(ScribeSaver.WriteAttribute))]
internal static class StudyBenchSaveName
{
    private static readonly string StudyBenchName = typeof(Building_StudyBench).FullName;
    private static readonly string VanillaName = GenTypes.GetTypeNameWithoutIgnoredNamespaces(typeof(Building_ResearchBench));

    private static void Prefix(string attributeName, ref string value)
    {
        if (value == StudyBenchName && attributeName == "Class") // not translated
            value = VanillaName;
    }
}

[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType))]
internal static class StudyBenchLoadType
{
    private static void Postfix(Type baseType, XmlNode node, ref Type __result)
    {
        if (__result != typeof(Building_ResearchBench) || node == null)
            return;
        // The game's System.Xml reference marks these as Windows-only; the game itself reads saves this way on every platform.
#pragma warning disable CA1416
        string defName = node["def"]?.InnerText; // not translated
#pragma warning restore CA1416
        ThingDef def = defName != null ? DefDatabase<ThingDef>.GetNamedSilentFail(defName) : null;
        if (def != null && def.thingClass == typeof(Building_StudyBench))
            __result = typeof(Building_StudyBench);
    }
}

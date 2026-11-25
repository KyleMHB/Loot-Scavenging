using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking the rules in game without opening the settings page. Changes last until
// the game closes; they are not written to the settings file.
internal static class LootDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    private static LootRules Rules => LootScavengingMod.Settings.values.rules;

    [DebugAction(Category, "Log rules", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void LogRules()
    {
        LootRules r = Rules;
        SettingValues v = LootScavengingMod.Settings.values;
        Log.Message($"[Loot Scavenging] whoHostile={r.whoHostile} whoNonHostile={r.whoNonHostile} whoWild={r.whoWild} whoPrisoners={r.whoPrisoners} whoRecruitedPrisoners={r.whoRecruitedPrisoners} whoOtherJoiners={r.whoOtherJoiners} homeMapOnly={r.homeMapOnly} colonistsKeepGear={v.colonistsKeepGear} hostilesKeepGear={v.hostilesKeepGear} nonHostilesKeepGear={v.nonHostilesKeepGear} weapons={r.weapons.outcome} apparel={r.apparel.outcome}"); // not translated
    }

    [DebugAction(Category, "Cycle wild row", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleWild()
    {
        Rules.whoWild = Next(Rules.whoWild);
        LogRules();
    }

    [DebugAction(Category, "Cycle prisoners row", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CyclePrisoners()
    {
        Rules.whoPrisoners = Next(Rules.whoPrisoners);
        LogRules();
    }

    [DebugAction(Category, "Toggle home map only", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleHomeMapOnly()
    {
        Rules.homeMapOnly = !Rules.homeMapOnly;
        LogRules();
    }

    private static WhoRule Next(WhoRule rule) => rule switch
    {
        WhoRule.Apply => WhoRule.Exclude,
        WhoRule.Exclude => WhoRule.Mark,
        _ => WhoRule.Apply,
    };
}

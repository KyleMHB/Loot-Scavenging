using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class SettingKeysTests
{
    // Every key the settings file used before the settings were walked by field, copied from the old ExposeData, less the
    // settings 0.6.0 cut or replaced (ticket 25), plus the ones it added. A changed key would make existing configs lose
    // that value.
    private static Dictionary<string, string> SavedKeys()
    {
        var keys = new Dictionary<string, string>
        {
            ["corpses.humanlike"] = "corpseHumanlike",
            ["corpses.animal"] = "corpseAnimal",
            ["corpses.mechanoid"] = "corpseMechanoid",
            ["rules.whoHostile"] = "whoHostile",
            ["rules.whoNonHostile"] = "whoNonHostile",
            ["rules.whoWild"] = "whoWild",
            ["rules.whoPrisoners"] = "whoPrisoners",
            ["rules.whoRecruitedPrisoners"] = "whoRecruitedPrisoners",
            ["rules.whoOtherJoiners"] = "whoOtherJoiners",
            ["rules.homeMapOnly"] = "homeMapOnly",
            ["rules.disassemblyYield"] = "disassemblyYield",
            ["rules.disassemblyEfficiency"] = "disassemblyEfficiency",
            ["rules.protectQuestItems"] = "protectQuestItems",
            ["rules.protectUniqueWeapons"] = "protectUniqueWeapons",
            ["rules.protectPersonaWeapons"] = "protectPersonaWeapons",
            ["rules.protectRelics"] = "protectRelics",
            ["rules.keepList"] = "keepList",
            ["rules.removeList"] = "removeList",
            ["colonistsKeepGear"] = "colonistsKeepGear",
            ["hostilesKeepGear"] = "hostilesKeepGear",
            ["nonHostilesKeepGear"] = "nonHostilesKeepGear",
            ["cremationStripsCorpses"] = "cremationStripsCorpses",
            ["scrapMarketValueNeolithic"] = "scrapMarketValueNeolithic",
            ["scrapMarketValueMedieval"] = "scrapMarketValueMedieval",
            ["scrapMarketValueIndustrial"] = "scrapMarketValueIndustrial",
            ["scrapMarketValueSpacer"] = "scrapMarketValueSpacer",
            ["scrapMarketValueUltra"] = "scrapMarketValueUltra",
            ["scrapMarketValueArchotech"] = "scrapMarketValueArchotech",
            ["unfamiliarScope"] = "unfamiliarScope",
            ["recycleOrStudy"] = "recycleOrStudy",
            ["studyTechprintProjects"] = "studyTechprintProjects",
            ["studyPointsNeolithic"] = "studyPointsNeolithic",
            ["studyPointsMedieval"] = "studyPointsMedieval",
            ["studyPointsIndustrial"] = "studyPointsIndustrial",
            ["studyPointsSpacer"] = "studyPointsSpacer",
            ["studyPointsUltra"] = "studyPointsUltra",
            ["studyPointsArchotech"] = "studyPointsArchotech",
        };
        string[] categories = { "weapons", "apparel", "ammunition", "drugs", "medicine", "food", "other" };
        string[,] ruleFields =
        {
            { "outcome", "Outcome" }, { "onlyBeyondReach", "OnlyBeyondReach" }, { "limitQuality", "LimitQuality" },
            { "belowQuality", "BelowQuality" }, { "limitHitPoints", "LimitHitPoints" },
            { "belowHitPointFraction", "BelowHitPointFraction" }, { "chance", "Chance" },
        };
        foreach (string category in categories)
        {
            for (int i = 0; i < ruleFields.GetLength(0); i++)
                keys["rules." + category + "." + ruleFields[i, 0]] = category + ruleFields[i, 1];
        }
        return keys;
    }

    [Fact]
    public void EverySettingKeepsItsSavedKey()
    {
        Dictionary<string, string> expected = SavedKeys();
        Dictionary<string, string> actual = SettingKeys.All.ToDictionary(k => k.Path, k => k.Key);
        Assert.Equal(expected.OrderBy(k => k.Key), actual.OrderBy(k => k.Key));
    }

    // An old key read into a new setting of another type would lose the value or fail to parse, so none is reused.
    [Fact]
    public void NoSettingReusesAKeyFrom052()
    {
        foreach (string old in LegacySettings.Keys)
            Assert.DoesNotContain(SettingKeys.All, k => k.Key == old);
    }

    [Fact]
    public void NoTwoSettingsShareAKey()
    {
        Assert.Equal(SettingKeys.All.Count, SettingKeys.All.Select(k => k.Key).Distinct().Count());
    }
}

using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class SettingKeysTests
{
    // Every key the settings file used before the settings were walked by field, copied from the old ExposeData. A changed
    // key would make existing configs lose that value.
    private static Dictionary<string, string> SavedKeys()
    {
        var keys = new Dictionary<string, string>
        {
            ["corpses.humanlike"] = "corpseHumanlike",
            ["corpses.animal"] = "corpseAnimal",
            ["corpses.mechanoid"] = "corpseMechanoid",
            ["corpses.chance"] = "corpseChance",
            ["corpses.butcherRemovesApparel"] = "butcherRemovesApparel",
            ["rules.hostile"] = "hostile",
            ["rules.nonHostileFaction"] = "nonHostileFaction",
            ["rules.noFaction"] = "noFaction",
            ["rules.prisoners"] = "prisoners",
            ["rules.homeMapOnly"] = "homeMapOnly",
            ["rules.onDownedAndStrip"] = "onDownedAndStrip",
            ["rules.dropMode"] = "dropMode",
            ["rules.dropIncludesMedicine"] = "dropIncludesMedicine",
            ["rules.degradeMin"] = "degradeMin",
            ["rules.degradeMax"] = "degradeMax",
            ["rules.degradeLowersQuality"] = "degradeLowersQuality",
            ["rules.disassemblyYield"] = "disassemblyYield",
            ["rules.disassemblyEfficiency"] = "disassemblyEfficiency",
            ["rules.ejectCharges"] = "ejectCharges",
            ["rules.protectQuestItems"] = "protectQuestItems",
            ["rules.protectUniqueWeapons"] = "protectUniqueWeapons",
            ["rules.protectPersonaWeapons"] = "protectPersonaWeapons",
            ["rules.protectRelics"] = "protectRelics",
            ["rules.keepList"] = "keepList",
            ["rules.removeList"] = "removeList",
            ["eraFallback"] = "eraFallback",
            ["scrapMarketValue"] = "scrapMarketValue",
            ["deathDrop"] = "deathDrop",
            ["unfamiliarScope"] = "unfamiliarScope",
            ["studyEras"] = "studyEras",
            ["recycleOrStudy"] = "recycleOrStudy",
            ["studyTechprintProjects"] = "studyTechprintProjects",
            ["studyPointsNeolithic"] = "studyPointsNeolithic",
            ["studyPointsMedieval"] = "studyPointsMedieval",
            ["studyPointsIndustrial"] = "studyPointsIndustrial",
            ["studyPointsSpacer"] = "studyPointsSpacer",
            ["studyPointsUltra"] = "studyPointsUltra",
            ["studyPointsArchotech"] = "studyPointsArchotech",
            ["studyPointsMultiplier"] = "studyPointsMultiplier",
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

    [Fact]
    public void NoTwoSettingsShareAKey()
    {
        Assert.Equal(SettingKeys.All.Count, SettingKeys.All.Select(k => k.Key).Distinct().Count());
    }
}

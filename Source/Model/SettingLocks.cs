using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>
/// The settings a pack has locked (spec, "The Progression pack": "an option to lock chosen settings"). A locked setting
/// always holds the pack's default: presets, Reset and a saved value cannot change it, and the page shows it disabled.
/// Names are field paths such as "rules.whoHostile" or "corpses.humanlike"; see <see cref="Lockable"/>.
/// </summary>
public sealed class SettingLocks
{
    /// <summary>
    /// Every name a pack can lock, one per row on the page: the constants in <see cref="SettingNames"/> plus one per item
    /// category. The keep and remove lists and the four protection toggles sit in the page's Lists section and are not
    /// lockable yet. A test checks each name is a real field path, so a misspelt constant fails there.
    /// </summary>
    public static readonly string[] Lockable = BuildLockable();

    private static string[] BuildLockable()
    {
        var all = new List<string>();
        foreach (System.Reflection.FieldInfo field in typeof(SettingNames).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
                all.Add((string)field.GetRawConstantValue());
        }
        foreach (CategoryInfo category in ItemCategories.All)
            all.Add(SettingNames.Category(category));
        return all.ToArray();
    }

    public static readonly SettingLocks None = new SettingLocks(null);

    // Lock names a pack may still use for settings since replaced: locking the old one locks each that replaced it (ticket
    // 24, ticket 25 decision 11). An empty list names a setting that was cut: it is accepted and ignored, so an old pack's
    // lock list does not log it as unknown. Kept out of Lockable, so the page and the Def's list show only the new names.
    private static readonly Dictionary<string, string[]> Renamed = BuildRenamed();

    private static Dictionary<string, string[]> BuildRenamed()
    {
        var rows = new List<string>();
        foreach (OwnerGroup group in LootRules.RowGroups)
            rows.Add(SettingNames.Row(group));
        var dropMode = new List<string>(rows);
        foreach (CategoryInfo category in ItemCategories.All)
            dropMode.Add(SettingNames.Category(category));
        var marketValues = new List<string>();
        var studyPoints = new List<string>();
        foreach (ScrapTier tier in SettingValues.Tiers)
        {
            marketValues.Add(SettingNames.ScrapMarketValue(tier));
            studyPoints.Add(SettingNames.StudyPoints(tier));
        }
        string[] keepGear = { SettingNames.HostilesKeepGear, SettingNames.NonHostilesKeepGear };
        string[] none = new string[0];
        return new Dictionary<string, string[]>
        {
            ["rules.hostile"] = new[] { SettingNames.WhoHostile }, // not translated
            ["rules.nonHostileFaction"] = new[] { SettingNames.WhoNonHostile }, // not translated
            ["rules.noFaction"] = new[] { SettingNames.WhoWild }, // not translated
            ["rules.prisoners"] = new[] { SettingNames.WhoPrisoners }, // not translated
            ["rules.prisonerJoinGear"] = new[] { SettingNames.WhoRecruitedPrisoners }, // not translated
            ["rules.otherJoinGear"] = new[] { SettingNames.WhoOtherJoiners }, // not translated
            ["rules.dropMode"] = dropMode.ToArray(), // not translated
            ["rules.dropIncludesMedicine"] = new[] { "rules.medicine" }, // not translated
            ["rules.dropIncludesAmmunition"] = new[] { "rules.ammunition" }, // not translated
            ["deathDrop"] = keepGear, // not translated
            ["rules.downedGear"] = keepGear, // not translated
            ["scrapMarketValue"] = marketValues.ToArray(), // not translated
            ["studyPointsMultiplier"] = studyPoints.ToArray(), // not translated
            ["rules.sortWhenArrested"] = none, // not translated
            ["rules.sortWhenStripped"] = none, // not translated
            ["rules.onDownedAndStrip"] = none, // not translated
            ["rules.degradeMin"] = none, // not translated
            ["rules.degradeMax"] = none, // not translated
            ["rules.degradeLowersQuality"] = none, // not translated
            ["rules.ejectCharges"] = none, // not translated
            ["corpses.chance"] = none, // not translated
            ["corpses.butcherRemovesApparel"] = none, // not translated
            ["eraFallback"] = none, // not translated
            ["studyEras"] = none, // not translated
        };
    }

    private readonly HashSet<string> names = new HashSet<string>();

    /// <summary>Requested names that are not in <see cref="Lockable"/>; they are ignored.</summary>
    public List<string> Unknown { get; } = new List<string>();

    public SettingLocks(IEnumerable<string> requested)
    {
        if (requested == null)
            return;
        var known = new HashSet<string>(Lockable);
        foreach (string raw in requested)
        {
            string name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;
            if (Renamed.TryGetValue(name, out string[] replacements))
            {
                foreach (string replacement in replacements)
                    names.Add(replacement);
            }
            else if (known.Contains(name))
                names.Add(name);
            else if (!Unknown.Contains(name))
                Unknown.Add(name);
        }
    }

    public bool Any => names.Count > 0;

    public bool IsLocked(string name) => name != null && names.Contains(name);

    /// <summary>Copies every locked value from <paramref name="source"/> (the pack's defaults) over <paramref name="target"/>.</summary>
    public void Enforce(SettingValues target, SettingValues source)
    {
        foreach (string name in names)
            ValueWalk.CopyPath(target, source, name);
    }
}

/// <summary>
/// The lockable setting names, as field paths into <see cref="SettingValues"/>. The page and the lock table both use these, so
/// a name is spelt once. Item categories are named by <see cref="Category"/>.
/// </summary>
public static class SettingNames
{
    public const string WhoHostile = "rules.whoHostile"; // not translated
    public const string WhoNonHostile = "rules.whoNonHostile"; // not translated
    public const string WhoWild = "rules.whoWild"; // not translated
    public const string WhoPrisoners = "rules.whoPrisoners"; // not translated
    public const string WhoRecruitedPrisoners = "rules.whoRecruitedPrisoners"; // not translated
    public const string WhoOtherJoiners = "rules.whoOtherJoiners"; // not translated
    public const string HomeMapOnly = "rules.homeMapOnly"; // not translated
    public const string ColonistsKeepGear = "colonistsKeepGear"; // not translated
    public const string HostilesKeepGear = "hostilesKeepGear"; // not translated
    public const string NonHostilesKeepGear = "nonHostilesKeepGear"; // not translated
    public const string DisassemblyYield = "rules.disassemblyYield"; // not translated
    public const string DisassemblyEfficiency = "rules.disassemblyEfficiency"; // not translated
    public const string CorpseHumanlike = "corpses.humanlike"; // not translated
    public const string CorpseAnimal = "corpses.animal"; // not translated
    public const string CorpseMechanoid = "corpses.mechanoid"; // not translated
    public const string UnfamiliarScope = "unfamiliarScope"; // not translated
    public const string RecycleOrStudy = "recycleOrStudy"; // not translated
    public const string StudyTechprintProjects = "studyTechprintProjects"; // not translated
    public const string StudyPointsNeolithic = "studyPointsNeolithic"; // not translated
    public const string StudyPointsMedieval = "studyPointsMedieval"; // not translated
    public const string StudyPointsIndustrial = "studyPointsIndustrial"; // not translated
    public const string StudyPointsSpacer = "studyPointsSpacer"; // not translated
    public const string StudyPointsUltra = "studyPointsUltra"; // not translated
    public const string StudyPointsArchotech = "studyPointsArchotech"; // not translated
    public const string ScrapMarketValueNeolithic = "scrapMarketValueNeolithic"; // not translated
    public const string ScrapMarketValueMedieval = "scrapMarketValueMedieval"; // not translated
    public const string ScrapMarketValueIndustrial = "scrapMarketValueIndustrial"; // not translated
    public const string ScrapMarketValueSpacer = "scrapMarketValueSpacer"; // not translated
    public const string ScrapMarketValueUltra = "scrapMarketValueUltra"; // not translated
    public const string ScrapMarketValueArchotech = "scrapMarketValueArchotech"; // not translated

    /// <summary>The lock name of a group's Who row.</summary>
    public static string Row(OwnerGroup group) => group switch
    {
        OwnerGroup.Hostile => WhoHostile,
        OwnerGroup.NonHostileFaction => WhoNonHostile,
        OwnerGroup.NoFaction => WhoWild,
        OwnerGroup.Prisoner => WhoPrisoners,
        OwnerGroup.RecruitedPrisoner => WhoRecruitedPrisoners,
        OwnerGroup.OtherJoiner => WhoOtherJoiners,
        _ => null,
    };

    /// <summary>The lock name of a tier's scrap market value.</summary>
    public static string ScrapMarketValue(ScrapTier tier) => "scrapMarketValue" + tier; // not translated

    /// <summary>The lock name of a tier's study points.</summary>
    public static string StudyPoints(ScrapTier tier) => "studyPoints" + tier; // not translated

    /// <summary>A category's rule, locked as a whole: outcome, conditions and chance.</summary>
    public static string Category(CategoryInfo category) => "rules." + category.Field; // not translated
}

/// <summary>What the presets are built from: the Progression defaults (from the pack's patchable Def) and the lock set.</summary>
public sealed class PresetConfig
{
    public SettingValues Progression;
    public SettingLocks Locks;

    public PresetConfig(SettingValues progression, SettingLocks locks)
    {
        Progression = progression;
        Locks = locks ?? SettingLocks.None;
    }

    /// <summary>The values compiled into the mod, with nothing locked. Used until the Def is read and by tests.</summary>
    public static PresetConfig Code() => new PresetConfig(Presets.CodeProgression(), SettingLocks.None);
}

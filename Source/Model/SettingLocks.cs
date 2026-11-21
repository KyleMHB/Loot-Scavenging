using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>
/// The settings a pack has locked (spec, "The Progression pack": "an option to lock chosen settings"). A locked setting
/// always holds the pack's default: presets, Reset and a saved value cannot change it, and the page shows it disabled.
/// Names are field paths such as "rules.dropMode" or "corpses.chance"; see <see cref="Lockable"/>.
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
            if (known.Contains(name))
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
    public const string Hostile = "rules.hostile"; // not translated
    public const string NonHostileFaction = "rules.nonHostileFaction"; // not translated
    public const string NoFaction = "rules.noFaction"; // not translated
    public const string Prisoners = "rules.prisoners"; // not translated
    public const string HomeMapOnly = "rules.homeMapOnly"; // not translated
    public const string OnDownedAndStrip = "rules.onDownedAndStrip"; // not translated
    public const string DropMode = "rules.dropMode"; // not translated
    public const string DropIncludesMedicine = "rules.dropIncludesMedicine"; // not translated
    public const string DegradeMin = "rules.degradeMin"; // not translated
    public const string DegradeMax = "rules.degradeMax"; // not translated
    public const string DegradeLowersQuality = "rules.degradeLowersQuality"; // not translated
    public const string DisassemblyYield = "rules.disassemblyYield"; // not translated
    public const string DisassemblyEfficiency = "rules.disassemblyEfficiency"; // not translated
    public const string EjectCharges = "rules.ejectCharges"; // not translated
    public const string CorpseHumanlike = "corpses.humanlike"; // not translated
    public const string CorpseAnimal = "corpses.animal"; // not translated
    public const string CorpseMechanoid = "corpses.mechanoid"; // not translated
    public const string CorpseChance = "corpses.chance"; // not translated
    public const string ButcherRemovesApparel = "corpses.butcherRemovesApparel"; // not translated
    public const string EraFallback = "eraFallback"; // not translated
    public const string ScrapMarketValue = "scrapMarketValue"; // not translated
    public const string DeathDrop = "deathDrop"; // not translated
    public const string UnfamiliarScope = "unfamiliarScope"; // not translated
    public const string StudyEras = "studyEras"; // not translated
    public const string RecycleOrStudy = "recycleOrStudy"; // not translated
    public const string StudyTechprintProjects = "studyTechprintProjects"; // not translated

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

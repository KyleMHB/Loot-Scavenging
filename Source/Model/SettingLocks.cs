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
    /// Every name a pack can lock, one per row on the page. The keep and remove lists and the four protection toggles sit in
    /// the page's Lists section and are not lockable yet.
    /// </summary>
    public static readonly string[] Lockable =
    {
        "rules.hostile", "rules.nonHostileFaction", "rules.noFaction", "rules.prisoners", "rules.homeMapOnly",
        "rules.onDownedAndStrip", "rules.dropMode",
        "rules.weapons", "rules.apparel", "rules.ammunition", "rules.drugs", "rules.medicine", "rules.food", "rules.other",
        "rules.degradeMin", "rules.degradeMax", "rules.degradeLowersQuality",
        "rules.disassemblyYield", "rules.disassemblyEfficiency", "rules.ejectCharges",
        "corpses.humanlike", "corpses.animal", "corpses.mechanoid", "corpses.chance", "corpses.butcherRemovesApparel",
        "eraFallback", "scrapMarketValue", "scrapOnGround", "unfamiliarScope", "studyEras", "recycleOrStudy",
        "studyTechprintProjects",
    };

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

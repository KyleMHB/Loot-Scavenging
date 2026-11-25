using System.Collections.Generic;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

/// <summary>
/// The default settings, as XML a pack can patch (Defs/SettingsDefaults.xml). <see cref="values"/> is the Progression preset,
/// which is also what a fresh config starts with and what Reset returns to; Cleanup and Vanilla are built from it. A pack
/// changes a default with a patch on /Defs/LootScavenging.SettingsDefaultsDef/values/..., and locks settings by adding
/// names from <see cref="SettingLocks.Lockable"/> to <see cref="locked"/>.
/// </summary>
public class SettingsDefaultsDef : Def
{
    public SettingValues values = new SettingValues();

    /// <summary>Settings the player cannot change. They always hold the value in <see cref="values"/>.</summary>
    public List<string> locked = new List<string>();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string e in base.ConfigErrors())
            yield return e;
        if (values == null)
            yield return "values is not set"; // not translated
        var seen = new SettingLocks(locked);
        foreach (string name in seen.Unknown)
            yield return "locked names a setting that cannot be locked: " + name; // not translated
    }
}

/// <summary>Reads the defaults Def once the defs are loaded and hands it to the presets and the live settings.</summary>
[StaticConstructorOnStartup]
internal static class SettingsDefaults
{
    static SettingsDefaults()
    {
        PresetConfig config = Read();
        Presets.Configure(config);
        LootScavengingMod.Settings?.OnDefaultsLoaded();
    }

    private static PresetConfig Read()
    {
        List<SettingsDefaultsDef> defs = DefDatabase<SettingsDefaultsDef>.AllDefsListForReading;
        if (defs.Count == 0)
            return PresetConfig.Code();
        if (defs.Count > 1)
            Log.Warning($"[Loot Scavenging] {defs.Count} settings defaults defs are loaded; using {defs[0].defName}. Patch that one instead of adding another."); // not translated

        SettingsDefaultsDef def = defs[0];
        // An old pack's patch on a node replaced in 0.6.0 becomes the settings that replaced it (ticket 25).
        SettingsLoad.MigrateDef(def.values);
        return new PresetConfig(def.values ?? PresetConfig.Code().Progression, new SettingLocks(def.locked));
    }
}

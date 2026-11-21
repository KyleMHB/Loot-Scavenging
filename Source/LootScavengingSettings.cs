using System.Collections.Generic;
using System.Reflection;
using LootScavenging.Loot;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. The mod loads them once (GetSettings in the mod constructor) and keeps the instance in
// LootScavengingMod.Settings; the evaluator, hooks and benches read its values directly, so that instance is the cache and
// there is nothing to rebuild when a value changes. The default values come from Defs/SettingsDefaults.xml (a pack can patch
// it), read in SettingsDefaultsDef.cs once the defs are loaded; Model/Presets.cs holds the compiled-in fallback.
// Every setting lives in SettingValues: a new one is a field there plus its page row. It is saved under the key from
// SettingKeys, which keeps the names existing configs use.
public class LootScavengingSettings : ModSettings
{
    public SettingValues values;

    // The preset last picked on the page (ticket 11). Whether it has been changed since is worked out, not saved.
    public PresetId activePreset = PresetId.Progression;

    // The one-time first-load message has been shown.
    public bool firstLoadMessageShown;

    // The settings defaults Def has been applied to this config. A config with no saved value starts false, so the Def's
    // values (which a pack may have patched) seed it once the defs are loaded.
    private bool defaultsSeeded;

    public LootScavengingSettings()
    {
        Apply(Presets.Defaults());
    }

    /// <summary>
    /// Called once the defs are loaded, since the mod constructor runs before them. A fresh config takes the Def's defaults;
    /// a saved one keeps its values except locked settings, which return to the pack's value.
    /// </summary>
    public void OnDefaultsLoaded()
    {
        if (!defaultsSeeded)
        {
            Apply(Presets.Defaults());
            activePreset = PresetId.Progression;
            defaultsSeeded = true;
        }
        else
        {
            SettingValues current = Capture();
            Presets.Config.Locks.Enforce(current, Presets.Config.Progression);
            Apply(current);
        }
        ScrapDefs.ApplyMarketValue();
    }

    /// <summary>The current values. Shares the live objects, so read it, do not keep it.</summary>
    public SettingValues Capture() => values;

    /// <summary>Sets every value from a copy of <paramref name="source"/>, so the source is never edited through the page.</summary>
    public void Apply(SettingValues source)
    {
        values = source.Clone();
    }

    public bool PresetChanged => Presets.IsChanged(activePreset, Capture());

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref activePreset, "activePreset", PresetId.Progression);
        Scribe_Values.Look(ref firstLoadMessageShown, "firstLoadMessageShown", false);
        Scribe_Values.Look(ref defaultsSeeded, "defaultsSeeded", false);

        // Missing keys load as the default preset's values.
        SettingValues defaults = Presets.Defaults();
        foreach (SettingKey key in SettingKeys.All)
        {
            FieldInfo field = ValueWalk.Resolve(values, key.Path, out object owner);
            ValueWalk.Resolve(defaults, key.Path, out object defaultOwner);
            if (field.FieldType == typeof(List<string>))
            {
                // Stored as def names, so an entry for a removed item is just an unmatched string.
                var list = (List<string>)field.GetValue(owner);
                Scribe_Collections.Look(ref list, key.Key, LookMode.Value);
                field.SetValue(owner, list ?? new List<string>());
            }
            else
            {
                LookValue.MakeGenericMethod(field.FieldType)
                    .Invoke(null, new[] { owner, field, key.Key, field.GetValue(defaultOwner) });
            }
        }
    }

    private static readonly MethodInfo LookValue = typeof(LootScavengingSettings).GetMethod(nameof(Look), BindingFlags.NonPublic | BindingFlags.Static);

    private static void Look<T>(object owner, FieldInfo field, string key, T defaultValue)
    {
        var value = (T)field.GetValue(owner);
        Scribe_Values.Look(ref value, key, defaultValue);
        field.SetValue(owner, value);
    }
}

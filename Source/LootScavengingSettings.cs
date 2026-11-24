using System.Collections.Generic;
using System.Reflection;
using LootScavenging.Loot;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. The mod loads them once (GetSettings in the mod constructor) and keeps the instance in
// LootScavengingMod.Settings; the evaluator, hooks and benches read its values directly, so that instance is the cache and
// there is nothing to rebuild when a value changes. The default values come from Defs/SettingsDefaults.xml (a pack can patch
// it), read in SettingsDefaultsDef.cs once the defs are loaded; Model/Presets.cs holds the compiled-in fallback, used only
// until then. The mod's own save writes every key. A key the file lacks is filled from the Def's Progression once it loads
// (Model/SettingsLoad.cs), so an older file, or one another tool wrote with only some keys, such as an Auto Mod Config preset,
// gets the pack's defaults for the rest.
// Every setting lives in SettingValues: a new one is a field there plus its page row. It is saved under the key from
// SettingKeys, which keeps the names existing configs use.
public class LootScavengingSettings : ModSettings
{
    public SettingValues values;

    // The preset last picked on the page (ticket 11). Whether it has been changed since is worked out, not saved.
    public PresetId activePreset = PresetId.Progression;

    // The settings defaults Def has been applied to this config. A file without it still counts as seeded when it holds any
    // saved key (SettingsLoad.IsSeeded); only a config with nothing saved takes the Def's values once the defs are loaded.
    private bool defaultsSeeded;

    // The keys the settings file held, recorded while it loads and used once by OnDefaultsLoaded. Null when no file was read
    // since then, so a later call only enforces the locks.
    private HashSet<string> loadedKeys;

    public LootScavengingSettings()
    {
        Apply(Presets.Defaults());
    }

    /// <summary>
    /// Called once the defs are loaded, since the mod constructor runs before them. A config with nothing saved takes the
    /// Def's defaults; a saved one keeps its values, fills the keys its file lacked from the Def's Progression, and puts locked
    /// settings back to the pack's value.
    /// </summary>
    public void OnDefaultsLoaded()
    {
        Apply(SettingsLoad.Resolve(Capture(), defaultsSeeded, loadedKeys, Presets.Config, out bool kept));
        if (!kept)
            activePreset = PresetId.Progression;
        defaultsSeeded = true;
        loadedKeys = null;
        LootScavengingMod.ApplySettingsToDefs();
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
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            RecordLoadedKeys();
        Scribe_Values.Look(ref activePreset, SettingsLoad.ActivePresetKey, PresetId.Progression, forceSave: true);
        Scribe_Values.Look(ref defaultsSeeded, SettingsLoad.SeededKey, false);

        // Every key is written, a value equal to its default too, so a choice the player made stays put when a pack or an update
        // changes the default (ticket 18). A missing key, in an older file or one another tool wrote, loads as this default, but
        // the settings load before the defs, so that is the compiled-in value until OnDefaultsLoaded fills it from the Def's
        // Progression.
        SettingValues defaults = SettingsLoad.Baseline(Presets.Config);
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

    // Only the loading pass reads the file's own node; the loader's later passes do not. The game's System.Xml reference
    // marks the indexer Windows-only; Scribe_Values reads the file the same way on every platform.
#pragma warning disable CA1416
    private void RecordLoadedKeys()
    {
        loadedKeys = new HashSet<string>();
        var node = Scribe.loader.curXmlParent;
        foreach (string key in new[] { SettingsLoad.ActivePresetKey, SettingsLoad.FirstLoadKey, SettingsLoad.SeededKey })
        {
            if (node[key] != null)
                loadedKeys.Add(key);
        }
        foreach (SettingKey key in SettingKeys.All)
        {
            if (node[key.Key] != null)
                loadedKeys.Add(key.Key);
        }
    }
#pragma warning restore CA1416

    private static readonly MethodInfo LookValue = typeof(LootScavengingSettings).GetMethod(nameof(Look), BindingFlags.NonPublic | BindingFlags.Static);

    private static void Look<T>(object owner, FieldInfo field, string key, T defaultValue)
    {
        var value = (T)field.GetValue(owner);
        Scribe_Values.Look(ref value, key, defaultValue, forceSave: true);
        field.SetValue(owner, value);
    }
}

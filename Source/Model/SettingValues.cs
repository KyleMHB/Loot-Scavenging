using System;
using System.Collections.Generic;
using System.Reflection;

namespace LootScavenging.Model;

/// <summary>
/// Every global setting as plain values: what a preset sets and what the page compares against it. The RimWorld settings
/// class copies these in and out. A field added here is copied and compared automatically.
/// </summary>
public sealed class SettingValues
{
    public const float DefaultScrapMarketValue = 0.01f;
    public const float MinScrapMarketValue = 0.01f;
    public const float MaxScrapMarketValue = 10f;

    public LootRules rules = new LootRules();
    public CorpseRules corpses = new CorpseRules();

    // "Don't drop your stuff" (ticket 25, decision 2). They choose where items land; the Who rows choose whether they are
    // sorted. A downed colonist or colony slave keeps his weapon in hand, and a dead one keeps his weapon and pack in the
    // corpse. Colony animals and mechanoids are left to the game, and colony gear is never sorted either way.
    public bool colonistsKeepGear = true;

    // A downed hostile keeps his weapon in hand, and a dead one keeps everything on the body. Off, he drops as in the game
    // and what drops is sorted as it drops.
    public bool hostilesKeepGear = true;

    // The same for non-hostile faction pawns and pawns with no faction.
    public bool nonHostilesKeepGear = true;

    // Kept gear goes with the body when the corpse is destroyed (spec decision 5). On, a cremation bill strips the corpse
    // first, so the gear drops and is judged as at a butcher bill's strip. Off burns it, as in 0.6.0 and the base game.
    public bool cremationStripsCorpses;

    public UnfamiliarScope unfamiliarScope = UnfamiliarScope.AllOutOfReach;

    public RecycleOrStudy recycleOrStudy = RecycleOrStudy.OneOrTheOther;
    public bool studyTechprintProjects;

    // Research points one unit of each tier's scrap pays when studied, before the storyteller's research factor. Zero stops
    // that tier being studied.
    public float studyPointsNeolithic = DefaultStudyPoints(ScrapTier.Neolithic);
    public float studyPointsMedieval = DefaultStudyPoints(ScrapTier.Medieval);
    public float studyPointsIndustrial = DefaultStudyPoints(ScrapTier.Industrial);
    public float studyPointsSpacer = DefaultStudyPoints(ScrapTier.Spacer);
    public float studyPointsUltra = DefaultStudyPoints(ScrapTier.Ultra);
    public float studyPointsArchotech = DefaultStudyPoints(ScrapTier.Archotech);

    // Market value per scrap unit of each tier (ticket 25, decision 9): near zero so stockpiled scrap does not raise wealth
    // or sell for silver.
    public float scrapMarketValueNeolithic = DefaultScrapMarketValue;
    public float scrapMarketValueMedieval = DefaultScrapMarketValue;
    public float scrapMarketValueIndustrial = DefaultScrapMarketValue;
    public float scrapMarketValueSpacer = DefaultScrapMarketValue;
    public float scrapMarketValueUltra = DefaultScrapMarketValue;
    public float scrapMarketValueArchotech = DefaultScrapMarketValue;

    // Settings cut or replaced in 0.6.0 (ticket 25). Non-public, so they are neither saved nor compared; the Def loader
    // still fills them from an old patch, and SettingsLoad translates them into the new settings.
#pragma warning disable CS0649 // filled by the game's Def loader by reflection
    internal string eraFallback;
    internal string scrapMarketValue;
    internal string deathDrop;
    internal string studyEras;
    internal string studyPointsMultiplier;
#pragma warning restore CS0649

    public const float MaxStudyPoints = 1000f;

    // Halved in 0.5.2 with the scrap value (ticket 19): twice the units at half the points keeps the research per item.
    public static float DefaultStudyPoints(ScrapTier tier) => tier switch
    {
        ScrapTier.Neolithic => 5f,
        ScrapTier.Medieval => 7.5f,
        ScrapTier.Industrial => 7.5f,
        ScrapTier.Spacer => 12.5f,
        ScrapTier.Ultra => 17.5f,
        _ => 25f,
    };

    /// <summary>The points set for a tier.</summary>
    public float StudyPoints(ScrapTier tier) => tier switch
    {
        ScrapTier.Neolithic => studyPointsNeolithic,
        ScrapTier.Medieval => studyPointsMedieval,
        ScrapTier.Industrial => studyPointsIndustrial,
        ScrapTier.Spacer => studyPointsSpacer,
        ScrapTier.Ultra => studyPointsUltra,
        _ => studyPointsArchotech,
    };

    public void SetStudyPoints(ScrapTier tier, float points)
    {
        switch (tier)
        {
            case ScrapTier.Neolithic: studyPointsNeolithic = points; break;
            case ScrapTier.Medieval: studyPointsMedieval = points; break;
            case ScrapTier.Industrial: studyPointsIndustrial = points; break;
            case ScrapTier.Spacer: studyPointsSpacer = points; break;
            case ScrapTier.Ultra: studyPointsUltra = points; break;
            default: studyPointsArchotech = points; break;
        }
    }

    /// <summary>Points one unit of a tier pays before the storyteller's research factor; zero when unusable.</summary>
    public float StudyPointsPerUnit(ScrapTier tier)
    {
        float points = StudyPoints(tier);
        return points > 0f && !float.IsInfinity(points) ? points : 0f;
    }

    /// <summary>The market value of one unit of a tier's scrap.</summary>
    public float ScrapMarketValue(ScrapTier tier) => tier switch
    {
        ScrapTier.Neolithic => scrapMarketValueNeolithic,
        ScrapTier.Medieval => scrapMarketValueMedieval,
        ScrapTier.Industrial => scrapMarketValueIndustrial,
        ScrapTier.Spacer => scrapMarketValueSpacer,
        ScrapTier.Ultra => scrapMarketValueUltra,
        _ => scrapMarketValueArchotech,
    };

    public void SetScrapMarketValue(ScrapTier tier, float value)
    {
        switch (tier)
        {
            case ScrapTier.Neolithic: scrapMarketValueNeolithic = value; break;
            case ScrapTier.Medieval: scrapMarketValueMedieval = value; break;
            case ScrapTier.Industrial: scrapMarketValueIndustrial = value; break;
            case ScrapTier.Spacer: scrapMarketValueSpacer = value; break;
            case ScrapTier.Ultra: scrapMarketValueUltra = value; break;
            default: scrapMarketValueArchotech = value; break;
        }
    }

    /// <summary>
    /// Whether a pawn of this group keeps his gear on him when downed or killed ("Don't drop your stuff"). A colony prisoner
    /// follows his faction: a hostile faction's prisoner uses the hostile box, anyone else the non-hostile one.
    /// </summary>
    public bool KeepsGear(OwnerGroup group, bool factionHostile) => group switch
    {
        OwnerGroup.Colony => colonistsKeepGear,
        OwnerGroup.Hostile => hostilesKeepGear,
        OwnerGroup.NonHostileFaction or OwnerGroup.NoFaction => nonHostilesKeepGear,
        _ => factionHostile ? hostilesKeepGear : nonHostilesKeepGear,
    };

    /// <summary>Every scrap tier, low to high.</summary>
    public static readonly ScrapTier[] Tiers = (ScrapTier[])Enum.GetValues(typeof(ScrapTier));

    /// <summary>A deep copy: nothing is shared, so editing the copy never changes the original.</summary>
    public SettingValues Clone() => ValueWalk.Copy(this);

    /// <summary>Every value equal, floats within a small tolerance (a saved float may not round-trip exactly).</summary>
    public bool SameAs(SettingValues other) => other != null && ValueWalk.Equal(this, other);
}

/// <summary>
/// The saved key of every setting, found by walking <see cref="SettingValues"/>, so a new field is saved with no extra code.
/// Keys keep the names configs already use: a field's own name, a category rule's fields prefixed by the category
/// ("weaponsOutcome"), and the corpse toggles prefixed "corpse".
/// </summary>
public static class SettingKeys
{
    private static List<SettingKey> all;

    /// <summary>Every saved leaf: plain values, strings and string lists.</summary>
    public static IReadOnlyList<SettingKey> All => all ??= Build();

    public static string KeyFor(string path)
    {
        string[] parts = path.Split('.');
        string last = parts[parts.Length - 1];
        if (parts.Length == 3 && parts[0] == "rules") // not translated
            return parts[1] + Capitalised(last);
        if (parts.Length == 2 && parts[0] == "corpses") // not translated
            return "corpse" + Capitalised(last); // not translated
        return last;
    }

    internal static bool IsLeaf(Type type) => type.IsValueType || type == typeof(string) || type == typeof(List<string>);

    private static List<SettingKey> Build()
    {
        var keys = new List<SettingKey>();
        Walk(typeof(SettingValues), null, keys);
        return keys;
    }

    private static void Walk(Type type, string prefix, List<SettingKey> into)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            string path = prefix == null ? field.Name : prefix + "." + field.Name;
            if (IsLeaf(field.FieldType))
                into.Add(new SettingKey(path, KeyFor(path)));
            else
                Walk(field.FieldType, path, into);
        }
    }

    private static string Capitalised(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);
}

public readonly struct SettingKey
{
    public SettingKey(string path, string key)
    {
        Path = path;
        Key = key;
    }

    /// <summary>The field path from <see cref="SettingValues"/>, as lock names use it.</summary>
    public string Path { get; }

    public string Key { get; }
}

/// <summary>
/// Copies and compares the settings classes field by field, so the copy and the comparison cannot drift apart when a
/// ticket adds a setting. Handles value types, strings, string lists and nested setting classes.
/// </summary>
internal static class ValueWalk
{
    private const float FloatTolerance = 0.0001f;

    public static T Copy<T>(T source) where T : class => (T)CopyObject(source);

    public static bool Equal(object a, object b)
    {
        if (a == null || b == null)
            return a == b;
        if (a.GetType() != b.GetType())
            return false;
        if (a is float fa)
            return Math.Abs(fa - (float)b) < FloatTolerance;
        if (a is List<string> la)
            return ListsEqual(la, (List<string>)b);
        Type type = a.GetType();
        if (type.IsValueType || a is string)
            return a.Equals(b);
        RequireSupported(type);
        foreach (FieldInfo field in Fields(type))
        {
            if (!Equal(field.GetValue(a), field.GetValue(b)))
                return false;
        }
        return true;
    }

    /// <summary>Whether a dotted field path ("rules.whoHostile") names a field of <paramref name="root"/>.</summary>
    public static bool PathExists(object root, string path)
    {
        Type type = root.GetType();
        foreach (string part in path.Split('.'))
        {
            FieldInfo field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
                return false;
            type = field.FieldType;
        }
        return true;
    }

    /// <summary>Sets the field at a dotted path on <paramref name="target"/> to a copy of the same field on <paramref name="source"/>.</summary>
    public static void CopyPath(object target, object source, string path)
    {
        string[] parts = path.Split('.');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            target = Field(target, parts[i]).GetValue(target);
            source = Field(source, parts[i]).GetValue(source);
        }
        FieldInfo last = Field(target, parts[parts.Length - 1]);
        last.SetValue(target, CopyObject(last.GetValue(source)));
    }

    /// <summary>The object holding the field at a dotted path, and that field.</summary>
    public static FieldInfo Resolve(object root, string path, out object owner)
    {
        string[] parts = path.Split('.');
        owner = root;
        for (int i = 0; i < parts.Length - 1; i++)
            owner = Field(owner, parts[i]).GetValue(owner);
        return Field(owner, parts[parts.Length - 1]);
    }

    private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);

    private static object CopyObject(object source)
    {
        if (source == null)
            return null;
        Type type = source.GetType();
        if (type.IsValueType || source is string)
            return source;
        if (source is List<string> list)
            return new List<string>(list);
        RequireSupported(type);
        object copy = Activator.CreateInstance(type);
        foreach (FieldInfo field in Fields(type))
            field.SetValue(copy, CopyObject(field.GetValue(source)));
        return copy;
    }

    private static bool ListsEqual(List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // Any other collection has no public fields, so the walk would copy it empty and always call it equal.
    private static void RequireSupported(Type type)
    {
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            throw new NotSupportedException("SettingValues cannot copy or compare a " + type.Name + "; only List<string> is supported."); // not translated
    }

    private static FieldInfo[] Fields(Type type) => type.GetFields(BindingFlags.Public | BindingFlags.Instance);
}

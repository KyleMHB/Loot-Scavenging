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

    public LootRules rules = new LootRules();
    public CorpseRules corpses = new CorpseRules();

    // Era fallback for gear with no research era and no tech level (spec: replaces the carrying faction's era).
    public EraFallback eraFallback = EraFallback.CarrierFaction;

    // Market value per scrap unit: near zero so stockpiled scrap does not raise wealth or sell for silver.
    public float scrapMarketValue = DefaultScrapMarketValue;

    // Death-time scrap lands on the ground; false leaves it in the corpse's inventory.
    public bool scrapOnGround = true;

    public UnfamiliarScope unfamiliarScope = UnfamiliarScope.AllOutOfReach;

    public StudyEraLimit studyEras = StudyEraLimit.ColonyEraAndBelow;
    public RecycleOrStudy recycleOrStudy = RecycleOrStudy.OneOrTheOther;
    public bool studyTechprintProjects;

    /// <summary>A deep copy: nothing is shared, so editing the copy never changes the original.</summary>
    public SettingValues Clone() => ValueWalk.Copy(this);

    /// <summary>Every value equal, floats within a small tolerance (a saved float may not round-trip exactly).</summary>
    public bool SameAs(SettingValues other) => other != null && ValueWalk.Equal(this, other);
}

/// <summary>
/// The saved key of every setting, found by walking <see cref="SettingValues"/>, so a new field is saved with no extra code.
/// Keys keep the names configs already use: a field's own name, a category rule's fields prefixed by the category
/// ("weaponsOutcome"), and the corpse toggles prefixed "corpse" (butcherRemovesApparel kept its own name).
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
        if (parts.Length == 2 && parts[0] == "corpses" && last != "butcherRemovesApparel") // not translated
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

    /// <summary>Whether a dotted field path ("rules.dropMode") names a field of <paramref name="root"/>.</summary>
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

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

using System;
using System.Collections.Generic;
using System.Linq;

namespace LootScavenging.Model;

/// <summary>One item the picker can offer: what the player reads and the def name the list stores.</summary>
public readonly struct ItemSearchEntry
{
    public ItemSearchEntry(string label, string defName)
    {
        Label = label ?? "";
        DefName = defName ?? "";
    }

    public string Label { get; }
    public string DefName { get; }
}

/// <summary>Label search for the item picker. Every word typed must appear in the label or def name.</summary>
public static class ItemSearch
{
    /// <summary>
    /// The entries matching <paramref name="query"/>, best first: a label that starts with the first word, then a word
    /// that starts a label word, then the rest, each alphabetically. An empty query returns everything alphabetically.
    /// </summary>
    public static List<ItemSearchEntry> Filter(IEnumerable<ItemSearchEntry> entries, string query)
    {
        string[] words = (query ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return entries
            .Where(e => Matches(e, words))
            .OrderBy(e => Rank(e, words))
            .ThenBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.DefName, StringComparer.Ordinal)
            .ToList();
    }

    private static bool Matches(ItemSearchEntry entry, string[] words)
    {
        foreach (string word in words)
        {
            if (!Contains(entry.Label, word) && !Contains(entry.DefName, word))
                return false;
        }
        return true;
    }

    private static int Rank(ItemSearchEntry entry, string[] words)
    {
        if (words.Length == 0)
            return 0;
        if (entry.Label.StartsWith(words[0], StringComparison.OrdinalIgnoreCase))
            return 0;
        foreach (string labelWord in entry.Label.Split(' '))
        {
            if (labelWord.StartsWith(words[0], StringComparison.OrdinalIgnoreCase))
                return 1;
        }
        return 2;
    }

    private static bool Contains(string text, string word) => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
}

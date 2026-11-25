using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

// Defs/SettingsDefaults.xml is the shipped Progression preset; the compiled-in values are the fallback. They must agree,
// or a game with the Def and one without would start from different defaults. The XML is read the way the game reads a
// Def (field name = element name, enum by name, list by <li>).
public class ShippedDefaultsTests
{
    private static string RepoFile(params string[] parts)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "LoadFolders.xml")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, Path.Combine(parts));
    }

    private static void Fill(object target, XElement node)
    {
        foreach (XElement child in node.Elements())
        {
            // Non-public too, as the game's loader binds them: the old nodes kept for old patches are read that way (ticket 25).
            FieldInfo field = target.GetType().GetField(child.Name.LocalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(field != null, "no field " + child.Name.LocalName + " on " + target.GetType().Name);
            object value;
            if (field.FieldType == typeof(List<string>))
            {
                var list = new List<string>();
                foreach (XElement li in child.Elements("li"))
                    list.Add(li.Value);
                value = list;
            }
            else if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                value = Activator.CreateInstance(field.FieldType);
                Fill(value, child);
            }
            else if (field.FieldType.IsEnum)
                value = Enum.Parse(field.FieldType, child.Value);
            else if (field.FieldType == typeof(bool))
                value = bool.Parse(child.Value);
            else
                value = Convert.ChangeType(child.Value, field.FieldType, System.Globalization.CultureInfo.InvariantCulture);
            field.SetValue(target, value);
        }
    }

    private static XElement Def() => XDocument.Load(RepoFile("Defs", "SettingsDefaults.xml")).Root.Element("LootScavenging.SettingsDefaultsDef");

    [Fact]
    public void TheShippedDefaultsMatchTheCompiledProgressionPreset()
    {
        var values = new SettingValues();
        Fill(values, Def().Element("values"));
        SettingsLoad.MigrateDef(values);
        Assert.True(values.SameAs(Presets.CodeProgression()));
    }

    [Fact]
    public void TheShippedDefaultsLockNothing()
    {
        Assert.Empty(Def().Element("locked").Elements());
    }

    [Fact]
    public void ThePackPresetPatchLocksOnlyNamesThatCanBeLocked()
    {
        XElement patch = XDocument.Load(RepoFile("packs", "progression", "ProgressionPreset.xml")).Root;
        var requested = new List<string>();
        foreach (XElement li in patch.Descendants("li"))
            requested.Add(li.Value);
        Assert.NotEmpty(requested);
        Assert.Empty(new SettingLocks(requested).Unknown);
    }

    [Fact]
    public void ThePackPresetSetsEraTypicalDisassemblyYield()
    {
        XElement patch = XDocument.Load(RepoFile("packs", "progression", "ProgressionPreset.xml")).Root;
        Assert.Contains(patch.Descendants("disassemblyYield"), e => e.Value == "EraTypical");
        Assert.Contains(patch.Descendants("li"), e => e.Value == "rules.disassemblyYield");
    }
}

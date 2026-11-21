using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class ItemSearchTests
{
    private static readonly ItemSearchEntry[] Items =
    {
        new ItemSearchEntry("assault rifle", "Gun_AssaultRifle"),
        new ItemSearchEntry("charge rifle", "Gun_ChargeRifle"),
        new ItemSearchEntry("rifle ammo", "Ammo_Rifle"),
        new ItemSearchEntry("herbal medicine", "MedicineHerbal"),
        new ItemSearchEntry("flak vest", "Apparel_FlakVest"),
    };

    private static string[] Find(string query) => ItemSearch.Filter(Items, query).Select(e => e.DefName).ToArray();

    [Fact]
    public void EmptyQueryReturnsEverythingAlphabetically()
    {
        Assert.Equal(new[] { "Gun_AssaultRifle", "Gun_ChargeRifle", "Apparel_FlakVest", "MedicineHerbal", "Ammo_Rifle" }, Find("  "));
    }

    [Fact]
    public void SearchIsCaseInsensitiveAndPutsLabelStartsFirst()
    {
        Assert.Equal(new[] { "Ammo_Rifle", "Gun_AssaultRifle", "Gun_ChargeRifle" }, Find("RIFLE"));
    }

    [Fact]
    public void EveryWordMustMatch()
    {
        Assert.Equal(new[] { "Gun_ChargeRifle" }, Find("rifle charge"));
    }

    [Fact]
    public void DefNameAlsoMatches()
    {
        Assert.Equal(new[] { "Apparel_FlakVest" }, Find("Apparel_"));
    }

    [Fact]
    public void NoMatchReturnsNothing() => Assert.Empty(Find("zzz"));
}

using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class CarrierErasTests
{
    private static CarrierEras Factions()
    {
        var eras = new CarrierEras();
        eras.AddKind(Era.Neolithic, new[] { "NeolithicMelee" }, new[] { "Neolithic" });
        eras.AddKind(Era.Industrial, new[] { "Gun", "IndustrialMelee" }, new[] { "IndustrialBasic" });
        eras.AddKind(Era.Spacer, new[] { "SpacerGun", "Gun" }, new[] { "SpacerMilitary" });
        return eras;
    }

    [Fact]
    public void GearCarriedByOneFactionTakesItsEra()
    {
        Assert.Equal(Era.Neolithic, Factions().For(new[] { "NeolithicMelee" }, null));
        Assert.Equal(Era.Spacer, Factions().For(null, new[] { "SpacerMilitary" }));
    }

    // An item kept whole because its carrier was above the colony must not then read as familiar, so the highest wins.
    [Fact]
    public void GearSeveralFactionsCarryTakesTheHighestEra()
    {
        Assert.Equal(Era.Spacer, Factions().For(new[] { "Gun" }, null));
        Assert.Equal(Era.Industrial, Factions().For(new[] { "IndustrialMelee" }, new[] { "Neolithic", "IndustrialBasic" }));
    }

    [Fact]
    public void GearNoFactionCarriesHasNoEra()
    {
        Assert.Equal(Era.Undefined, Factions().For(new[] { "Unknown" }, new string[0]));
        Assert.Equal(Era.Undefined, Factions().For(null, null));
    }

    [Fact]
    public void KindsWithNoEraAreIgnored()
    {
        var eras = new CarrierEras();
        eras.AddKind(Era.Undefined, new[] { "Gun" }, null);
        Assert.Equal(Era.Undefined, eras.For(new[] { "Gun" }, null));
    }
}

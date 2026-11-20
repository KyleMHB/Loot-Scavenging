using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class OutcomeTests
{
    [Fact]
    public void KeepIsTheDefaultOutcome()
    {
        Assert.Equal(Outcome.Keep, default(Outcome));
    }
}

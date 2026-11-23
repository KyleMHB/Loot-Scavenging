using System.Collections.Generic;
using System.Linq;
using RimWorldDevTests;
using Verse;

namespace LootScavenging.DevTests;

// Startup errors. The runner counts errors only from the first batch on, which is how a startup NullReferenceException
// once went uncounted; this batch reads the game's whole in-memory log instead.
public sealed class StartupBatch : Batch
{
    private const string Clean = "lootscavenging.startup.startup-log-clean";

    public override string Id => "lootscavenging-startup";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Clean, Levels.Mechanical, "the log from startup to this batch holds no error naming the mod or its namespace"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        yield return Wait.Ticks(1);
        var c = ctx.Begin(Clean);
        var errors = Log.Messages.Where(m => m.type == LogMessageType.Error
            && ((m.text ?? "").Contains("Loot Scavenging") || (m.text ?? "").Contains("LootScavenging") || (m.StackTrace ?? "").Contains("LootScavenging")))
            .Where(m => !(m.text ?? "").Contains("[Loot Scavenging tests]")).ToList();
        // The game keeps the newest 1000 messages (LogMessageQueue.maxMessages) and drops the oldest, so a full queue
        // may no longer hold startup.
        int kept = Log.Messages.Count();
        c.Observe("errors", errors.Count).Observe("messagesKept", kept);
        foreach (var e in errors.Take(3)) c.Note("error: " + e.text.Split('\n')[0]);
        if (kept >= 1000)
            c.NotApplicable("the game's log is full (1000 messages), so startup may have been dropped; run this batch first in a fresh launch");
        else
        {
            c.Step(errors.Count == 0, "no error naming the mod was logged at startup");
            c.Conclude();
        }
    }
}

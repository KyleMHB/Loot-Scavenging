using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The first-load message on the real startup path (ticket 11). The message is queued from the mod's constructor and shown
// once ever, at a launch whose settings file has the flag unset. The launcher deletes every Mod_*.xml before each launch
// (rimtest.py, "Mod settings start at defaults"), so every harness launch is such a launch, and a reused session's log
// still holds its startup. The runner counts errors only from the first batch on, which is how a startup
// NullReferenceException in ShowFirstLoadMessage went uncounted; this batch reads the game's whole in-memory log instead.
//
// Reads the settings file while mods are constructed, before the first-load message can be queued, so the batch knows
// whether this launch started with the flag unset. A file write time is not enough: the file is also written at other
// points of a launch. Developer-only; the class has no settings page.
public sealed class FirstLoadLaunchState : Mod
{
    public static bool? FlagAtLaunch;

    public FirstLoadLaunchState(ModContentPack content) : base(content)
    {
        var file = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*_LootScavengingMod.xml").FirstOrDefault();
        FlagAtLaunch = file != null && File.ReadAllText(file).Contains("<firstLoadMessageShown>True</firstLoadMessageShown>");
    }
}

public sealed class FirstLoadBatch : Batch
{
    private const string P = "lootscavenging.firstload.";
    private const string Startup = P + "message-at-startup", Clean = P + "startup-log-clean";

    public override string Id => "lootscavenging-firstload";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Startup, Levels.Mechanical, "with the flag unset at launch (read while mods are constructed), startup showed the message and wrote the flag before the first batch"),
        new PlannedCase(Clean, Levels.Mechanical, "the log from startup to this batch holds no error naming the mod or its namespace"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        yield return Wait.Ticks(1);
        var file = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*_LootScavengingMod.xml").FirstOrDefault();
        DateTime started = Process.GetCurrentProcess().StartTime;
        DateTime? written = file != null ? File.GetLastWriteTime(file) : (DateTime?)null;
        bool? flagAtLaunch = FirstLoadLaunchState.FlagAtLaunch;
        bool shownAtStartup = flagAtLaunch == false;

        {
            var c = ctx.Begin(Startup);
            c.Observe("settingsFile", file != null ? Path.GetFileName(file) : "none").Observe("processStarted", started.ToString("HH:mm:ss"))
                .Observe("settingsWritten", written?.ToString("HH:mm:ss") ?? "never").Observe("flagAtLaunch", flagAtLaunch?.ToString() ?? "not read")
                .Observe("flag", Settings.firstLoadMessageShown);
            if (!shownAtStartup)
                c.NotApplicable("the flag was already set at this launch (a launch outside the harness launcher)");
            else
            {
                c.Step(Settings.firstLoadMessageShown && written.HasValue && written.Value >= started,
                    "the launch started with the flag unset; startup showed the message, set the flag and wrote the settings file");
                c.Conclude();
            }
        }

        {
            var c = ctx.Begin(Clean);
            var errors = Log.Messages.Where(m => m.type == LogMessageType.Error
                && ((m.text ?? "").Contains("Loot Scavenging") || (m.text ?? "").Contains("LootScavenging") || (m.StackTrace ?? "").Contains("LootScavenging")))
                .Where(m => !(m.text ?? "").Contains("[Loot Scavenging tests]")).ToList();
            // The game keeps the newest 1000 messages (LogMessageQueue.maxMessages) and drops the oldest, so a full queue
            // may no longer hold startup.
            int kept = Log.Messages.Count();
            c.Observe("errors", errors.Count).Observe("shownAtStartup", shownAtStartup).Observe("messagesKept", kept);
            foreach (var e in errors.Take(3)) c.Note("error: " + e.text.Split('\n')[0]);
            if (kept >= 1000)
                c.NotApplicable("the game's log is full (1000 messages), so startup may have been dropped; run this batch first in a fresh launch");
            else
            {
                c.Step(errors.Count == 0, shownAtStartup
                    ? "no error naming the mod was logged at a startup that showed the first-load message"
                    : "no error naming the mod was logged at startup");
                c.Conclude();
            }
        }
    }
}

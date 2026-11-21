using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace LootScavengingRemovalProbe;

// Developer-only mod for the save-removal check; never shipped. Adapted from Spearfishing's probe. With Loot Scavenging turned
// off, it loads the save named by -removalProbeSave=<name>, lets the game run a few hundred ticks, writes what the map kept to
// -removalProbeOut=<file> and quits. Without those arguments it does nothing. tools/removal-check/removal_check.py drives it.
// The save comes from the lootscavenging-saves batch: industrial scrap, studied scrap, a research bench with a study bill, a
// machining table with a vanilla bill and a recycle bill, and a looted charge rifle with the unfamiliar marker.
[StaticConstructorOnStartup]
public static class RemovalProbe
{
    internal const int RunTicks = 600, FrameLimit = 20000;

    private static readonly string save = Arg("removalProbeSave");
    private static readonly string output = Arg("removalProbeOut");

    static RemovalProbe()
    {
        if (save == null || output == null) return;
        Log.Message("[RemovalProbe] loading save " + save); // not translated
        // Waylith skips every frame while the window is unfocused unless run-in-background is on, and Prefs.Apply resets the
        // engine flag from prefs a few frames in. The probe's save-data folder is its own, so setting the pref is safe.
        Prefs.RunInBackground = true;
        UnityEngine.Application.runInBackground = true;
        LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(save));
    }

    public static bool Active => save != null && output != null;

    private static string Arg(string name)
    {
        string prefix = "-" + name + "=";
        return Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?.Substring(prefix.Length);
    }

    private static void Check(StringBuilder sb, bool holds, string what) => sb.AppendLine((holds ? "PASS " : "FAIL ") + what);

    internal static void Report(int ranTicks, string note)
    {
        var sb = new StringBuilder();
        sb.AppendLine("save: " + save);
        sb.AppendLine("note: " + note);
        sb.AppendLine("ticksRun: " + ranTicks);
        sb.AppendLine("activeMods: " + string.Join(", ", ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageId)));
        var map = Find.Maps.FirstOrDefault(m => m.IsPlayerHome) ?? Find.CurrentMap;
        Check(sb, map != null && ranTicks >= RunTicks, $"the save loaded and ran {RunTicks} ticks");
        if (map != null)
        {
            var benches = map.listerBuildings.allBuildingsColonist.Where(b => b.def.defName == "SimpleResearchBench").ToList();
            sb.AppendLine("researchBenches: " + string.Join(", ", benches.Select(b => b.GetType().FullName)));
            Check(sb, benches.Count > 0 && benches.All(b => b.GetType() == typeof(Building_ResearchBench)), "the research bench loaded as a plain research bench");

            var tables = map.listerBuildings.allBuildingsColonist.OfType<Building_WorkTable>().Where(b => b.def.defName == "TableMachining").ToList();
            var bills = tables.SelectMany(t => t.BillStack.Bills).ToList();
            sb.AppendLine("machiningTableBills: " + string.Join(", ", bills.Select(b => b.recipe?.defName ?? "null recipe")));
            Check(sb, tables.Count > 0, "the machining table is still there");
            Check(sb, bills.Any(b => b.recipe?.defName == "Make_ComponentIndustrial"), "its vanilla bill survived");
            Check(sb, bills.All(b => b.recipe != null && !b.recipe.defName.StartsWith("LS_")), "no recycle bill or broken bill is left on it");

            var items = map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).ToList();
            sb.AppendLine("items: " + string.Join(", ", items.GroupBy(t => t.def.defName).Select(g => g.Key + " x" + g.Sum(t => t.stackCount))));
            Check(sb, items.All(t => !t.def.defName.StartsWith("LS_")), "no scrap or studied scrap is left");
            var rifles = items.Where(t => t.def.defName == "Gun_ChargeRifle").ToList();
            var colonist = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
            Check(sb, rifles.Count >= 1, "the looted charge rifle is still there");
            Check(sb, rifles.Count >= 1 && colonist != null && rifles.All(r => EquipmentUtility.CanEquip(r, colonist)), "it can be equipped (the unfamiliar marker is gone)");
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                sb.AppendLine($"  colonist {pawn.LabelShort}: job {pawn.CurJobDef?.defName ?? "none"}");
        }
        var messages = Log.Messages.ToList();
        var errors = messages.Where(m => m.type == LogMessageType.Error).ToList();
        sb.AppendLine($"log: {errors.Count} errors, {messages.Count(m => m.type == LogMessageType.Warning)} warnings");
        foreach (var m in messages.Where(m => m.type != LogMessageType.Message))
        {
            string text = m.text.Length > 600 ? m.text.Substring(0, 600) + " ..." : m.text;
            sb.AppendLine($"  [{m.type}] x{m.repeats} {text.Replace("\r", "").Replace("\n", " | ")}");
        }
        File.WriteAllText(output, sb.ToString());
        Root.Shutdown();
    }

    internal static string Output => output;
}

// Runs the probe's clock once the save is playing. A game component is added to every loaded game, so it needs no save data.
public class RemovalProbeComponent : GameComponent
{
    private int startTick = -1, frames;
    private bool done;

    public RemovalProbeComponent(Game game)
    {
    }

    public override void GameComponentUpdate()
    {
        if (!RemovalProbe.Active || done) return;
        UnityEngine.Application.runInBackground = true;
        if (Current.ProgramState != ProgramState.Playing) return;
        frames++;
        if (startTick < 0)
        {
            startTick = Find.TickManager.TicksGame;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        }
        int ran = Find.TickManager.TicksGame - startTick;
        if (ran < RemovalProbe.RunTicks && frames < RemovalProbe.FrameLimit) return;
        done = true;
        try
        {
            RemovalProbe.Report(ran, ran < RemovalProbe.RunTicks ? "frame limit reached first" : "ok"); // not translated
        }
        catch (Exception e)
        {
            File.WriteAllText(RemovalProbe.Output + ".error", e.ToString());
            Root.Shutdown();
        }
    }
}

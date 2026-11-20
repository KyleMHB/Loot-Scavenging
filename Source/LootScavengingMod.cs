using HarmonyLib;
using Verse;

namespace LootScavenging;

public class LootScavengingMod : Mod
{
    public const string HarmonyId = "kylemhb.lootscavenging";

    public static LootScavengingSettings Settings { get; private set; }

    public LootScavengingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LootScavengingSettings>();
        new Harmony(HarmonyId).PatchAll(typeof(LootScavengingMod).Assembly);
    }

    public override string SettingsCategory() => Content.Name;
}

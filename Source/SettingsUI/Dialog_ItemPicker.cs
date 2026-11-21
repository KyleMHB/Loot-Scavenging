using System.Collections.Generic;
using LootScavenging.Model;
using UnityEngine;
using Verse;

namespace LootScavenging.SettingsUI;

/// <summary>
/// The item picker: a search box over the loadable items, each with an Add button for one list. It edits the list in
/// place and stays open so several items can be added; entries are def names.
/// </summary>
internal sealed class Dialog_ItemPicker : Window
{
    private const float RowHeight = 28f;
    private const int MaxShown = 200;

    // Built once per game session: the loaded items do not change while the game runs.
    private static List<ItemSearchEntry> allItems;

    private readonly List<string> list;
    private readonly string title;
    private string query = "";
    private List<ItemSearchEntry> shown;
    private Vector2 scroll;

    public Dialog_ItemPicker(string title, List<string> list)
    {
        this.title = title;
        this.list = list;
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
        forcePause = false;
        Refilter();
    }

    public override Vector2 InitialSize => new Vector2(520f, 640f);

    private static List<ItemSearchEntry> AllItems()
    {
        if (allItems != null)
            return allItems;
        allItems = new List<ItemSearchEntry>();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            if (IsLootLike(def))
                allItems.Add(new ItemSearchEntry(def.LabelCap.Resolve(), def.defName));
        }
        return allItems;
    }

    // Things that can lie in a pawn's inventory or be worn or wielded; buildings, plants and pawns are not loot.
    private static bool IsLootLike(ThingDef def)
    {
        if (def.category != ThingCategory.Item || string.IsNullOrEmpty(def.label) || def.IsCorpse)
            return false;
        return def.EverHaulable || def.IsWeapon || def.IsApparel;
    }

    private void Refilter() => shown = ItemSearch.Filter(AllItems(), query);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), title);
        Text.Font = GameFont.Small;

        string before = query;
        query = Widgets.TextField(new Rect(inRect.x, inRect.y + 36f, inRect.width, 28f), query);
        if (query != before)
            Refilter();

        string count = shown.Count > MaxShown
            ? "LootScavenging_PickerFirstShown".Translate(MaxShown, shown.Count).Resolve()
            : "LootScavenging_PickerCount".Translate(shown.Count).Resolve();
        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(inRect.x, inRect.y + 66f, inRect.width, 20f), count);
        Text.Font = GameFont.Small;

        var outRect = new Rect(inRect.x, inRect.y + 90f, inRect.width, inRect.height - 90f - 44f);
        int rows = shown.Count > MaxShown ? MaxShown : shown.Count;
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, rows * RowHeight);
        Widgets.BeginScrollView(outRect, ref scroll, viewRect);
        // Only the rows in view are drawn: the full item list runs to thousands with big mod packs.
        int first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / RowHeight));
        int last = Mathf.Min(rows, first + Mathf.CeilToInt(outRect.height / RowHeight) + 1);
        for (int i = first; i < last; i++)
            DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), shown[i]);
        Widgets.EndScrollView();
    }

    private void DrawRow(Rect row, ItemSearchEntry item)
    {
        if (Mouse.IsOver(row))
            Widgets.DrawHighlight(row);
        var buttonRect = new Rect(row.xMax - 80f, row.y + 2f, 80f, RowHeight - 4f);
        Widgets.Label(new Rect(row.x + 4f, row.y + 3f, row.width - 90f, RowHeight), item.Label + " (" + item.DefName + ")");
        if (list.Contains(item.DefName))
            Widgets.Label(buttonRect, "LootScavenging_PickerOnList".Translate());
        else if (Widgets.ButtonText(buttonRect, "LootScavenging_ListAdd".Translate()))
            list.Add(item.DefName);
    }
}

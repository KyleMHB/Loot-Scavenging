using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LootScavenging;

/// <summary>
/// A research bench that can hold bills, so study is bench work with the usual bill controls (spec, "Requirements
/// learned from Technology Blueprints"). Vanilla research benches are not bill givers; <see cref="StudyRecipes"/> swaps
/// this class in for every def whose thingClass is exactly <see cref="Building_ResearchBench"/>, so research itself is
/// unchanged. Benches with their own subclass from another mod keep it and get no study bills.
/// </summary>
public class Building_StudyBench : Building_ResearchBench, IBillGiver
{
    public BillStack billStack;

    private CompPowerTrader powerComp;
    private CompBreakdownable breakdownableComp;

    public Building_StudyBench()
    {
        billStack = new BillStack(this);
    }

    public BillStack BillStack => billStack;

    public IEnumerable<IntVec3> IngredientStackCells => GenAdj.CellsOccupiedBy(this);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref billStack, "billStack", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && billStack == null)
            billStack = new BillStack(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        breakdownableComp = GetComp<CompBreakdownable>();
        foreach (Bill bill in billStack)
            bill.ValidateSettings();
    }

    public bool CurrentlyUsableForBills() => UsableForBillsAfterFueling();

    public bool UsableForBillsAfterFueling()
    {
        if (powerComp != null && !powerComp.PowerOn)
            return false;
        return breakdownableComp == null || !breakdownableComp.BrokenDown;
    }

    public void Notify_BillDeleted(Bill bill)
    {
    }
}

/// <summary>
/// The bills tab for <see cref="Building_StudyBench"/>. The game's own tab casts its target to a work table, so the bench
/// needs this one: the bill list and an add-bill menu of the bench's recipes.
/// </summary>
public class ITab_StudyBills : ITab
{
    private static readonly Vector2 WinSize = new Vector2(420f, 480f);

    private float viewHeight = 1000f;
    private Vector2 scrollPosition;
    private Bill mouseoverBill;

    public ITab_StudyBills()
    {
        size = WinSize;
        labelKey = "TabBills";
        tutorTag = "Bills";
    }

    private Building_StudyBench SelBench => SelThing as Building_StudyBench;

    public override bool IsVisible => SelBench != null && base.IsVisible;

    protected override void FillTab()
    {
        Building_StudyBench bench = SelBench;
        if (bench == null)
            return;
        DoPasteButton(bench);
        Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
        mouseoverBill = bench.billStack.DoListing(rect, () => Options(bench), ref scrollPosition, ref viewHeight);
    }

    // The game's paste button for a copied bill (its ITab_Bills), in the same place. Its layout numbers are private there.
    private const float PasteX = 48f;
    private const float PasteY = 3f;
    private const float PasteSize = 24f;
    private const int BillLimit = 15;

    private static void DoPasteButton(Building_StudyBench bench)
    {
        Bill clipboard = BillUtility.Clipboard;
        if (clipboard == null)
            return;
        Rect rect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
        if (!bench.def.AllRecipes.Contains(clipboard.recipe) || !clipboard.recipe.AvailableNow || !clipboard.recipe.AvailableOnNow(bench))
        {
            DoGreyedPaste(rect, "ClipboardBillNotAvailableHere".Translate() + ": " + clipboard.LabelCap);
        }
        else if (bench.billStack.Count >= BillLimit)
        {
            DoGreyedPaste(rect, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + "): " + clipboard.LabelCap);
        }
        else
        {
            if (Widgets.ButtonImageFitted(rect, TexButton.Paste, Color.white))
            {
                Bill bill = clipboard.Clone();
                bill.InitializeAfterClone();
                bench.billStack.AddBill(bill);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            if (Mouse.IsOver(rect))
                TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + ": " + clipboard.LabelCap);
        }
    }

    private static void DoGreyedPaste(Rect rect, string tip)
    {
        GUI.color = Color.gray;
        Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
        GUI.color = Color.white;
        if (Mouse.IsOver(rect))
            TooltipHandler.TipRegion(rect, tip);
    }

    private static List<FloatMenuOption> Options(Building_StudyBench bench)
    {
        var options = new List<FloatMenuOption>();
        List<RecipeDef> recipes = bench.def.AllRecipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeDef recipe = recipes[i];
            if (!recipe.AvailableNow || !recipe.AvailableOnNow(bench))
                continue;
            options.Add(new FloatMenuOption(recipe.LabelCap, () =>
            {
                if (!bench.Map.mapPawns.FreeColonists.Any(col => recipe.PawnSatisfiesSkillRequirements(col)))
                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                bench.billStack.AddBill(recipe.MakeNewBill());
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }));
        }
        if (options.Count == 0)
            options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
        return options;
    }

    public override void TabUpdate()
    {
        if (mouseoverBill != null && SelBench != null)
        {
            mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelBench.Position);
            mouseoverBill = null;
        }
    }
}

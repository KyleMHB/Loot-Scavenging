using System;
using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// The two era functions everything era-related calls (spec, "Loot rules"): <see cref="ColonyEra"/> and <see cref="ItemEra"/>.
/// The ranking itself is plain C# in Model/EraRanking.cs.
/// </summary>
public static class EraReader
{
    /// <summary>
    /// Integration point: the Progression pack (or any mod) sets this to supply the colony's era.
    /// Return null to fall back to the research-based value.
    /// </summary>
    public static Func<Era?> ColonyEraOverride;

    /// <summary>
    /// The colony's current era. Vanilla has no such value (Faction.OfPlayer.def.techLevel is static), so it is computed:
    /// the highest tech level among finished research projects (those with a research cost; Anomaly knowledge projects
    /// are skipped), never below Animal. Computed per call; it is a dictionary lookup per project.
    /// </summary>
    public static Era ColonyEra()
    {
        Era? supplied = ColonyEraOverride?.Invoke();
        if (supplied.HasValue)
        {
            return supplied.Value;
        }

        return EraRanking.ColonyEra(FinishedProjectEras());
    }

    /// <summary>
    /// An item's era and whether it is out of reach, using the settings' era fallback. The carrier is the faction of the
    /// pawn that held it, or null when unknown.
    /// </summary>
    public static EraResult ItemEra(ThingDef def, Faction carrier = null)
    {
        return ItemEra(def, carrier, LootScavengingMod.Settings?.eraFallback ?? EraFallback.CarrierFaction);
    }

    public static EraResult ItemEra(ThingDef def, Faction carrier, EraFallback fallback)
    {
        Era researchEra = Era.Undefined;
        bool unfinished = false;
        List<ResearchProjectDef> projects = PrerequisitesOf(def);
        for (int i = 0; i < projects.Count; i++)
        {
            ResearchProjectDef project = projects[i];
            if (!project.IsFinished)
            {
                unfinished = true;
            }
            // Several prerequisites: the highest one sets the item's era.
            if ((Era)project.techLevel > researchEra)
            {
                researchEra = (Era)project.techLevel;
            }
        }

        Era factionEra = carrier?.def != null ? (Era)carrier.def.techLevel : Era.Undefined;
        return EraRanking.Resolve(researchEra, unfinished, (Era)def.techLevel, factionEra, fallback, ColonyEra());
    }

    private static IEnumerable<Era> FinishedProjectEras()
    {
        List<ResearchProjectDef> all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
        for (int i = 0; i < all.Count; i++)
        {
            ResearchProjectDef project = all[i];
            if (project.baseCost > 0f && project.IsFinished)
            {
                yield return (Era)project.techLevel;
            }
        }
    }

    // Prerequisites per item, from the item itself (recipeMaker, buildable prerequisites) or else the first recipe that
    // produces it and names research. Defs do not change after load, so the recipe index is built once.
    private static Dictionary<ThingDef, List<ResearchProjectDef>> recipeIndex;
    private static readonly List<ResearchProjectDef> None = new List<ResearchProjectDef>();

    private static List<ResearchProjectDef> PrerequisitesOf(ThingDef def)
    {
        var own = new List<ResearchProjectDef>();
        AddProject(own, def.recipeMaker?.researchPrerequisite);
        AddProjects(own, def.recipeMaker?.researchPrerequisites);
        AddProjects(own, def.researchPrerequisites);
        if (own.Count > 0)
        {
            return own;
        }

        if (recipeIndex == null)
        {
            BuildRecipeIndex();
        }
        return recipeIndex.TryGetValue(def, out List<ResearchProjectDef> found) ? found : None;
    }

    private static void BuildRecipeIndex()
    {
        recipeIndex = new Dictionary<ThingDef, List<ResearchProjectDef>>();
        List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeDef recipe = recipes[i];
            if (recipe.products == null)
            {
                continue;
            }
            var projects = new List<ResearchProjectDef>();
            AddProject(projects, recipe.researchPrerequisite);
            AddProjects(projects, recipe.researchPrerequisites);
            if (projects.Count == 0)
            {
                continue;
            }
            for (int p = 0; p < recipe.products.Count; p++)
            {
                ThingDef product = recipe.products[p].thingDef;
                if (product != null && !recipeIndex.ContainsKey(product))
                {
                    recipeIndex[product] = projects;
                }
            }
        }
    }

    private static void AddProject(List<ResearchProjectDef> list, ResearchProjectDef project)
    {
        if (project != null && !list.Contains(project))
        {
            list.Add(project);
        }
    }

    private static void AddProjects(List<ResearchProjectDef> list, List<ResearchProjectDef> projects)
    {
        if (projects == null)
        {
            return;
        }
        for (int i = 0; i < projects.Count; i++)
        {
            AddProject(list, projects[i]);
        }
    }
}

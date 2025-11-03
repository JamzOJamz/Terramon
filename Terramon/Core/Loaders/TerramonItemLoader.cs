using System.Reflection;
using Terramon.Content.Items;
using Terraria.ModLoader.Core;

namespace Terramon.Core.Loaders;

[AttributeUsage(AttributeTargets.Class)]
public class LoadGroupAttribute(string group) : Attribute
{
    public string Group { get; } = group;
}

[AttributeUsage(AttributeTargets.Class)]
public class LoadWeightAttribute(float weight) : Attribute
{
    public float Weight { get; } = weight;
}

[Autoload(false)]
public class TerramonItemLoader : ModSystem
{
    private enum LoadGroup
    {
        Apricorns,
        PokeBalls,
        EvolutionaryItems,
        Vitamins,
        HeldItems,
        KeyItems,
        Interactive,
        MusicBoxes,
        PokeBallMinis,
        TrainerVanity,
    }

    public override void OnModLoad()
    {
        // Load all item types across all mods loaded
        var items = (from t in AssemblyManager.GetLoadableTypes(Mod.Code)
            where !t.IsAbstract && t.IsSubclassOf(typeof(TerramonItem)) && t.GetConstructor(Type.EmptyTypes) != null &&
                  !t.GetCustomAttributes<AutoloadAttribute>(false).Any()
            select (ModItem)Activator.CreateInstance(t, null));

        // Group items by LoadGroup, sort each group by LoadWeight, and then flatten the result
        var sortedItems = items
            .GroupBy(item =>
            {
                var loadGroupAttribute = item.GetType().GetCustomAttribute<LoadGroupAttribute>();
                if (loadGroupAttribute == null)
                    return int.MaxValue; // Items without a LoadGroup load last

                // Find the index of the LoadGroup in TerramonItemAPI.LoadGroups
                if (!Enum.TryParse<LoadGroup>(loadGroupAttribute.Group, out var index))
                    return (int?)null; // Items with no matching LoadGroup should not be loaded

                return (int)index;
            })
            .Where(group => group.Key.HasValue) // Filter out items without a valid LoadGroup
            .OrderBy(group => group.Key.Value) // Sort groups by their index, with no-group items at the end
            .SelectMany(group => group
                .OrderBy(item =>
                {
                    var loadWeightAttribute = item.GetType().GetCustomAttribute<LoadWeightAttribute>();
                    // Default to a weight of 0 if no LoadWeight attribute is present, which will sort the item first
                    return loadWeightAttribute?.Weight ?? 0;
                })
            );

        // Add sorted items to the mod content
        foreach (var item in sortedItems) Mod.AddContent(item);
    }
}
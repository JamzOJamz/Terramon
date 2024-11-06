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

public class TerramonItemLoader : ModSystem
{
    private static readonly List<string> LoadGroupList = [
        "Apricorns",
        "PokeBalls",
        "EvolutionaryItems",
        "Vitamins",
        "KeyItems",
        "MusicBoxes",
        "PokeBallMinis",
        "TrainerVanity"
    ];
    
    public override void OnModLoad()
    {
        // Load all item types across all mods loaded
        var items = (from t in AssemblyManager.GetLoadableTypes(Mod.Code)
            where !t.IsAbstract && t.IsSubclassOf(typeof(TerramonItem)) &&
                  t.GetCustomAttributes<AutoloadAttribute>(true).FirstOrDefault()?.Value == false
            select (ModItem)Activator.CreateInstance(t, null)).ToList();

        // Log all items found
        foreach (var item in items) Mod.Logger.Debug($"Found item: {item.GetType().Name}");

        // Group items by LoadGroup, sort each group by LoadWeight, and then flatten the result
        var sortedItems = items
            .GroupBy(item =>
            {
                var loadGroupAttribute = item.GetType().GetCustomAttribute<LoadGroupAttribute>();
                if (loadGroupAttribute == null)
                    return int.MaxValue; // Items without a LoadGroup load last

                // Find the index of the LoadGroup in TerramonItemAPI.LoadGroups
                var index = LoadGroupList.IndexOf(loadGroupAttribute.Group);
                if (index == -1)
                    throw new InvalidOperationException(
                        $"Item {item.GetType().Name} has an invalid LoadGroup attribute: {loadGroupAttribute.Group}");

                return index;
            })
            .OrderBy(group => group.Key) // Sort groups by their index, with no-group items at the end
            .SelectMany(group => group
                .OrderBy(item =>
                {
                    var loadWeightAttribute = item.GetType().GetCustomAttribute<LoadWeightAttribute>();
                    // Default to a weight of 0 if no LoadWeight attribute is present, which will sort the item first
                    return loadWeightAttribute?.Weight ?? 0;
                })
            )
            .ToList();

        // Add sorted items to the mod content
        foreach (var item in sortedItems) Mod.AddContent(item);
    }
}
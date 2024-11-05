using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terramon.Content.Items;
using Terraria.ModLoader.Core;

namespace Terramon.Core.Loaders;

/// <summary>
///     Allows defining dependencies between manually loaded <see cref="TerramonItem" /> types, ensuring that items are
///     loaded in the correct order.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LoadAfterAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

public class TerramonItemLoader : ModSystem
{
    public override void OnModLoad()
    {
        // Load all item types
        var items = (from t in AssemblyManager.GetLoadableTypes(Mod.Code)
            where !t.IsAbstract && t.IsSubclassOf(typeof(TerramonItem)) &&
                  t.GetCustomAttributes<AutoloadAttribute>(true).FirstOrDefault()?.Value == false
            select (TerramonItem)Activator.CreateInstance(t, null)).ToList();

        // Sort items by LoadAfter dependencies using topological sort
        var sortedItems = TopologicalSort(items);

        // Add sorted items to the mod content
        foreach (var item in sortedItems) Mod.AddContent(item);
    }

    private static List<TerramonItem> TopologicalSort(List<TerramonItem> items)
    {
        var itemDictionary = items.ToDictionary(item => item.GetType(), item => item);
        var sorted = new List<TerramonItem>();
        var visited = new HashSet<Type>();
        var tempMarked = new HashSet<Type>();

        // Perform DFS for each item that has dependencies
        foreach (var item in items)
            if (HasDependencies(item))
                Visit(item.GetType(), itemDictionary, sorted, visited, tempMarked);

        // Add items with no dependencies to the end of the sorted list, ensuring no duplicates
        sorted.AddRange(items.Where(item => !visited.Contains(item.GetType())));

        return sorted;
    }

    private static void Visit(Type itemType, IReadOnlyDictionary<Type, TerramonItem> itemDictionary,
        ICollection<TerramonItem> sorted, ISet<Type> visited, ISet<Type> tempMarked)
    {
        if (visited.Contains(itemType))
            return;

        if (!tempMarked.Add(itemType))
            throw new InvalidOperationException($"Cyclic dependency detected involving {itemType.Name}");

        // Check for dependencies via LoadAfterAttribute
        var loadAfterAttributes = itemType.GetCustomAttributes<LoadAfterAttribute>();
        foreach (var attribute in loadAfterAttributes)
            if (attribute.Type.IsAbstract)
            {
                // If the dependency is an abstract class, find all concrete types that inherit from it
                var subclasses = itemDictionary.Keys.Where(t => t.IsSubclassOf(attribute.Type));
                foreach (var subclass in subclasses) Visit(subclass, itemDictionary, sorted, visited, tempMarked);
            }
            else
            {
                // If the dependency is a concrete class, process it directly
                if (itemDictionary.TryGetValue(attribute.Type, out var dependency))
                    Visit(dependency.GetType(), itemDictionary, sorted, visited, tempMarked);
            }

        tempMarked.Remove(itemType);
        visited.Add(itemType);
        sorted.Add(itemDictionary[itemType]);
    }

    private static bool HasDependencies(IModType item)
    {
        // Check if the item has any LoadAfter attributes, indicating dependencies
        return item.GetType().GetCustomAttributes<LoadAfterAttribute>().Any();
    }
}
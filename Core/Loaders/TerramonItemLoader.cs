using System;
using System.Linq;
using Terramon.Content.Items;

namespace Terramon.Core.Loaders;

public class TerramonItemLoader : ModSystem
{
    public override void OnModLoad()
    {
        var items = (from t in Mod.Code.GetTypes() where !t.IsAbstract && t.IsSubclassOf(typeof(TerramonItem)) select (TerramonItem)Activator.CreateInstance(t, null)).ToList();

        // Sort items by the priority property
        var sortedItems = items.OrderBy(item => item.LoadPriority);

        // Add sorted items to the mod content
        foreach (var item in sortedItems)
        {
            Mod.AddContent(item);
        }
    }
}
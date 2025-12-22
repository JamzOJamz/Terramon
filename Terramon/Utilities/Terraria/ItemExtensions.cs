using Terramon.Utilities.Xna;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Chat;

namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for Terraria's <see cref="Item" /> class.
/// </summary>
public static class ItemExtensions
{
    /// <summary>
    ///     Returns the item name normalized, prefixed with an item chat tag, and with the color of its rarity.
    /// </summary>
    public static string PrettyName(this Item i, bool itemIcon = true, bool stack = false)
    {
        var oldStack = i.stack;
        i.stack = 1;

        var rarityColor = i.rare switch
        {
            ItemRarityID.Expert => Main.DiscoColor,
            ItemRarityID.Master => new Color(255, (byte)(Main.masterColor * 200f), 0),
            >= ItemRarityID.Count => RarityLoader.GetRarity(i.rare).RarityColor,
            _ => ItemRarity._rarities.GetValueOrDefault(i.rare, Color.White)
        };

        var result = (itemIcon ? ItemTagHandler.GenerateTag(i) + ' ' : string.Empty) +
                     $"[c/{rarityColor.ToHexString()}:{i.Name}{(stack ? $" ({oldStack})" : "")}]";
        i.stack = oldStack;
        return result;
    }
}
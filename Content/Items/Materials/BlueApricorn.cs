using Terramon.Content.Items.Vitamins;

namespace Terramon.Content.Items.Materials;

public class BlueApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<RareCandyRarity>();
}
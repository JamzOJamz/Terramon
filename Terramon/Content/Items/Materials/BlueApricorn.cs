namespace Terramon.Content.Items;

public class BlueApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<RareCandyRarity>();
}
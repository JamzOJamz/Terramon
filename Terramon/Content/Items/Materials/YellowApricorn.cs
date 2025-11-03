namespace Terramon.Content.Items;

public class YellowApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<ReviveRarity>();
}
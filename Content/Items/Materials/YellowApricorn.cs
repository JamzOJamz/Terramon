using Terramon.Content.Items.Recovery;

namespace Terramon.Content.Items.Materials;

public class YellowApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<ReviveRarity>();
}
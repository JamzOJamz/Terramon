using Terramon.Content.Items.Mechanical;

namespace Terramon.Content.Items.Materials;

public class WhiteApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<PremierBallRarity>();
}
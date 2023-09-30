using Terramon.Core.Helpers;

namespace Terramon.Content.Items.Materials;

public class PinkApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<PinkApricornRarity>();
}

public class PinkApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xFF84B8);
}
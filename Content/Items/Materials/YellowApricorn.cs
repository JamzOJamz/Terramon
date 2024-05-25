using Terramon.Helpers;

namespace Terramon.Content.Items.Materials;

public class YellowApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<YellowApricornRarity>();
}

public class YellowApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xFFC532);
}
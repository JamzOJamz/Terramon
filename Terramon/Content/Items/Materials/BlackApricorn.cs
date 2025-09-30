using Terramon.Helpers;

namespace Terramon.Content.Items;

public class BlackApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<BlackApricornRarity>();
}

public class BlackApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x878787);
}
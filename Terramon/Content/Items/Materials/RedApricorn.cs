using Terramon.Helpers;

namespace Terramon.Content.Items;

public class RedApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<RedApricornRarity>();
}

public class RedApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD85749);
}
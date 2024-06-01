using Terramon.Helpers;

namespace Terramon.Content.Items.Materials;

public class GreenApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<GreenApricornRarity>();
}

public class GreenApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x6DBB57);
}
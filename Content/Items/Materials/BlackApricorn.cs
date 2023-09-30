using Terramon.Core.Helpers;

namespace Terramon.Content.Items.Materials;

public class BlackApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<BlackApricornRarity>();
}

public class BlackApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x878787);
}
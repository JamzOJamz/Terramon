using Terramon.Helpers;

namespace Terramon.Content.Items.Materials;

public class PinkApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<PinkApricornRarity>();
    
    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
    }
}

public class PinkApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xFF84B8);
}
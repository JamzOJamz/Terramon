using Terramon.Helpers;

namespace Terramon.Content.Items;

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
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xFF84B8);
}
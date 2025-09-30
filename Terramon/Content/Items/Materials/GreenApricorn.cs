using Terramon.Helpers;

namespace Terramon.Content.Items;

public class GreenApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<GreenApricornRarity>();
    
    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
    }
}

public class GreenApricornRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x6DBB57);
}
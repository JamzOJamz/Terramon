using Terramon.Helpers;

namespace Terramon.Content.Items.Recovery;

public class Potion : BasePotionItem
{
    protected override ushort HealAmount => 20;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }
    
    protected override int UseRarity => ModContent.RarityType<PotionRarity>();
}

public class PotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xA46FD8);
}
using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items.Recovery;

[LoadWeight(2f)] // After SuperPotion (1f)
public class HyperPotion : BasePotionItem
{
    protected override ushort HealAmount => 200;
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 10;
    }
    
    protected override int UseRarity => ModContent.RarityType<HyperPotionRarity>();
}

public class HyperPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD762B7);
}
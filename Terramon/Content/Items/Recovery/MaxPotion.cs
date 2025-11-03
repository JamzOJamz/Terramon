using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items;

[LoadWeight(3f)] // After HyperPotion (2f)
public class MaxPotion : BasePotionItem
{
    protected override ushort HealAmount => ushort.MaxValue;
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 5;
    }
    
    protected override int UseRarity => ModContent.RarityType<MaxPotionRarity>();
}

public class MaxPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x6FC2F2);
}
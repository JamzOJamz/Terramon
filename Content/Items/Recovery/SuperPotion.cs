using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items.Recovery;

[LoadWeight(1f)] // After Potion (0f)
public class SuperPotion : BasePotionItem
{
    protected override ushort HealAmount => 50;
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 15;
    }
    
    protected override int UseRarity => ModContent.RarityType<SuperPotionRarity>();
}

public class SuperPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD97967);
}
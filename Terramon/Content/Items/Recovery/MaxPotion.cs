using Terramon.Helpers;

namespace Terramon.Content.Items;

public class MaxPotion : BasePotionItem
{
    protected override ushort HealAmount => ushort.MaxValue;

    protected override int UseRarity => ModContent.RarityType<MaxPotionRarity>();

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 5;
    }
}

public class MaxPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x6FC2F2);
}
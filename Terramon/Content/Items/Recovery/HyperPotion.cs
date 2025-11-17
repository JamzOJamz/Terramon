using Terramon.Helpers;

namespace Terramon.Content.Items;

public class HyperPotion : BasePotionItem
{
    protected override ushort HealAmount => 200;

    protected override int UseRarity => ModContent.RarityType<HyperPotionRarity>();

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 10;
    }
}

public class HyperPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD762B7);
}
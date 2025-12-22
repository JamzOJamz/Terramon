using Terramon.Utilities.Xna;

namespace Terramon.Content.Items;

public class SuperPotion : BasePotionItem
{
    protected override ushort HealAmount => 50;

    protected override int UseRarity => ModContent.RarityType<SuperPotionRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Item.ResearchUnlockCount = 15;
    }
}

public class SuperPotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD97967);
}
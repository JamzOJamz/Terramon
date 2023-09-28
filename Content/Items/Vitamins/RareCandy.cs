using Microsoft.Xna.Framework;

namespace Terramon.Content.Items.Vitamins;

public class RareCandy : Vitamin
{
    protected override int UseRarity => ModContent.RarityType<RareCandyRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 30;
        Item.height = 30;
        Item.value = 1;
    }
}

public class RareCandyRarity : ModRarity
{
    public override Color RarityColor => new(98, 153, 229);
}
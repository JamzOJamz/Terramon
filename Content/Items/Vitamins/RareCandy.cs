using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Vitamins;

public class RareCandy : Vitamin
{
    protected override int UseRarity => ModContent.RarityType<RareCandyRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 100;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 28;
        Item.height = 28;
        Item.value = 1;
    }
}

public class RareCandyRarity : ModRarity
{
    public override Color RarityColor => new(98, 153, 229);
}
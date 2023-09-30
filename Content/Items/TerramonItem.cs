using Terraria.ID;

namespace Terramon.Content.Items;

public abstract class TerramonItem : ModItem
{
    protected virtual int UseRarity => ItemRarityID.Gray;

    public override void SetDefaults()
    {
        Item.rare = UseRarity;
        Item.maxStack = Item.CommonMaxStack;
    }
}
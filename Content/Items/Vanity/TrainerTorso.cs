using Terraria.GameContent.Creative;
using Terraria.ID;

namespace Terramon.Content.Items;

[AutoloadEquip(EquipType.Body)]
public class TrainerTorso : VanityItem
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 18;
        Item.height = 18;
        Item.value = 3000;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.White;
        Item.vanity = true;
    }
}
namespace Terramon.Content.Items;

[AutoloadEquip(EquipType.Legs)]
public class TrainerLegs : VanityItem
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Item.ResearchUnlockCount = 1;
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
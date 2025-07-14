using Terramon.Core.Loaders;

namespace Terramon.Content.Items.Vanity;

[AutoloadEquip(EquipType.Body)]
[LoadGroup("TrainerVanity")]
[LoadWeight(1f)] // After TrainerCap (0f)
public class TrainerTorso : VanityItem
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
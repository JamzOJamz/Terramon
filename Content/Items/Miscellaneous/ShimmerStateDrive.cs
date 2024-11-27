using Terramon.Content.Rarities;

namespace Terramon.Content.Items;

public class ShimmerStateDrive : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Miscellaneous/ShimmerStateDrive";
    protected override int UseRarity => ModContent.RarityType<AetherRarity>();
    
    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.consumable = true;
        Item.useAnimation = 45;
        Item.useTime = 45;
        Item.UseSound = SoundID.Item92;
        Item.width = 32;
        Item.height = 32;
        Item.value = Item.sellPrice(0, 1, 50);
    }

    public override bool? UseItem(Player player)
    {
        return true; // TODO: Implement once PC is finished
    }

    public override bool ConsumeItem(Player player)
    {
        return false; // Make consumable later
    }
}
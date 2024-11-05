using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.Items;

public class ShimmerStateDrive : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Miscellaneous/ShimmerStateDrive";
    public override bool Obtainable => false;
    protected override int UseRarity => ModContent.RarityType<AetherBallRarity>();

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
        return true;
    }

    public override bool ConsumeItem(Player player)
    {
        return false;
    }
}
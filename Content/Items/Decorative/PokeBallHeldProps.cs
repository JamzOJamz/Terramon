using Terramon.Content.Items.Mechanical;
using Terramon.Content.Tiles.Decorative;
using Terraria.ID;

namespace Terramon.Content.Items.Decorative;

public abstract class PokeBallHeldProp : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetAssetName();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 16;
        Item.height = 16;
        Item.value = 1000;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
    }

    private string GetAssetName()
    {
        return GetType().Name.Split("HeldProp")[0] + "Projectile";
    }
}

public class RegularBallHeldProp : PokeBallHeldProp
{
    protected override int UseRarity => ModContent.RarityType<PokeBallRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.createTile = ModContent.TileType<RegularBallProp>();
    }
}
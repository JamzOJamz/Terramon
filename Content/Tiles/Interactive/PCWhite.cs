using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.Tiles.Interactive;

public class PCWhite : PCTile
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DustType = DustID.Silver;
        RegisterItemDrop(ModContent.ItemType<PCItemWhite>());
    }

    public override void MouseOver(int i, int j)
    {
        base.MouseOver(i, j);
        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<PCItemWhite>();
    }
}

public class PCItemWhite : PCItem
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<PCWhite>());
        base.SetDefaults();
    }
}
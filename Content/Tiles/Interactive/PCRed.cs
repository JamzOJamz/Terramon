using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.Tiles.Interactive;

public class PCRed : PCTile
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DustType = DustID.Crimstone;
        RegisterItemDrop(ModContent.ItemType<PCItemRed>());
    }

    public override void MouseOver(int i, int j)
    {
        base.MouseOver(i, j);
        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<PCItemRed>();
    }
}

public class PCItemRed : PCItem
{
    protected override int UseRarity => ModContent.RarityType<CherishBallRarity>();

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<PCRed>());
        base.SetDefaults();
    }
}
namespace Terramon.Content.Tiles.Paintings;

public class ErikaPaintingItem : PaintingItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<ErikaPaintingTile>());
        base.SetDefaults();
        Item.width = 28;
        Item.height = 28;
    }
}
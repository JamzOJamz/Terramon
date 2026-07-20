using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Paintings;

public class ErikaPaintingTile : PaintingTile
{
    protected override void SetTileObjectData()
    {
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(Type);
    }
}
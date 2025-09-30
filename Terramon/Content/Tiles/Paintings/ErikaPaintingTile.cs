using Terraria.Localization;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Paintings;

public class ErikaPaintingTile : PaintingTile
{
    public override string Texture => "Terramon/Assets/Tiles/Paintings/" + GetType().Name;
    
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(Type);

        AddMapEntry(Color.White, Language.GetText("MapObject.Painting"));
    }
}
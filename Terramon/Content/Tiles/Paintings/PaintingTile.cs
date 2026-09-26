using Terraria.Localization;

namespace Terramon.Content.Tiles.Paintings;

public abstract class PaintingTile : ModTile
{
    public override string Texture => "Terramon/Assets/Tiles/Paintings/" + GetType().Name;

    protected abstract void SetTileObjectData();

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;

        SetTileObjectData();

        AddMapEntry(Color.White, Language.GetText("MapObject.Painting"));
    }
}
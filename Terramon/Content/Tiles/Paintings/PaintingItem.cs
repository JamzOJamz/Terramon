using Terramon.Content.Items;

namespace Terramon.Content.Tiles.Paintings;

public abstract class PaintingItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Tiles/Paintings/" + GetType().Name;
}
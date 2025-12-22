using Terramon.Content.Items;

namespace Terramon.Content.Tiles.Paintings;

public abstract class PaintingItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Textures/Tiles/Paintings/" + GetType().Name;
}
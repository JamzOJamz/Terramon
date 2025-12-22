namespace Terramon.Content.Items;

public abstract class Material : TerramonItem
{
    public override string Texture => "Terramon/Assets/Textures/Items/Materials/" + GetType().Name;
}
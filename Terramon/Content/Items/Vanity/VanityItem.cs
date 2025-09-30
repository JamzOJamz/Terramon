namespace Terramon.Content.Items;

public abstract class VanityItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Vanity/" + GetType().Name;
}
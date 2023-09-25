namespace Terramon.Content.Items.Vanity;

public abstract class BaseVanityItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Vanity/" + GetType().Name;
}
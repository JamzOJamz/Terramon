namespace Terramon.Content.Items;

public abstract class VanityItem : TerramonItem
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.Vanity;
    
    public override string Texture => "Terramon/Assets/Items/Vanity/" + GetType().Name;
}
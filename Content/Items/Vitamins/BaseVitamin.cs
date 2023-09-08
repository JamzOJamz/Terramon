namespace Terramon.Content.Items.Vitamins;

public abstract class BaseVitamin : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Vitamins/" + GetType().Name;
}
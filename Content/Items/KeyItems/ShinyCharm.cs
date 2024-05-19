namespace Terramon.Content.Items.KeyItems;

public class ShinyCharm : KeyItem
{
    protected override bool Obtainable => false;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 30;
    }
}
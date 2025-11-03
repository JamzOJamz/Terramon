namespace Terramon.Content.Items;

public sealed class ExpCharm : KeyItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 30;
    }

    public override void UpdateInventory(Player player)
    {
        player.Terramon().HasExpCharm = true;
    }
}

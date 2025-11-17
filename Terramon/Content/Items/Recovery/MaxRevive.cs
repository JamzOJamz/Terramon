namespace Terramon.Content.Items;

public class MaxRevive : BaseReviveItem
{
    protected override float RestorationPercentage => 1f;
    protected override int UseRarity => ModContent.RarityType<FireStoneRarity>();
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 36;
        Item.height = 32;
    }
}
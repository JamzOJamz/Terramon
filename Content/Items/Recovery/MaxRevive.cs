using Terramon.Content.Items.Evolutionary;
using Terramon.Core.Loaders;

namespace Terramon.Content.Items.Recovery;

[LoadWeight(5f)] // After Revive (4f)
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
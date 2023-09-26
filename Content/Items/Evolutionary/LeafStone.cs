using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.Evolutionary;

public class LeafStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<LeafStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 20;
        Item.height = 22;
    }
}

public class LeafStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } = {
        new(172, 204, 125),
        new(119, 153, 68)
    };

    protected override float Time => 2f;
}
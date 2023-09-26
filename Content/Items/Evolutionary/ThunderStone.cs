using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.Evolutionary;

public class ThunderStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<ThunderStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 20;
    }
}

public class ThunderStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } = {
        new(54, 204, 34),
        new(229, 208, 100)
    };

    protected override float Time => 2f;
}
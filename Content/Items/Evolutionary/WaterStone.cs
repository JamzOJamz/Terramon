using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.Evolutionary;

public class WaterStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<WaterStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 20;
    }
}

public class WaterStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } = {
        new(25, 185, 229),
        new(30, 150, 255)
    };

    protected override float Time => 2f;
}
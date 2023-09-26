using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.Evolutionary;

public class WaterStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<WaterStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 26;
    }
}

public class WaterStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } = {
        new(112, 178, 215),
        new(83, 115, 208)
    };

    protected override float Time => 2f;
}
using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.Evolutionary;

public class DuskStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<DuskStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 24;
    }
}

public class DuskStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    {
        new(219, 161, 221),
        new(152, 99, 183)
    };

    protected override float Time => 2f;
}
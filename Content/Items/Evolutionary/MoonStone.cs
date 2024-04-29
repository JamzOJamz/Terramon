using Terramon.Content.Items.Materials;

namespace Terramon.Content.Items.Evolutionary;

public class MoonStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<BlackApricornRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 28;
        Item.height = 28;
    }
}
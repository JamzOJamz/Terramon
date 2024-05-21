using Terramon.Content.Rarities;
using Terramon.ID;

namespace Terramon.Content.Items.Evolutionary;

public class LeafStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<LeafStoneRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 28;
    }
    
    public override ushort GetEvolvedSpecies(PokemonData data, EvolutionTrigger trigger)
    {
        if (trigger != EvolutionTrigger.DirectUse) return 0;
        return data.ID switch
        {
            NationalDexID.Gloom => NationalDexID.Vileplume,
            NationalDexID.Weepinbell => NationalDexID.Victreebel,
            NationalDexID.Exeggcute => NationalDexID.Exeggutor,
            _ => 0
        };
    }
}

public class LeafStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(172, 204, 125),
        new Color(119, 153, 68)
    ];

    protected override float Time => 2f;
}
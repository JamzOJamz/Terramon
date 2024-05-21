using Terramon.Content.Rarities;
using Terramon.ID;

namespace Terramon.Content.Items.Evolutionary;

public class FireStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<FireStoneRarity>();

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
            NationalDexID.Vulpix => NationalDexID.Ninetales,
            NationalDexID.Growlithe => NationalDexID.Arcanine,
            NationalDexID.Eevee => NationalDexID.Flareon,
            _ => 0
        };
    }
}

public class FireStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(255, 202, 99),
        new Color(234, 132, 53)
    ];

    protected override float Time => 2f;
}
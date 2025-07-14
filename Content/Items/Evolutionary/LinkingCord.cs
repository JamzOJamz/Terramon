using Terramon.Helpers;
using Terramon.ID;

namespace Terramon.Content.Items.Evolutionary;

public class LinkingCord : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<LinkingCordRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 32;
        Item.height = 36;
        Item.value = Item.buyPrice(gold: 5);
    }
    
    public override ushort GetEvolvedSpecies(PokemonData data)
    {
        return data.ID switch
        {
            NationalDexID.Kadabra => NationalDexID.Alakazam,
            NationalDexID.Machoke => NationalDexID.Machamp,
            NationalDexID.Graveler => NationalDexID.Golem,
            NationalDexID.Haunter => NationalDexID.Gengar,
            _ => 0
        };
    }
}

public class LinkingCordRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x9B94B4);
}
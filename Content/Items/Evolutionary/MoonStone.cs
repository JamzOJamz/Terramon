using Terramon.Content.Items.Materials;
using Terramon.ID;

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
    
    public override ushort GetEvolvedSpecies(PokemonData data)
    {
        return data.ID switch
        {
            NationalDexID.Nidorina => NationalDexID.Nidoqueen,
            NationalDexID.Nidorino => NationalDexID.Nidoking,
            NationalDexID.Clefairy => NationalDexID.Clefable,
            NationalDexID.Jigglypuff => NationalDexID.Wigglytuff,
            _ => 0
        };
    }
}
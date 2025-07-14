using Terramon.Content.Rarities;
using Terramon.ID;

namespace Terramon.Content.Items.Evolutionary;

public class ThunderStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<ThunderStoneRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<FireStone>();
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 28;
    }
    
    public override ushort GetEvolvedSpecies(PokemonData data)
    {
        return data.ID switch
        {
            NationalDexID.Pikachu => NationalDexID.Raichu,
            NationalDexID.Eevee => NationalDexID.Jolteon,
            _ => 0
        };
    }
}

public class ThunderStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(54, 204, 34),
        new Color(229, 208, 100)
    ];

    protected override float Time => 2f;
}
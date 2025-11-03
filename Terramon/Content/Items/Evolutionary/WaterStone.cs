using Terramon.Content.Rarities;
using Terramon.ID;

namespace Terramon.Content.Items;

public class WaterStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<WaterStoneRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ThunderStone>();
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 26;
    }

    public override ushort GetEvolvedSpecies(PokemonData data)
    {
        return data.ID switch
        {
            NationalDexID.Poliwhirl => NationalDexID.Poliwrath,
            NationalDexID.Shellder => NationalDexID.Cloyster,
            NationalDexID.Staryu => NationalDexID.Starmie,
            NationalDexID.Eevee => NationalDexID.Vaporeon,
            _ => 0
        };
    }
}

public class WaterStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(112, 178, 215),
        new Color(83, 115, 208)
    ];

    protected override float Time => 2f;
}
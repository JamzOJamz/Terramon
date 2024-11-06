using Terramon.Content.Rarities;

namespace Terramon.Content.Items;

public class IceStone : EvolutionaryItem
{
    protected override int UseRarity => ModContent.RarityType<IceStoneRarity>();
    
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 24;
    }
}

public class IceStoneRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(152, 232, 255),
        new Color(89, 206, 255)
    ];

    protected override float Time => 2f;
}
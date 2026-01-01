using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Rarities;
using Terramon.Helpers;

namespace Terramon.Content.Items;

public sealed class Nugget() : ValuableItem(5000)
{
    protected override int UseRarity => ModContent.RarityType<NuggetRarity>();
}

public sealed class BigNugget() : ValuableItem(20000)
{
    protected override int UseRarity => ModContent.RarityType<NuggetRarity>();
}

public sealed class TinyMushroom() : ValuableItem(250)
{
    protected override int UseRarity => ModContent.RarityType<MushroomRarity>();
}

public sealed class BigMushroom() : ValuableItem(2500)
{
    protected override int UseRarity => ModContent.RarityType<MushroomRarity>();
}

public sealed class Pearl() : ValuableItem(1000)
{
    protected override int UseRarity => ModContent.RarityType<PearlRarity>();
}

public sealed class BigPearl() : ValuableItem(4000)
{
    protected override int UseRarity => ModContent.RarityType<PearlRarity>();
}

public sealed class SilverLeaf() : ValuableItem(500)
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();
}

public sealed class GoldLeaf() : ValuableItem(500)
{
    protected override int UseRarity => ModContent.RarityType<NuggetRarity>();
}

public sealed class RelicCopper() : ValuableItem(1000)
{
    protected override int UseRarity => ModContent.RarityType<RelicCopperRarity>();
}

public sealed class RelicSilver() : ValuableItem(5000)
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();
}

public sealed class RelicGold() : ValuableItem(30000)
{
    protected override int UseRarity => ModContent.RarityType<NuggetRarity>();
}

public sealed class NuggetRarity : ModRarity
{
    public override Color RarityColor => ColorUtils.FromHexRGB(0xFFCC32);
}

public sealed class MushroomRarity : ModRarity
{
    public override Color RarityColor => ColorUtils.FromHexRGB(0xFF568B);
}

public sealed class PearlRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new(247, 247, 186),
        new(97, 221, 192),
        new(220, 177, 178)
    ];

    protected override float Time => 2f;
}

public sealed class RelicCopperRarity : ModRarity
{
    public override Color RarityColor => ColorUtils.FromHexRGB(0xF7945B);
}
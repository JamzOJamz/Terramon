using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.Items;

public class WhiteApricorn : ApricornItem
{
    public override bool Obtainable => false;
    protected override int UseRarity { get; } = ModContent.RarityType<PremierBallRarity>();
}
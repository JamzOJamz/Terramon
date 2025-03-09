using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class PremierBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<PremierBallItem>();
}

[LoadWeight(4f)] // After MasterBallMiniItem (3f)
internal class PremierBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();
}

[LoadWeight(4f)] // After MasterBallItem (3f)
internal class PremierBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<PremierBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<PremierBallTile>();
    protected override int InGamePrice => 200;
}

public class PremierBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<PremierBallItem>();
}

public class PremierBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xC9C9E5);
}
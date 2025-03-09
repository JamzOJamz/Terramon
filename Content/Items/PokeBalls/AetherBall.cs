using Terramon.Content.Rarities;
using Terramon.Core.Loaders;

namespace Terramon.Content.Items.PokeBalls;

internal class AetherBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<AetherBallItem>();
}

[LoadWeight(7f)] // After CherishBallMiniItem (6f)
internal class AetherBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<AetherRarity>();
}

[LoadWeight(7f)] // After CherishBallItem (6f)
internal class AetherBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<AetherRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<AetherBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<AetherBallTile>();
    protected override int InGamePrice => 200;
}

public class AetherBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<AetherBallItem>();
}
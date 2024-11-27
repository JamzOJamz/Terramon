using Terramon.Content.Rarities;
using Terramon.Core.Loaders;

namespace Terramon.Content.Items.PokeBalls;

internal class AetherBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<AetherBallItem>();
    protected override float CatchModifier => 1;
}

[LoadWeight(6f)] // After CherishBallMiniItem (5f)
internal class AetherBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<AetherRarity>();
}

[LoadWeight(6f)] // After CherishBallItem (5f)
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
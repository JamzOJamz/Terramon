using Terramon.Content.Rarities;

namespace Terramon.Content.Items.PokeBalls;

internal class AetherBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<AetherBallItem>();
    protected override float CatchModifier => 1;
}

internal class AetherBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<AetherRarity>();
}

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
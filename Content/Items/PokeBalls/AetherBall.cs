using Terramon.Content.Rarities;

namespace Terramon.Content.Items.PokeBalls;

internal class AetherBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<AetherBallItem>();
    protected override float CatchModifier => 1;
}

internal class AetherBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<AetherBallRarity>();
}

internal class AetherBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<AetherBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<AetherBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<AetherBallTile>();
    protected override int InGamePrice => 200;
}

public class AetherBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<AetherBallItem>();
}

public class AetherBallRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(255, 84, 222),
        new Color(75, 123, 255),
        new Color(113, 60, 234)
    ];

    protected override float Time => 2f;
}
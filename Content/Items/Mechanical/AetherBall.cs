using Terramon.Content.Rarities;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class AetherBallProjectile : BasePkballProjectile
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/PokeBallProjectile";
    
    public override int pokeballCapture => ModContent.ItemType<AetherBallItem>();
    protected override float catchModifier => 1;
}

internal class AetherBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<AetherBallRarity>();
    protected override int pokeballThrow => ModContent.ProjectileType<AetherBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<AetherBallTile>();
    protected override int igPrice => 200;

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault($"Premier Ball");
        // Tooltip.SetDefault("A somewhat rare Poké Ball that has \nbeen specially made to commemorate an \nevent of some sort.");
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            igPrice / 2; //Amount needed to duplicate them in Journey Mode
    }
}

public class AetherBallTile : BasePkballTile
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/PokeBallTile";
    protected override int dropItem => ModContent.ItemType<PremierBallItem>();
}

public class AetherBallRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    {
        new(85, 230, 179),
        new(213, 20, 201),
        new(170, 62, 254)
    };

    protected override float Time => 2f;
}
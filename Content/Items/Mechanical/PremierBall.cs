using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class PremierBallProjectile : BasePkballProjectile
{
    public override int pokeballCapture => ModContent.ItemType<PremierBallItem>();
    public override float catchModifier => 1;
}

internal class PremierBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<PremierBallRarity>();
    protected override int pokeballThrow => ModContent.ProjectileType<PremierBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<PremierBallTile>();
    protected override int igPrice => 200;

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault($"Premier Ball");
        // Tooltip.SetDefault("A somewhat rare Poké Ball that has \nbeen specially made to commemorate an \nevent of some sort.");
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            igPrice / 2; //Amount needed to duplicate them in Journey Mode
    }
}

public class PremierBallTile : BasePkballTile
{
    protected override int dropItem => ModContent.ItemType<PremierBallItem>();
}

public class PremierBallRarity : ModRarity
{
    public override Color RarityColor => new(201, 201, 229);
}
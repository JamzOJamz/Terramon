using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class GreatBallProjectile : BasePkballProjectile
{
    public override int pokeballCapture => ModContent.ItemType<GreatBallItem>();
    public override float catchModifier => 1.5f;
}

internal class GreatBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    protected override int pokeballThrow => ModContent.ProjectileType<GreatBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<GreatBallTile>();
    protected override int igPrice => 600;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            igPrice / 2; //Amount needed to duplicate them in Journey Mode
    }
}

public class GreatBallTile : BasePkballTile
{
    protected override int dropItem => ModContent.ItemType<GreatBallItem>();
}

public class GreatBallRarity : ModRarity
{
    public override Color RarityColor => new(47, 155, 224);
}
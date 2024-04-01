using Terramon.Core.Helpers;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class PokeBallProjectile : BasePkballProjectile
{
    public override int pokeballCapture => ModContent.ItemType<PokeBallItem>();
    public override float catchModifier => 1;
}

internal class PokeBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<PokeBallRarity>();
    protected override int pokeballThrow => ModContent.ProjectileType<PokeBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<PokeBallTile>();
    protected override int igPrice => 200;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            igPrice / 2; // Amount needed to duplicate them in Journey Mode
    }
}

public class PokeBallTile : BasePkballTile
{
    protected override int dropItem => ModContent.ItemType<PokeBallItem>();
}

public class PokeBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xD64A56);
}
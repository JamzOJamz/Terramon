using Terramon.Core.Loaders;
using Terramon.Helpers;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.PokeBalls;

internal class CherishBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<CherishBallItem>();
    protected override float CatchModifier => 1;
}

[LoadWeight(5f)] // After PremierBallMiniItem (4f)
internal class CherishBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<CherishBallRarity>();
}

[LoadWeight(5f)] // After PremierBallItem (4f)
internal class CherishBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<CherishBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<CherishBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<CherishBallTile>();
    
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = Item.buyPrice(gold: 10);
    }
}

public class CherishBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<CherishBallItem>();
}

public class CherishBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD3434A);
}
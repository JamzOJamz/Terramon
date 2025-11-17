using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class CherishBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<CherishBallItem>();
    protected override float CatchModifier => 1;
}

internal class CherishBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<CherishBallRarity>();
}

internal class CherishBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<CherishBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<CherishBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<CherishBallTile>();
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
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
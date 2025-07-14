using Terramon.Content.Items.Materials;
using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class PokeBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<PokeBallItem>();
    protected override float CatchModifier => 1;
}

internal class PokeBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<PokeBallRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AetherBallMiniItem>();
    }
}

internal class PokeBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<PokeBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<PokeBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<PokeBallTile>();
    protected override int InGamePrice => 200;

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AetherBallItem>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(RecipeGroupID.IronBar, 2)
            .AddIngredient<RedApricorn>(4)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

public class PokeBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<PokeBallItem>();
}

public class PokeBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xD64A56);
}
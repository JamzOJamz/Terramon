using Terramon.Content.Items.Materials;
using Terramon.Helpers;
using Terraria.ID;

namespace Terramon.Content.Items.PokeBalls;

internal class GreatBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<GreatBallItem>();
    protected override float CatchModifier => 1.5f;
}

internal class GreatBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    public override int SubLoadPriority => 1;
}

internal class GreatBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<GreatBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<GreatBallTile>();
    protected override int InGamePrice => 600;
    public override int SubLoadPriority => 1;
    
    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup($"{nameof(Terramon)}:SilverBar", 2)
            .AddIngredient<BlueApricorn>(2)
            .AddIngredient<RedApricorn>(2)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

public class GreatBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<GreatBallItem>();
}

public class GreatBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x2F9BE0);
}
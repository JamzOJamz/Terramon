using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class UltraBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<UltraBallItem>();
    protected override float CatchModifier => 2f;
}

internal class UltraBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<UltraBallRarity>();
}

internal class UltraBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<UltraBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<UltraBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<UltraBallTile>();
    protected override int InGamePrice => 1000;
    
    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup($"{nameof(Terramon)}:GoldBar", 2)
            .AddIngredient<BlackApricorn>(2)
            .AddIngredient<YellowApricorn>(2)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

public class UltraBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<UltraBallItem>();
}

public class UltraBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xF9B643);
}
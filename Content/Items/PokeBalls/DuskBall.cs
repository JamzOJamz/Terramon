using Terramon.Content.NPCs;
using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class DuskBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<DuskBallItem>();

    protected override float ChangeCatchModifier(PokemonNPC target)
    {
        return Main.dayTime ? CatchModifier : CatchModifier * 3f;
    }
}

[LoadWeight(5f)] // After PremierBallMiniItem (4f)
internal class DuskBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<DuskBallRarity>();
}

[LoadWeight(5f)] // After PremierBallItem (4f)
internal class DuskBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<DuskBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<DuskBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<DuskBallTile>();
    protected override int InGamePrice => 1000;
    
    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup($"{nameof(Terramon)}:GoldBar", 2)
            .AddIngredient<GreenApricorn>(2)
            .AddIngredient<BlackApricorn>(2)
            .AddTile(TileID.Anvils)
            .Register();
    }
}


public class DuskBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<UltraBallItem>();
}

public class DuskBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x49A643);
}
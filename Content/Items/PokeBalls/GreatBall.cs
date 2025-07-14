﻿using Terramon.Content.Items.Materials;
using Terramon.Core.Loaders;
using Terramon.Helpers;

namespace Terramon.Content.Items.PokeBalls;

internal class GreatBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<GreatBallItem>();
    protected override float CatchModifier => 1.5f;
}

[LoadWeight(1f)] // After PokeBallMiniItem (0f)
internal class GreatBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
}

[LoadWeight(1f)] // After PokeBallItem (0f)
internal class GreatBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<GreatBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<GreatBallTile>();
    protected override int InGamePrice => 600;
    
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
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x2F9BE0);
}
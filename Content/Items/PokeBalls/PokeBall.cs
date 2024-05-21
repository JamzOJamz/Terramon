﻿using Terramon.Core.Helpers;
using Terraria.ID;

namespace Terramon.Content.Items.PokeBalls;

internal class PokeBallProjectile : BasePkballProjectile
{
    protected override int pokeballItem => ModContent.ItemType<PokeBallItem>();
    protected override float catchModifier => 1;
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
    protected override int pokeballThrow => ModContent.ProjectileType<PokeBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<PokeBallTile>();
    protected override int igPrice => 200;

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AetherBallItem>();
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
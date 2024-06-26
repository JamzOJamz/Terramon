﻿using Terramon.Helpers;
using Terraria.GameContent.Creative;

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
    protected override int InGamePrice => 800;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            InGamePrice / 2; //Amount needed to duplicate them in Journey Mode
    }
}

public class UltraBallTile : BasePkballTile
{
    protected override int DropItem => ModContent.ItemType<UltraBallItem>();
}

public class UltraBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xF9B643);
}
﻿using Terramon.Helpers;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.PokeBalls;

internal class GreatBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<GreatBallItem>();
    protected override float CatchModifier => 1.5f;
}

internal class GreatBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
}

internal class GreatBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<GreatBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<GreatBallTile>();
    protected override int InGamePrice => 600;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            InGamePrice / 2; //Amount needed to duplicate them in Journey Mode
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
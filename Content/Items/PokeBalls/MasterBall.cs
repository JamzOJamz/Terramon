using Terramon.Content.NPCs.Pokemon;
using Terramon.Core.Loaders;
using Terramon.Helpers;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.PokeBalls;

internal class MasterBallProjectile : BasePkballProjectile
{
    protected override int PokeballItem => ModContent.ItemType<MasterBallItem>();

    protected override int DropItemChanceDenominator => 1;

    protected override bool CatchPokemonChances(PokemonNPC target, float random)
    {
        return true;
    }
}

[LoadAfter(typeof(PremierBallMiniItem))]
internal class MasterBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
}

[LoadAfter(typeof(PremierBallItem))]
internal class MasterBallItem : BasePkballItem
{
    public override bool Obtainable => false;
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<MasterBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<MasterBallTile>();

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
}

public class MasterBallTile : BasePkballTile
{
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name + "_Highlight";
    protected override int DropItem => ModContent.ItemType<MasterBallItem>();
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xA460B2);
}
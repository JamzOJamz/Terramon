using Terramon.Content.NPCs.Pokemon;
using Terramon.Core.Helpers;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class MasterBallProjectile : BasePkballProjectile
{
    public override int pokeballCapture => ModContent.ItemType<MasterBallItem>();
    protected override float catchModifier => 2f;

    protected override bool CatchPokemonChances(PokemonNPC capture, float random)
    {
        return true;
    }
}

internal class MasterBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
    protected override int pokeballThrow => ModContent.ProjectileType<MasterBallProjectile>();
    protected override int pokeballTile => ModContent.TileType<MasterBallTile>();

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
}

public class MasterBallTile : BasePkballTile
{
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name + "_Highlight";
    protected override int dropItem => ModContent.ItemType<MasterBallItem>();
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0xA460B2);
}
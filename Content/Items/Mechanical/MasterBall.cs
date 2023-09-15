using Microsoft.Xna.Framework;
using Terramon.Content.NPCs.Pokemon;
using Terraria.GameContent.Creative;

namespace Terramon.Content.Items.Mechanical;

internal class MasterBallProjectile : BasePkballProjectile
{
    public override int pokeballCapture => ModContent.ItemType<MasterBallItem>();
    public override float catchModifier => 2f;

    public override bool CatchPokemonChances(PokemonNPC capture, float random)
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
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] =
            igPrice / 2; //Amount needed to duplicate them in Journey Mode
    }
}

public class MasterBallTile : BasePkballTile
{
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name +"_Highlight";
    protected override int dropItem => ModContent.ItemType<MasterBallItem>();
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor => new(164, 96, 178);
}
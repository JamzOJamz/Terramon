using Terramon.Content.NPCs;
using Terramon.Core.Loaders;
using Terramon.Helpers;

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

[LoadWeight(3f)] // After UltraBallMiniItem (2f)
internal class MasterBallMiniItem : BasePkballMiniItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
}

[LoadWeight(3f)] // After UltraBallItem (2f)
internal class MasterBallItem : BasePkballItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
    protected override int PokeballThrow => ModContent.ProjectileType<MasterBallProjectile>();
    protected override int PokeballTile => ModContent.TileType<MasterBallTile>();

    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
        Item.ResearchUnlockCount = 1;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale,
        int whoAmI)
    {
        Main.GetItemDrawFrame(Item.type, out var itemTexture, out var itemFrame);
        var drawOrigin = itemFrame.Size() / 2f;
        var drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, drawOrigin.Y);
        spriteBatch.Draw(itemTexture, drawPosition, itemFrame, Color.White, rotation, drawOrigin, scale, SpriteEffects.None, 0);
        
        return false;
    }
}

public class MasterBallTile : BasePkballTile
{
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name + "_Highlight";
    protected override int DropItem => ModContent.ItemType<MasterBallItem>();
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xA460B2);
}
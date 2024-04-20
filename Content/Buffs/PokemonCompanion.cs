using ReLogic.Content;
using Terramon.ID;
using Terraria.DataStructures;
using Terraria.ID;

namespace Terramon.Content.Buffs;

public class PokemonCompanion : ModBuff
{
    private const string PokeballIconPrefix = "Terramon/Assets/Items/PokeBalls/";
    private const string TemplatePath = "Terramon/Assets/Buffs/BuffTemplate";
    private const string ShinyTemplatePath = "Terramon/Assets/Buffs/BuffTemplateShiny";
    private const string StarIconPath = "Terramon/Assets/Buffs/IconStar";
    private Asset<Texture2D> shinyTemplateTexture;
    private Asset<Texture2D> starIconTexture;
    private Asset<Texture2D> templateTexture;

    public override string Texture => "Terramon/Assets/Buffs/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        templateTexture = ModContent.Request<Texture2D>(TemplatePath);
        shinyTemplateTexture = ModContent.Request<Texture2D>(ShinyTemplatePath);
        starIconTexture = ModContent.Request<Texture2D>(StarIconPath);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        var player = TerramonPlayer.LocalPlayer;

        // Draw the template texture
        var useTemplate = player.Party[0] != null && player.Party[0].IsShiny ? shinyTemplateTexture : templateTexture;
        spriteBatch.Draw(useTemplate.Value, drawParams.Position, new Rectangle(0, 0, 32, 32), drawParams.DrawColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the pokeball icon
        var ballId = player.Party[0]?.Ball ?? BallID.PokeBall;
        var ballName = BallID.Search.GetName(ballId) + "Projectile";
        var pokeballIconTexture = ModContent.Request<Texture2D>(PokeballIconPrefix + ballName);
        spriteBatch.Draw(pokeballIconTexture.Value, drawParams.Position + new Vector2(0, 2),
            pokeballIconTexture.Frame(verticalFrames: 4, frameY: 2), drawParams.DrawColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the star icon
        spriteBatch.Draw(starIconTexture.Value, drawParams.Position + new Vector2(14, 14),
            new Rectangle(0, 0, starIconTexture.Width(), starIconTexture.Height()), drawParams.DrawColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        return false;
    }
    
    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        rare = ItemRarityID.Expert;
    }
}
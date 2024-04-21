using ReLogic.Content;
using Terramon.ID;
using Terraria.DataStructures;
using Terraria.ID;

namespace Terramon.Content.Buffs;

public class PokemonCompanion : ModBuff
{
    private const string PokeballIconPrefix = "Terramon/Assets/Items/PokeBalls/";
    private const string TemplatePath = "Terramon/Assets/Buffs/BuffTemplate";
    private const string StarIconPath = "Terramon/Assets/Buffs/IconStar";
    private static RenderTarget2D rt;
    private Asset<Texture2D> starIconTexture;
    private Asset<Texture2D> templateTexture;

    public override string Texture => "Terramon/Assets/Buffs/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        templateTexture = ModContent.Request<Texture2D>(TemplatePath);
        starIconTexture = ModContent.Request<Texture2D>(StarIconPath);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        var player = TerramonPlayer.LocalPlayer;

        // Use the render target
        var gd = Main.graphics.GraphicsDevice;
        gd.SetRenderTarget(rt);
        gd.Clear(Color.Transparent);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        // Draw the template texture
        spriteBatch.Draw(templateTexture.Value, Vector2.Zero, new Rectangle(0, 0, 32, 32), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the pokeball icon
        var ballId = player.Party[0]?.Ball ?? BallID.PokeBall;
        var ballName = BallID.Search.GetName(ballId) + "Projectile";
        var pokeballIconTexture = ModContent.Request<Texture2D>(PokeballIconPrefix + ballName);
        spriteBatch.Draw(pokeballIconTexture.Value, new Vector2(0, 2),
            pokeballIconTexture.Frame(verticalFrames: 4, frameY: 2), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the star icon
        spriteBatch.Draw(starIconTexture.Value, new Vector2(14, 14),
            new Rectangle(0, 0, starIconTexture.Width(), starIconTexture.Height()), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        spriteBatch.End();
        gd.SetRenderTarget(null);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        // Draw the render target
        spriteBatch.Draw(rt, drawParams.Position, drawParams.DrawColor);

        return false;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        rare = ItemRarityID.Expert;
    }

    public override void Load()
    {
        Main.instance.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        Main.QueueMainThreadAction(() => { rt = new RenderTarget2D(Main.graphics.GraphicsDevice, 32, 32); });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => rt.Dispose());
    }
}
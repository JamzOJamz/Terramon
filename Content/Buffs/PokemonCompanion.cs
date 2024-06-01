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
    private static RenderTarget2D _rt;
    private Asset<Texture2D> _starIconTexture;
    private Asset<Texture2D> _templateTexture;

    public override string Texture => "Terramon/Assets/Buffs/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
        _templateTexture = ModContent.Request<Texture2D>(TemplatePath);
        _starIconTexture = ModContent.Request<Texture2D>(StarIconPath);
    }

    public override void Update(Player player, ref int buffIndex)
    {
        // Prevent the buff from expiring
        player.buffTime[buffIndex] = int.MaxValue;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        spriteBatch.End();

        // Use the render target
        var gd = Main.graphics.GraphicsDevice;
        gd.SetRenderTarget(_rt);
        gd.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        var player = TerramonPlayer.LocalPlayer;

        // Draw the template texture
        spriteBatch.Draw(_templateTexture.Value, Vector2.Zero, new Rectangle(0, 0, 32, 32), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the pokeball icon
        var ballId = player.GetActivePokemon()?.Ball ?? BallID.PokeBall;
        var ballName = BallID.Search.GetName(ballId) + "Projectile";
        var pokeballIconTexture = ModContent.Request<Texture2D>(PokeballIconPrefix + ballName);
        spriteBatch.Draw(pokeballIconTexture.Value, new Vector2(0, 2),
            pokeballIconTexture.Frame(verticalFrames: 4, frameY: 2), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the star icon
        spriteBatch.Draw(_starIconTexture.Value, new Vector2(14, 14),
            new Rectangle(0, 0, _starIconTexture.Width(), _starIconTexture.Height()), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        spriteBatch.End();

        // Reset the render target
        gd.SetRenderTarget(null);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        // Draw the render target
        spriteBatch.Draw(_rt, drawParams.Position, drawParams.DrawColor);

        return false;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        var player = TerramonPlayer.LocalPlayer;
        tip = Description.Format(player.GetActivePokemon()?.DisplayName);
        rare = ItemRarityID.Expert;
    }

    public override void Load()
    {
        if (Main.dedServ) return;
        Main.instance.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        Main.QueueMainThreadAction(() => { _rt = new RenderTarget2D(Main.graphics.GraphicsDevice, 32, 32); });
    }

    public override void Unload()
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() => _rt.Dispose());
    }
}
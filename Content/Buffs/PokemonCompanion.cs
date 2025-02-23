using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Core.Loaders;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace Terramon.Content.Buffs;

public class PokemonCompanion : ModBuff
{
    private const string StarIconPath = "Terramon/Assets/Buffs/IconStar";
    private static RenderTarget2D _rt;
    private Asset<Texture2D> _starIconTexture;

    public override string Texture => "Terraria/Images/Buff";

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
        if (!Main.dedServ)
            _starIconTexture = ModContent.Request<Texture2D>(StarIconPath);
    }

    public override void Update(Player player, ref int buffIndex)
    {
        // Prevent the buff from expiring
        player.buffTime[buffIndex] = 18000;

        // Spawn the pet if needed
        var id = player.GetModPlayer<TerramonPlayer>().GetActivePokemon()?.ID ?? 0;
        if (id == 0) return;
        var unused = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused,
            PokemonEntityLoader.IDToPetType[id]);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        spriteBatch.End();

        // Use the render target
        var gd = Main.graphics.GraphicsDevice;
        gd.SetRenderTarget(_rt);
        gd.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);

        var player = TerramonPlayer.LocalPlayer;

        // Draw the template texture
        spriteBatch.Draw(TextureAssets.Buff[Type].Value, Vector2.Zero, new Rectangle(0, 0, 32, 32), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw the pokeball icon
        var ballId = player.GetActivePokemon()?.Ball ?? BallID.PokeBall;
        spriteBatch.Draw(BallAssets.GetBallIcon(ballId).Value, new Vector2(4, 6),
            null, Color.White,
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
        var activePokemon = player.GetActivePokemon();
        if (activePokemon != null)
            tip = string.Format(tip, activePokemon.DisplayName);
        if (ModContent.GetInstance<ClientConfig>().RainbowBuffText)
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
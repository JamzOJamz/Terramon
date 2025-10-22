using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.TurnBased;
public sealed class ParticipantPanel : UIElement
{
    public static Asset<Texture2D> HPBar { get; private set; } 
    public static Asset<Texture2D> EXPBar { get; private set; }
    public static Asset<Texture2D> GenderIcon { get; private set; }
    public static Asset<Texture2D> BallSlots { get; private set; }
    public static Asset<Texture2D> PanelTexture { get; private set; }

    public PokemonData CurrentMon;
    private float _hpVisual;
    private Func<float> _getPixelRatio;
    /// <summary>
    /// Use instead of <see cref="UIElement.HAlign"/>
    /// </summary>
    public float SideFactor
    {
        get => (HAlign - 0.5f) * 2f;
        set => HAlign = (value + 1f) * 0.5f;
    }
    static ParticipantPanel()
    {
        HPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/HPBar");
        EXPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/EXPBar");
        GenderIcon = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/Gender");
        BallSlots = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/BallSlots_Simple");
        PanelTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/PlayerPanel_Simple");
    }
    public ParticipantPanel(Func<float> getPixelRatio = null)
    {
        _getPixelRatio = getPixelRatio;
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        DynamicSpriteFont bigFont = FontAssets.DeathText.Value;
        DynamicSpriteFont smallFont = FontAssets.MouseText.Value;

        float zoom = _getPixelRatio?.Invoke() ?? 1f;
        var dims = GetDimensions();
        float third = PanelTexture.Height() * 0.5f;
        float halfThird = third * 0.5f;
        float sideFactor = SideFactor;
        float sideShift = halfThird * sideFactor;
        var parallelogramRect = new Rectangle((int)(dims.X + - halfThird + sideShift), (int)dims.Y, (int)(dims.Width + third), (int)dims.Height);
        DynamicPixelRatioElement.DrawAdjustableParallelogram(spriteBatch, PanelTexture.Value, parallelogramRect, Color.White, zoom);

        var monName = CurrentMon?.DisplayName ?? "???";
        var monGender = (int)(CurrentMon?.Gender ?? Gender.Unspecified) - 1;
        var monLevel = CurrentMon?.Level ?? 0;
        var monHpFactor = CurrentMon is null ? 0f : CurrentMon.HP / (float)CurrentMon.MaxHP;

        Vector2 drawTextPosition = new(dims.X + 64f + sideShift, dims.Y + 16f);
        float textDrawScale = 0.7f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monName, drawTextPosition, Color.White, 0f, Vector2.Zero, new Vector2(0.7f));

        if (monGender != -1)
        {
            Vector2 drawGenderPosition = drawTextPosition;
            drawGenderPosition.X += bigFont.MeasureString(monName + " ").X * textDrawScale;

            spriteBatch.Draw(GenderIcon.Value, drawGenderPosition, GenderIcon.Frame(2, 1, monGender), Color.White);
        }

        float hpWidth = dims.Width - 96f;
        Vector2 drawHpPosition = new(dims.X + (dims.Width * 0.5f) - (hpWidth * 0.5f), dims.Y + dims.Height - 48f);
        if (_hpVisual < monHpFactor)
            TestBattleUI.Step(ref _hpVisual, monHpFactor, 0.01f);
        else
            _hpVisual = monHpFactor;

        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth, Color.Black, zoom);
        if (_hpVisual != monHpFactor)
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * _hpVisual, Color.Red, zoom);
        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * monHpFactor, Color.White, zoom);

        // spriteBatch.Draw(TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.White * 0.5f);
    }
}

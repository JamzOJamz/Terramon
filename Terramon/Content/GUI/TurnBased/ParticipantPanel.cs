using Humanizer;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.TurnBased;
public sealed class ParticipantPanel(Func<float> getPixelRatio = null) : UIElement
{
    public static Asset<Texture2D> HPBar { get; private set; } 
    public static Asset<Texture2D> EXPBar { get; private set; }
    public static Asset<Texture2D> GenderIcon { get; private set; }
    public static Asset<Texture2D> BallSlots { get; private set; }
    public static Asset<Texture2D> PanelTexture { get; private set; }

    public PokemonData CurrentMon;
    public bool DrawEXPBar;
    private float _hpVisual;
    private readonly Func<float> _getPixelRatio = getPixelRatio;
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

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        DynamicSpriteFont bigFont = FontAssets.DeathText.Value;
        DynamicSpriteFont smallFont = FontAssets.MouseText.Value;

        float zoom = _getPixelRatio?.Invoke() ?? 1f;
        var dims = GetDimensions();
        float third = dims.Height * 0.5f;
        float halfThird = third * 0.5f;
        float sideFactor = SideFactor;
        float sideShift = halfThird * sideFactor;
        float extra = 1.2f;
        var parallelogramRect = new Rectangle((int)(dims.X - halfThird + sideShift * extra), (int)dims.Y, (int)(dims.Width + third * extra), (int)dims.Height);
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

        string lv = "Lv. ";
        float levelNumberScale = 0.85f;
        Vector2 levelNumberSize = bigFont.MeasureString(monLevel.ToString()) * levelNumberScale;
        Vector2 levelLabelSize = smallFont.MeasureString(lv);
        Vector2 drawLevelPosition = new(dims.X + dims.Width - 64f, dims.Y + levelNumberSize.Y + 6f);
        Vector2 drawLevelLabelPosition = drawLevelPosition;

        drawLevelPosition.Y -= levelNumberSize.Y;
        drawLevelLabelPosition.X -= levelNumberSize.X + 8f;
        drawLevelLabelPosition.Y -= levelLabelSize.Y * 1.4f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monLevel.ToString(), drawLevelPosition, Color.White, 0f, Vector2.Zero, new Vector2(levelNumberScale));
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, lv, drawLevelLabelPosition, Color.White, 0f, Vector2.Zero, Vector2.One);

        float hpWidth = dims.Width - 84f;
        float drawHpY = dims.Y + dims.Height - (DrawEXPBar ? 68f : 52f);
        float parallelogramCenterForHp = GetParallelogramCenter(drawHpY + HPBar.Height() * 0.5f, in parallelogramRect);
        Vector2 drawHpPosition = new(parallelogramCenterForHp - (hpWidth * 0.5f) - (sideShift * 0.75f), drawHpY);
        if (_hpVisual < monHpFactor)
            Step(ref _hpVisual, monHpFactor, 0.01f);
        else
            _hpVisual = monHpFactor;

        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth, Color.Black, zoom);
        if (_hpVisual != monHpFactor)
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * _hpVisual, Color.Red, zoom);
        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * monHpFactor, Color.White, zoom);

        string hpDisplay = CurrentMon is null ? "??? / ???" : $"HP: {CurrentMon.HP} / {CurrentMon.MaxHP}";
        Vector2 hpDisplaySize = smallFont.MeasureString(hpDisplay);
        Vector2 hpDisplayPosition = drawHpPosition + new Vector2(hpWidth * 0.5f, (HPBar.Height() + 8) * 0.5f * zoom) - hpDisplaySize * 0.5f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, hpDisplay, hpDisplayPosition, Color.White, 0f, Vector2.Zero, Vector2.One);

        if (DrawEXPBar)
        {
            float xDifference = 128f;
            Vector2 expDrawPos = drawHpPosition + new Vector2(xDifference, HPBar.Height());
            float expWidth = hpWidth - xDifference - HPBar.Width() / 3;
            float expFactor = CurrentMon is null ? 0f : monLevel == Terramon.MaxPokemonLevel ? 1f : CurrentMon.TotalEXP / (float)ExperienceLookupTable.GetLevelTotalExp(monLevel + 1, CurrentMon.Schema.GrowthRate);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth, Color.Black, zoom);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth * expFactor, Color.White, zoom);
        }

        // spriteBatch.Draw(TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.White * 0.5f);
    }

    public static float GetParallelogramCenter(float y, in Rectangle bounds)
    {
        if (y < bounds.Y || y > bounds.Y + bounds.Height)
            return y;
        y -= bounds.Y; // makes it relative, so we don't need to use bounds.Y or bounds.X anymore
        y /= bounds.Height; // makes it range from 0f - 1f
        float spaceAtTop = bounds.Height * 0.5f;
        float space = spaceAtTop * (1f - y);
        return bounds.X + space + ((bounds.Width - spaceAtTop) * 0.5f);
    }

    public static void Step(ref float f, float target, float step)
    {
        if (f > target)
            f = Math.Max(f - step, target);
        else if (f < target)
            f = Math.Min(f + step, target);
    }
}

using ReLogic.Content;
using ReLogic.Graphics;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.TurnBased;

public sealed class ParticipantPanel(Func<float> getPixelRatio = null) : UIElement
{
    private readonly float[] _ballSlotXPositions = new float[6];
    private readonly Func<float> _getPixelRatio = getPixelRatio;
    private float _hpVisual;
    private PokemonData _currentMon;
    public PokemonData CurrentMon
    {
        get => _currentMon;
        set
        {
            if (_currentMon == value)
                return;
            _currentMon = value;
            _hpVisual = 0f;
        }
    }
    public bool DrawBallSlots;
    public bool DrawEXPBar;

    static ParticipantPanel()
    {
        HPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/HPBar");
        EXPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/EXPBar");
        GenderIcon = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/Gender");
        BallSlots = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/BallSlots_Simple");
        PanelTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/PlayerPanel_Simple");
    }

    public static Asset<Texture2D> HPBar { get; }
    public static Asset<Texture2D> EXPBar { get; }
    public static Asset<Texture2D> GenderIcon { get; }
    public static Asset<Texture2D> BallSlots { get; }
    public static Asset<Texture2D> PanelTexture { get; }

    private static SoundStyle Ping { get; } = new("Terramon/Sounds/battle_tb_ping")
        { Volume = 0.3f, MaxInstances = 0 };

    private static SoundStyle PingEmpty { get; } = new("Terramon/Sounds/battle_tb_empty")
        { Volume = 0.3f, MaxInstances = 0 };

    private static SoundStyle Start { get; } = new("Terramon/Sounds/battle_tb_start")
        { Volume = 0.3f };

    /// <summary>
    ///     Use instead of <see cref="UIElement.HAlign" />
    /// </summary>
    public float SideFactor
    {
        get => (HAlign - 0.5f) * 2f;
        set => HAlign = (value + 1f) * 0.5f;
    }

    public void Animate(int ticks)
    {
        const int animationStart = 190;
        const int ticksPerSlot = 6;
        const int ticksForWholeAnimation = ticksPerSlot * 6;
        const int animationEnd = animationStart + ticksForWholeAnimation;
        const float initialPosition = -42f;
        
        if (ticks == animationStart - 25)
            SoundEngine.PlaySound(Start);

        if (!DrawBallSlots || ticks > animationEnd || ticks < animationStart)
            return;

        for (int i = 0; i < _ballSlotXPositions.Length; i++)
        {
            int startTick = animationStart + (i * ticksPerSlot);

            if (ticks == startTick + ticksPerSlot - 1)
                SoundEngine.PlaySound(TerramonPlayer.LocalPlayer.Party[i] != null ? Ping : PingEmpty);

            int endTick = startTick + ticksPerSlot;
            float targetXPosition = -8f + (i * 26f);
            float lerp = Utils.GetLerpValue(startTick, endTick, ticks, true);

            _ballSlotXPositions[i] =
                float.Lerp(initialPosition, targetXPosition, Tween.ApplyEasing(Ease.OutBack, lerp));
        }
    }

    public void ResetAnimation() => Array.Fill(_ballSlotXPositions, -42f);
    
    private static readonly Color BackColor = new(10, 9, 22);

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
        bool right = sideFactor > 0f;
        float extra = 1.2f;
        // float the = (MathF.Sin((float)Main.timeForVisualEffects * 0.1f) + 1f) * 16f;
        var parallelogramRect = new Rectangle((int)(dims.X - halfThird + sideShift * extra), (int)dims.Y,
            (int)(dims.Width + third * extra), (int)dims.Height);
        DynamicPixelRatioElement.DrawAdjustableParallelogram(spriteBatch, PanelTexture.Value, parallelogramRect,
            Color.White, zoom);

        var monName = CurrentMon?.DisplayName ?? "???";
        var monGender = (int)(CurrentMon?.Gender ?? Gender.Unspecified) - 1;
        var monLevel = CurrentMon?.Level ?? 0;
        var monHpFactor = CurrentMon is null ? 0f : CurrentMon.HP / (float)CurrentMon.MaxHP;

        Vector2 drawTextPosition = new(dims.X + 64f + sideShift, dims.Y + 16f);
        float textDrawScale = 0.7f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monName, drawTextPosition, Color.White, 0f,
            Vector2.Zero, new Vector2(0.7f));

        if (monGender != -1)
        {
            Vector2 drawGenderPosition = drawTextPosition;
            drawGenderPosition.X += bigFont.MeasureString(monName + " ").X * textDrawScale;
            
            // Remove floating points from draw position
            drawGenderPosition = drawGenderPosition.Floor();
            
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

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monLevel.ToString(), drawLevelPosition,
            Color.White, 0f, Vector2.Zero, new Vector2(levelNumberScale));
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, lv, drawLevelLabelPosition, Color.White, 0f,
            Vector2.Zero, Vector2.One);

        var hpBarHeight = HPBar.Height() - 2;

        float hpWidth = dims.Width - 84f;
        float drawHpY = dims.Y + dims.Height - (DrawEXPBar ? 68f : 52f);
        float parallelogramCenterForHp = GetParallelogramCenter(drawHpY + hpBarHeight * 0.5f, in parallelogramRect);
        Vector2 drawHpPosition = new(parallelogramCenterForHp - (hpWidth * 0.5f) - (sideShift * 0.75f), drawHpY);
        
        // Remove floating points from draw position
        drawHpPosition = drawHpPosition.Floor();
        
        if (monHpFactor < _hpVisual)
            Step(ref _hpVisual, monHpFactor, 0.01f);
        else
            _hpVisual = monHpFactor;

        var colorShader = ShaderAssets.FadeToColor.Shader;
        var colorParam = colorShader.Parameters["uColor"];
        colorShader.Parameters["uOpacity"].SetValue(1f);

        colorParam.SetValue(BackColor.ToVector3());

        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth, BackColor,
            zoom, colorShader);
        if (_hpVisual != monHpFactor)
        {
            colorParam.SetValue(Color.Red.ToVector3());
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * _hpVisual,
                Color.Red, zoom, colorShader);
        }
        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * monHpFactor,
            Color.White, zoom);

        string hpDisplay = CurrentMon is null ? "??? / ???" : $"HP: {CurrentMon.HP} / {CurrentMon.MaxHP}";
        Vector2 hpDisplaySize = smallFont.MeasureString(hpDisplay);
        Vector2 hpDisplayPosition = drawHpPosition + new Vector2(hpWidth * 0.5f, (hpBarHeight + 8) * 0.5f * zoom) -
                                    hpDisplaySize * 0.5f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, hpDisplay, hpDisplayPosition, Color.White,
            0f, Vector2.Zero, Vector2.One);

        if (DrawEXPBar)
        {
            float xDifference = 128f;
            Vector2 expDrawPos = drawHpPosition + new Vector2(xDifference, hpBarHeight);
            float expWidth = hpWidth - xDifference - HPBar.Width() / 3;
            float expFactor = CurrentMon is null ? 0f :
                monLevel == Terramon.MaxPokemonLevel ? 1f :
                CurrentMon.TotalEXP /
                (float)ExperienceLookupTable.GetLevelTotalExp(monLevel + 1, CurrentMon.Schema.GrowthRate);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth, BackColor,
                zoom);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth * expFactor,
                Color.White, zoom);
        }

        if (DrawBallSlots)
        {
            Texture2D ballSlots = BallSlots.Value;
            PokemonData[] party = TerramonPlayer.LocalPlayer.Party;
            Rectangle frame = ballSlots.Frame(3, 1, 2);
            int next = 4;
            if (!right)
            {
                frame.X -= frame.Width;
                next = 0;
            }

            for (int i = _ballSlotXPositions.Length - 1; i >= 0; i--)
            {
                if (i == next)
                    frame.X -= frame.Width;
                float slotPosition = _ballSlotXPositions[i];
                if (i != 0 && slotPosition == -8)
                    continue;
                Vector2 drawPosition = new(dims.X + slotPosition, dims.Y + dims.Height);

                spriteBatch.Draw(ballSlots, drawPosition, frame, Color.White, 0f, Vector2.Zero, zoom,
                    SpriteEffects.None, 0f);

                if (party[i] is null)
                    continue;

                Texture2D bola = Terramon.Instance.Assets.Request<Texture2D>("Assets/Items/PokeBalls/PokeBallMiniItem")
                    .Value;

                drawPosition.X += 12f;
                drawPosition.Y += 6f;
                spriteBatch.Draw(bola, drawPosition, null, Color.White, 0f, Vector2.Zero, zoom, SpriteEffects.None, 0f);
            }
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
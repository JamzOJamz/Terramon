using rail;
using ReLogic.Content;
using Terramon.Content.NPCs;
using Terramon.ID;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Terramon.Content.GUI;
public sealed class TestBattleUI : UIState
{
    public static TestBattleUI Instance = new();
    public static Asset<Texture2D> Forest;
    private static UIElement _optionsPanel;
    private static UIElement _movesPanel;
    static TestBattleUI()
    {
        // Create options panel
        _optionsPanel = new();
        _optionsPanel.Left.Percent = _optionsPanel.Width.Percent = 0.5f;
        _optionsPanel.Height.Percent = 1f;
        for (ButtonType i = ButtonType.Fight; i <= ButtonType.Run; i++)
        {
            int xFactor = (int)i % 2;
            int yFactor = (int)i / 2;
            DynamicPixelRatioElement button = new($"Terramon/Assets/GUI/TurnBased/{i}Button", buttonLike: true);
            button.Left.Percent = xFactor * 0.5f;
            button.Top.Percent = yFactor * 0.5f;
            button.Width.Percent = button.Height.Percent = 0.5f;
            button.OnLeftClick += i switch
            {
                ButtonType.Fight => FightButton,
                ButtonType.Bag => BagButton,
                ButtonType.Pokemon => PokemonButton,
                ButtonType.Run => RunButton,
                _ => null,
            };
            UIText label = new(Terramon.Instance.GetLocalization($"GUI.TurnBased.ButtonLabels.{i}"), 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            _optionsPanel.Append(button);
        }

        // Create moves panel (blank)
        _movesPanel = new();
        _movesPanel.Width.Percent = _movesPanel.Height.Percent = 1f;
        for (int i = 0; i < 4; i++)
        {
            float xFactor = i % 2 * 0.5f;
            float yFactor = i / 2 * 0.5f;
            DynamicPixelRatioElement button = new(null, null, buttonLike: true);
            button.Left.Percent = xFactor;
            button.Top.Percent = yFactor;
            button.Width.Percent = button.Height.Percent = 0.5f;
            UIText label = new(string.Empty, 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            _movesPanel.Append(button);
        }
    }
    public override void OnInitialize()
    {
        // Forest ??= Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/Forest_NineSlice");

        /*
        var parallelogram = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/PlayerPanel_Simple");
        ParallelogramElement player = new(parallelogram, -96f);
        player.Width.Pixels = 378f;
        player.Height.Pixels = 128f;
        player.Top.Pixels = 256f;
        Append(player);
        */

        DynamicPixelRatioElement p = new("Terramon/Assets/GUI/TurnBased/Simple", true);
        p.Width.Percent = 0.5f;
        p.Height.Percent = 0.3f;
        p.HAlign = 0.5f;
        p.VAlign = 0.9f;
        p.SetPadding(32f);

        p.Append(_optionsPanel);

        Append(p);
    }
    public static void UpdateMoves(PokemonData data = null)
    {
        data ??= TerramonPlayer.LocalPlayer.GetActivePokemon();
        if (data is null)
            return;
        int cur = 0;
        foreach (var moveButton in _movesPanel.Children)
        {
            var actual = (DynamicPixelRatioElement)moveButton;
            var label = (UIText)actual.Children.First();

            var move = data.Moves[cur];
            actual.Color = Main.rand.NextFromCollection(Enum.GetValues<PokemonType>().ToList()).GetColor();
            actual.UpdateTextures($"Terramon/Assets/GUI/TurnBased/MoveButton_Normal");
            actual.OnLeftClick += (evt, listeningElement) => ClickMoveButton(evt, listeningElement, in move);
            label.SetText(move.ID.ToString());

            cur++;
        }
    }

    private static void ClickMoveButton(UIMouseEvent evt, UIElement listeningElement, in MoveData move)
    {

    }

    private static void FightButton(UIMouseEvent evt, UIElement listeningElement)
    {
        // for quick asset testing
        /*
        string path = Terramon.Instance.SourceFolder + """\Assets\GUI\TurnBased\Forest_NineSlice.png""";
        // Main.NewText(Path.Exists(path));
        using Stream stream = File.OpenRead(path);
        Forest = Terramon.Instance.Assets.CreateUntracked<Texture2D>(stream, path);
        */
        var parent = listeningElement.Parent.Parent;
        parent.RemoveChild(_optionsPanel);
        parent.Append(_movesPanel);
        UpdateMoves();
        parent.RecalculateChildren();
    }
    private static void BagButton(UIMouseEvent evt, UIElement listeningElement)
    {

    }
    private static void PokemonButton(UIMouseEvent evt, UIElement listeningElement)
    {

    }
    private static void RunButton(UIMouseEvent evt, UIElement listeningElement)
    {
        var plr = TerramonPlayer.LocalPlayer;
        var battle = plr.Battle;
        if (battle != null)
        {
            battle.BattleStream?.Dispose();
            if (battle.WildNPCIndex.HasValue)
                ((PokemonNPC)Main.npc[battle.WildNPCIndex.Value].ModNPC).EndBattle();
            plr.Battle = null;
        }
        PartyDisplay.Sidebar.Open();
        IngameFancyUI.Close();
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        /*
        RemoveAllChildren();
        OnInitialize();
        Recalculate();
        */
    }
}
public sealed class ParallelogramElement : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private readonly float _xOffset;
    public ParallelogramElement(Asset<Texture2D> texture, float xOffset = 0f)
    {
        _texture = texture;
        _xOffset = xOffset;
        OverrideSamplerState = SamplerState.PointClamp;
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_texture is null)
            return;
        float zoom = Main.GameZoomTarget;
        var bounds = GetDimensions();
        Texture2D tex = _texture.Value!;
        float textureXOffset = (tex.Width - (tex.Width / zoom)) * zoom;
        Vector2 pos = new(bounds.X - (textureXOffset - _xOffset), bounds.Y);
        float yOffset = (tex.Height / zoom) - bounds.Height;
        DrawAdjustableParallelogram(spriteBatch, tex, pos, Color.White, yOffset, zoom);
        // spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds.ToRectangle(), Color.White * 0.5f);
    }
    public static void DrawAdjustableParallelogram(SpriteBatch spriteBatch, Texture2D tex, Vector2 position, Color col, float bottomOffset, float scale)
    {
        Rectangle frame = tex.Frame(1, 2);
        bottomOffset *= scale;
        spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        position += new Vector2(bottomOffset * -0.5f, (frame.Height * scale) + bottomOffset);
        frame.Y += frame.Height;
        spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
public sealed class DynamicPixelRatioElement : UIElement
{
    private Asset<Texture2D> _nineSlice;
    private Asset<Texture2D> _corners;
    private bool _noStretching;
    private bool _buttonLike;
    private bool _hovered;
    private float _hoverTime;
    public Color Color { get; set; } = Color.White;
    public DynamicPixelRatioElement(string basePath, bool noStretching = false, bool buttonLike = false)
    {
        UpdateTextures(basePath, noStretching);
        SetupCommon(buttonLike);
    }

    private static void Unhovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        dyn._hovered = false;
    }

    private static void Hovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        dyn._hovered = true;
    }

    public DynamicPixelRatioElement(Asset<Texture2D> nineSlice, Asset<Texture2D> corners, bool noStretching = false, bool buttonLike = false)
    {
        UpdateTextures(nineSlice, corners, noStretching);
        SetupCommon(buttonLike);
    }
    private void SetupCommon(bool buttonLike)
    {
        OverrideSamplerState = SamplerState.PointClamp;
        if (!buttonLike)
            return;
        _buttonLike = true;
        OnMouseOver += Hovered;
        OnMouseOut += Unhovered;
    }
    public DynamicPixelRatioElement UpdateTextures(string newBasePath, bool noStretching = false)
    {
        if (ModContent.RequestIfExists(newBasePath + "_NineSlice", out _nineSlice))
            _noStretching = noStretching;
        else if (ModContent.RequestIfExists(newBasePath + "_NineSlice_Stretch", out _nineSlice))
            _noStretching = false;
        ModContent.RequestIfExists(newBasePath + "_Corners", out _corners);
        return this;
    }
    public DynamicPixelRatioElement UpdateTextures(Asset<Texture2D> nineSlice, Asset<Texture2D> corners, bool noStretching = false)
    {
        _nineSlice = nineSlice;
        _corners = corners;
        _noStretching = noStretching;
        return this;
    }
    public override void Update(GameTime gameTime)
    {
        if (_hovered && _hoverTime < 1f)
            _hoverTime = Math.Min(_hoverTime + 0.1f, 1f);
        else if (!_hovered && _hoverTime > 0f)
            _hoverTime = Math.Max(_hoverTime - 0.1f, 0f);
        base.Update(gameTime);
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetDimensions().ToRectangle();
        Color drawColor = Color;
        if (_buttonLike)
        {
            int infl = (int)(_hoverTime * 2f);
            bounds.Inflate(infl, infl);
            drawColor.A = (byte)((1f - _hoverTime) * byte.MaxValue);
        }
        if (_nineSlice != null)
            DrawAdjustableBox(spriteBatch, _nineSlice.Value, bounds, drawColor, Main.GameZoomTarget, _noStretching);
        if (_corners != null)
            DrawAdjustableCorners(spriteBatch, _corners.Value, bounds, drawColor, Main.GameZoomTarget);
    }
    public static void DrawAdjustableCorners(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col, float scale)
    {
        Rectangle frame = tex.Frame(2, 1);
        spriteBatch.Draw(tex, rect.TopLeft(), frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        Vector2 og = new(tex.Width * 0.5f, tex.Height);
        spriteBatch.Draw(tex, rect.BottomRight(), frame, col, 0f, og, scale, SpriteEffects.None, 0f);
    }
    public static void DrawAdjustableBox(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col, float pxScale, bool noStretching = false, SkipDrawBoxSegment flags = SkipDrawBoxSegment.None)
    {
        Vector2 quadSize = new Vector2(tex.Width / 3f, tex.Height / 3f) * pxScale;
        var xScale = (rect.Width - quadSize.X * 2) / quadSize.X;
        var yScale = (rect.Height - quadSize.Y * 2) / quadSize.Y;

        void DrawSegment(in Vector2 position, in Rectangle frame, float xSize = 1f, float ySize = 1f)
        {
            spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, new Vector2(xSize * pxScale, ySize * pxScale), SpriteEffects.None, 0f);
        }
        void DrawWrapped(Vector2 position, Rectangle frame, float scale, bool down)
        {
            for (float currentScale = 0f; currentScale < scale; currentScale++)
            {
                if (currentScale != 0f)
                {
                    if (down)
                        position.Y += quadSize.Y;
                    else
                        position.X += quadSize.X;
                }
                float overshoot = currentScale + 1f;
                if (overshoot > scale)
                {
                    if (down)
                        frame.Height = (int)MathF.Ceiling(frame.Height * (scale - currentScale));
                    else
                        frame.Width = (int)MathF.Ceiling(frame.Width * (scale - currentScale));
                }
                DrawSegment(in position, in frame);
            }
        }
        // Draw center
        if ((flags & SkipDrawBoxSegment.CenterCenter) == 0)
        {
            Rectangle centerFrame = tex.Frame(3, 3, 1, 1);
            DrawSegment(new Vector2(rect.X + quadSize.X, rect.Y + quadSize.Y), centerFrame, xScale, yScale);
        }

        // Draw sides
        if ((flags & SkipDrawBoxSegment.TopCenter) == 0)
        {
            Rectangle topSideFrame = tex.Frame(3, 3, 1, 0);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y);
            if (noStretching)
                DrawWrapped(pos, topSideFrame, xScale, false);
            else
                DrawSegment(pos, topSideFrame, xScale, 1f);
        }

        if ((flags & SkipDrawBoxSegment.CenterLeft) == 0)
        {
            Rectangle leftSideFrame = tex.Frame(3, 3, 0, 1);
            Vector2 pos = new(rect.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, leftSideFrame, yScale, true);
            else
                DrawSegment(pos, leftSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.CenterRight) == 0)
        {
            Rectangle rightSideFrame = tex.Frame(3, 3, 2, 1);
            Vector2 pos = new(rect.X + rect.Width - quadSize.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, rightSideFrame, yScale, true);
            else
                DrawSegment(pos, rightSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.BottomCenter) == 0)
        {
            Rectangle bottomSideFrame = tex.Frame(3, 3, 1, 2);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y + rect.Height - quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, bottomSideFrame, xScale, false);
            else
                DrawSegment(pos, bottomSideFrame, xScale, 1f);
        }

        // Draw corners

        if ((flags & SkipDrawBoxSegment.TopLeft) == 0)
        {
            Rectangle topLeftCorner = tex.Frame(3, 3, 0, 0);
            DrawSegment(new Vector2(rect.X, rect.Y), topLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.TopRight) == 0)
        {
            Rectangle topRightCorner = tex.Frame(3, 3, 2, 0);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y), topRightCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomLeft) == 0)
        {
            Rectangle bottomLeftCorner = tex.Frame(3, 3, 0, 2);
            DrawSegment(new Vector2(rect.X, rect.Y + rect.Height - quadSize.Y), bottomLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomRight) == 0)
        {
            Rectangle bottomRightCorner = tex.Frame(3, 3, 2, 2);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y + rect.Height - quadSize.Y), bottomRightCorner);
        }
    }
}
internal enum ButtonType
{
    Fight,
    Bag,
    Pokemon,
    Run,
}
[Flags]
public enum SkipDrawBoxSegment
{
    None = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 4,
    CenterLeft = 8,
    CenterCenter = 16,
    CenterRight = 32,
    BottomLeft = 64,
    BottomCenter = 128,
    BottomRight = 256,
}
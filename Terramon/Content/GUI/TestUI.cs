using ReLogic.Content;
using Terramon.Core.Loaders.UILoading;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Terramon.Content.GUI;
public sealed class TestBattleUI : SmartUIState
{
    public static Asset<Texture2D> Forest;
    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }
    public override bool Visible => false;
    public override void OnInitialize()
    {
        Forest ??= Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/Forest_NineSlice");
        DynamicPixelRatioElement p = new(Forest, null, true);
        p.Width.Percent = 0.5f;
        p.Height.Percent = 0.3f;
        p.HAlign = 0.5f;
        p.VAlign = 0.9f;
        p.SetPadding(32f);

        for (ButtonType i = ButtonType.Fight; i <= ButtonType.Run; i++)
        {
            int xFactor = (int)i % 2;
            int yFactor = (int)i / 2;
            DynamicPixelRatioElement button = new($"Terramon/Assets/GUI/TurnBased/{i}Button");
            button.Left.Percent = 0.5f + (xFactor * 0.25f);
            button.Top.Percent = yFactor * 0.5f;
            button.Width.Percent = 0.25f;
            button.Height.Percent = 0.5f;
            button.OnLeftClick += i switch
            {
                ButtonType.Fight => FightButton,
                _ => null,
            };
            UIText label = new(Terramon.Instance.GetLocalization($"GUI.TurnBased.ButtonLabels.{i}"), 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            p.Append(button);
        }
        Append(p);
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
public sealed class DynamicPixelRatioElement : UIElement
{
    private readonly Asset<Texture2D> _nineSlice;
    private readonly Asset<Texture2D> _corners;
    private readonly bool _noStretching;
    public DynamicPixelRatioElement(string basePath, bool noStretching = false)
    {
        ModContent.RequestIfExists(basePath + "_NineSlice", out _nineSlice);
        ModContent.RequestIfExists(basePath + "_Corners", out _corners);
        _noStretching = noStretching;
        OverrideSamplerState = SamplerState.PointClamp;
    }
    public DynamicPixelRatioElement(Asset<Texture2D> nineSlice, Asset<Texture2D> corners, bool noStretching = false)
    {
        _nineSlice = nineSlice;
        _corners = corners;
        _noStretching = noStretching;
        OverrideSamplerState = SamplerState.PointClamp;
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetDimensions().ToRectangle();
        if (_nineSlice != null)
            DrawAdjustableBox(spriteBatch, _nineSlice.Value, bounds, Color.White, Main.GameZoomTarget, _noStretching);
        if (_corners != null)
            DrawAdjustableCorners(spriteBatch, _corners.Value, bounds, Color.White, Main.GameZoomTarget);
    }
    public static void DrawAdjustableCorners(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col, float scale)
    {
        Rectangle frame = tex.Frame(2, 1);
        spriteBatch.Draw(tex, rect.TopLeft(), frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        frame.X += tex.Width / 2;
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
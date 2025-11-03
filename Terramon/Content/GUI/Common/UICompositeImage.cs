using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Terramon.Content.GUI.Common;

public class UICompositeImage : UIImage, ILoadable
{
    private static readonly Dictionary<Point16, RenderTarget2D> RenderTargetCache = [];

    private static readonly UIElement DummyElement = new();
    private RenderTarget2D _rt;
    private CalculatedStyle _storedDimensions;

    public Color CompositeColor = Color.White;

    protected UICompositeImage(Asset<Texture2D> texture, int rtSizeX, int rtSizeY) : base(texture)
    {
        var rtSize = new Point16(rtSizeX, rtSizeY);
        if (RenderTargetCache.TryGetValue(rtSize, out _rt))
            return;
        Main.QueueMainThreadAction(() =>
        {
            _rt = new RenderTarget2D(Main.graphics.GraphicsDevice, rtSizeX, rtSizeY);
            RenderTargetCache[rtSize] = _rt;
        });
    }

    public bool IsLoadingEnabled(Mod mod) => !Main.dedServ;

    public void Load(Mod mod)
    {
    }

    public void Unload()
    {
        DisposeRenderTargets();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Reparent to dummy element for correct drawing behaviour
        var oldParent = Parent;
        Parent = DummyElement;

        // Spoof position
        var oldTop = Top;
        var oldLeft = Left;
        Top.Set(0, 0f);
        Left.Set(0, 0f);
        
        // Store dimensions for ContainsPoint
        _storedDimensions = _dimensions;

        Recalculate();

        // This is necessary to avoid funky RenderTarget stuff with other mods that might set this back to DiscardContents
        Main.instance.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        spriteBatch.End();

        // Use the render target
        var gd = Main.graphics.GraphicsDevice;
        gd.SetRenderTarget(_rt);
        gd.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        base.Draw(spriteBatch);

        spriteBatch.End();

        // Reset the render target
        gd.SetRenderTarget(null);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        // Restore position
        Top = oldTop;
        Left = oldLeft;

        // Restore old parent
        Parent = oldParent;

        Recalculate();

        // Draw the render target
        var dimensions = _storedDimensions;
        var rtSize = _rt.Size();
        var drawPos = dimensions.Position() + rtSize * (1f - ImageScale) / 2f + rtSize * NormalizedOrigin;
        if (RemoveFloatingPointsFromDrawPosition)
            drawPos = drawPos.Floor();

        spriteBatch.Draw(_rt, drawPos, null, CompositeColor, Rotation, rtSize * NormalizedOrigin, ImageScale,
            SpriteEffects.None, 0f);
    }

    public override bool ContainsPoint(Vector2 point)
    {
        if (point.X > _storedDimensions.X && point.Y > _storedDimensions.Y &&
            point.X < _storedDimensions.X + _storedDimensions.Width)
            return point.Y < _storedDimensions.Y + _storedDimensions.Height;

        return false;
    }

    private static void DisposeRenderTargets()
    {
        Main.QueueMainThreadAction(() =>
        {
            foreach (var rt in RenderTargetCache.Values)
            {
                rt!.Dispose();
            }
            RenderTargetCache.Clear();
        });
    }
}
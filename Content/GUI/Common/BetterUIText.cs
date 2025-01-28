using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.Common;

public class BetterUIText : UIElement
{
    private static readonly Vector2[] ShadowDirections =
    [
        -Vector2.UnitX, // Left
        Vector2.UnitX, // Right
        -Vector2.UnitY, // Up
        Vector2.UnitY, // Down
        new Vector2(-1, -1).SafeNormalize(Vector2.Zero), // Top-left (diagonal)
        new Vector2(1, -1).SafeNormalize(Vector2.Zero), // Top-right (diagonal)
        new Vector2(-1, 1).SafeNormalize(Vector2.Zero), // Bottom-left (diagonal)
        new Vector2(1, 1).SafeNormalize(Vector2.Zero) // Bottom-right (diagonal)
    ];

    private Color _color = Color.White;
    private bool _isLarge;
    private bool _isWrapped;
    private string _lastTextReference;
    private object _text = "";
    private Vector2 _textSize = Vector2.Zero;
    private string _visibleText;
    public bool DynamicallyScaleDownToWidth;

    public BetterUIText(string text, float textScale = 1f, bool large = false)
    {
        TextOriginX = 0.5f;
        TextOriginY = 0f;
        IsWrapped = false;
        WrappedTextBottomPadding = 20f;
        InternalSetText(text, textScale, large);
    }
    
    public BetterUIText(object text, float textScale = 1f, bool large = false)
    {
        TextOriginX = 0.5f;
        TextOriginY = 0f;
        IsWrapped = false;
        WrappedTextBottomPadding = 20f;
        InternalSetText(text, textScale, large);
    }
    
    public float TextScale { get; private set; } = 1f;

    public string Text => _text.ToString();

    public float TextOriginX { get; set; }

    public float TextOriginY { get; set; }

    public float WrappedTextBottomPadding { get; set; }

    public bool IsWrapped
    {
        get => _isWrapped;
        set
        {
            _isWrapped = value;
            if (value)
                MinWidth.Set(0,
                    0); // TML: IsWrapped when true should prevent changing MinWidth, otherwise can't shrink in width due to CreateWrappedText+GetInnerDimensions logic. IsWrapped is false in ctor, so need to undo changes.
            InternalSetText(_text, TextScale, _isLarge);
        }
    }

    public Color TextColor
    {
        get => _color;
        set => _color = value;
    }

    public Color ShadowColor { get; set; } = Color.Black;

    public float ShadowSpread { get; set; } = 1.5f;

    public bool ShowTypingCaret { get; set; }

    public event Action OnInternalTextChange;

    public override void Recalculate()
    {
        InternalSetText(_text, TextScale, _isLarge);
        base.Recalculate();
    }

    public void SetText(string text)
    {
        InternalSetText(text, TextScale, _isLarge);
    }

    public void SetText(LocalizedText text)
    {
        InternalSetText(text, TextScale, _isLarge);
    }

    public void SetText(string text, float textScale, bool large)
    {
        InternalSetText(text, textScale, large);
    }

    public void SetText(LocalizedText text, float textScale, bool large)
    {
        InternalSetText(text, textScale, large);
    }
    
    public void SetTextScale(float textScale)
    {
        InternalSetText(_text, textScale, _isLarge);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        VerifyTextState();
        var innerDimensions = GetInnerDimensions();
        var position = innerDimensions.Position();
        if (_isLarge)
            position.Y -= 10f * TextScale;
        else
            position.Y -= 2f * TextScale;

        position.X += (innerDimensions.Width - _textSize.X) * TextOriginX;
        position.Y += (innerDimensions.Height - _textSize.Y) * TextOriginY;
        var num = TextScale;
        if (DynamicallyScaleDownToWidth && _textSize.X > innerDimensions.Width)
            num *= innerDimensions.Width / _textSize.X;
        
        var useText = _visibleText;
        if (ShowTypingCaret && Main.GameUpdateCount % 20 < 10)
            useText += "|";
        var value = (_isLarge ? FontAssets.DeathText : FontAssets.MouseText).Value;
        var vector = value.MeasureString(useText);
        var baseColor = ShadowColor * (_color.A / 255f);
        var origin = new Vector2(0f, 0f) * vector;
        var baseScale = new Vector2(num);
        var snippets = ChatManager.ParseMessage(useText, _color).ToArray();
        ChatManager.ConvertNormalSnippets(snippets);

        foreach (var t in ShadowDirections)
            ChatManager.DrawColorCodedString(spriteBatch, value, snippets, position + t * ShadowSpread, baseColor, 0f,
                origin, baseScale, out _, -1f, true);
        ChatManager.DrawColorCodedString(spriteBatch, value, snippets, position, Color.White, 0f, origin, baseScale,
            out var _, -1f);
    }

    private void VerifyTextState()
    {
        if ((object)_lastTextReference != Text)
            InternalSetText(_text, TextScale, _isLarge);
    }

    private void InternalSetText(object text, float textScale, bool large)
    {
        var dynamicSpriteFont = large ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;
        _text = text;
        _isLarge = large;
        TextScale = textScale;
        _lastTextReference = _text.ToString();
        _visibleText = IsWrapped
            ? dynamicSpriteFont.CreateWrappedText(_lastTextReference, GetInnerDimensions().Width / TextScale)
            : _lastTextReference;

        // TML: Changed to use ChatManager.GetStringSize() since using DynamicSpriteFont.MeasureString() ignores chat tags,
        // giving the UI element a much larger calculated size than it should have.
        var vector = ChatManager.GetStringSize(dynamicSpriteFont, _visibleText, new Vector2(1));

        var vector2 = _textSize = !IsWrapped
            ? new Vector2(vector.X, large ? 32f : 16f) * textScale
            : new Vector2(vector.X, vector.Y + WrappedTextBottomPadding) * textScale;
        if (!IsWrapped) // TML: IsWrapped when true should prevent changing MinWidth, otherwise can't shrink in width due to logic.
            MinWidth.Set(vector2.X + PaddingLeft + PaddingRight, 0f);
        MinHeight.Set(vector2.Y + PaddingTop + PaddingBottom, 0f);
        OnInternalTextChange?.Invoke();
    }
}
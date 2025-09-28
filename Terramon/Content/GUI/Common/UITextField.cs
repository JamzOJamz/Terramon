using Microsoft.Xna.Framework.Input;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria.GameInput;
using Terraria.UI;

namespace Terramon.Content.GUI.Common;

internal class UITextField : UIElement
{
    // Composition string is handled at the very beginning of the update
    // In order to check if there is a composition string before backspace is typed, we need to check the previous state
    private bool _oldHasCompositionString;
    private bool _reset;
    private bool _updated;
    private readonly int _maxLength;

    public UITextField(int maxLength)
    {
        _maxLength = maxLength;
        Width.Set(130, 0);
        Height.Set(24, 0);
    }

    public string CurrentValue { get; private set; } = "";
    
    public bool IsTyping { get; private set; }

    public void SetCurrentText(string text)
    {
        CurrentValue = text;
        _updated = true;
    }

    public void SetTyping()
    {
        IsTyping = true;
        Main.blockInput = true;
    }

    public void SetNotTyping()
    {
        IsTyping = false;
        Main.blockInput = false;
    }

    /*public override void LeftClick(UIMouseEvent evt)
    {
        SetTyping();

        base.LeftClick(evt);
    }

    public override void RightClick(UIMouseEvent evt)
    {
        SetTyping();
        CurrentValue = "";
        _updated = true;

        base.RightClick(evt);
    }*/

    public override void Update(GameTime gameTime)
    {
        if (_reset)
        {
            _updated = false;
            _reset = false;
        }

        if (_updated)
            _reset = true;

        /*if (Main.mouseLeft && !IsMouseHovering)
            SetNotTyping();*/

        base.Update(gameTime);
    }

    private void HandleText()
    {
        if (Main.keyState.IsKeyDown(Keys.Escape) || Main.inFancyUI)
            SetNotTyping();

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();

        var newText = Main.GetInputText(CurrentValue);

        // GetInputText() handles typing operation, but there is a issue that it doesn't handle backspace correctly when the composition string is not empty. It will delete a character both in the text and the composition string instead of only the one in composition string. We'll fix the issue here to provide a better user experience
        if (_oldHasCompositionString && Main.inputText.IsKeyDown(Keys.Back))
            newText = CurrentValue; // force text not to be changed

        if (newText != CurrentValue)
        {
            // Cap the text length
            if (newText.Length > _maxLength)
                newText = newText[.._maxLength];
            
            CurrentValue = newText;
            _updated = true;
        }

        _oldHasCompositionString = Platform.Get<IImeService>().CompositionString is { Length: > 0 };
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        //GUIHelper.DrawBox(spriteBatch, GetDimensions().ToRectangle(), ThemeHandler.ButtonColor);

        if (IsTyping)
        {
            //GUIHelper.DrawOutline(spriteBatch, GetDimensions().ToRectangle(), ThemeHandler.ButtonColor.InvertColor());
            HandleText();

            // draw ime panel, note that if there's no composition string then it won't draw anything
            //Main.instance.DrawWindowsIMEPanel(GetDimensions().Position());
        }

        /*var pos = GetDimensions().Position() + Vector2.One * 4;

        const float scale = 0.75f;
        var displayed = CurrentValue ?? "";

        Utils.DrawBorderString(spriteBatch, displayed, pos, Color.White, scale);

        // composition string + cursor drawing below
        if (!_typing)
            return;

        pos.X += FontAssets.MouseText.Value.MeasureString(displayed).X * scale;
        var compositionString = Platform.Get<IImeService>().CompositionString;

        if (compositionString is { Length: > 0 })
        {
            Utils.DrawBorderString(spriteBatch, compositionString, pos, new Color(255, 240, 20), scale);
            pos.X += FontAssets.MouseText.Value.MeasureString(compositionString).X * scale;
        }

        if (Main.GameUpdateCount % 20 < 10)
            Utils.DrawBorderString(spriteBatch, "|", pos, Color.White, scale);*/
    }
}
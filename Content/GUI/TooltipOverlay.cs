using System;
using System.Collections.Generic;
using ReLogic.Content;
using Terramon.Core.Loaders.UILoading;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI;

public class TooltipOverlay : SmartUIState, ILoadable
{
    private static string _text = string.Empty;
    private static string _tooltip = string.Empty;
    private static Asset<Texture2D> _icon;

    public override bool Visible => true;

    public void Load(Mod mod)
    {
        On_Main.DrawInterface += static (orig, self, gameTime) =>
        {
            orig(self, gameTime);
            Reset();
        };

        On_Main.DrawMap += static (orig, self, gameTime) =>
        {
            orig(self, gameTime);
            Reset();
        };
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.Count - 1;
    }

    public static void SetName(string newName)
    {
        _text = newName;
    }

    public static void SetTooltip(string newTooltip)
    {
        _tooltip = newTooltip;
    }

    public static void SetIcon(Asset<Texture2D> newIcon)
    {
        _icon = newIcon;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_text == string.Empty)
            return;

        var font = FontAssets.MouseText.Value;

        var nameWidth = ChatManager.GetStringSize(font, _text, Vector2.One).X;
        if (_icon != null)
            nameWidth += 22;
        var tipWidth = ChatManager.GetStringSize(font, _tooltip, Vector2.One).X * 0.9f;

        var width = Math.Max(nameWidth, tipWidth);
        float height = -4;
        Vector2 pos;

        if (Main.MouseScreen.X > Main.screenWidth - width)
            pos = Main.MouseScreen - new Vector2(width + 33, 33);
        else
            pos = Main.MouseScreen + new Vector2(33, 33);

        height += ChatManager.GetStringSize(font, "{Dummy}", Vector2.One).Y;
        height += ChatManager.GetStringSize(font, _tooltip, Vector2.One * 0.9f).Y;

        if (pos.Y + height > Main.screenHeight)
            pos.Y -= height;

        Utils.DrawInvBG(Main.spriteBatch,
            new Rectangle((int)pos.X - 13, (int)pos.Y - 10, (int)width + 26, (int)height + 20),
            new Color(36, 33, 96) * 0.925f);

        if (_icon != null)
        {
            spriteBatch.Draw(_icon.Value, pos + new Vector2(-1, 3),
                null, Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            pos.X += 22;
        }

        Utils.DrawBorderString(Main.spriteBatch, _text, pos, Color.White);
        pos.Y += ChatManager.GetStringSize(font, _text, Vector2.One).Y + 4;
        if (_icon != null)
            pos.X -= 22;

        Utils.DrawBorderString(Main.spriteBatch, _tooltip, pos, new Color(218, 218, 242), 0.9f);
    }

    private static void Reset()
    {
        _text = string.Empty;
        _tooltip = string.Empty;
        _icon = null;
    }
}
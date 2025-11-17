using System.Reflection;
using ReLogic.Graphics;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;

namespace Terramon.Content.GUI;

internal static class MenuSocialWidget
{
    private const string DiscordURL = "https://discord.gg/qDn5eW27c4";
    private const string DiscordInviteCode = "qDn5eW27c4";
    private const string WikiURL = "https://terrariamods.wiki.gg/wiki/Terramon_Mod";
    private const string YouTubeURL = "https://www.youtube.com/@TerramonMod";
    private const string GitHubURL = "https://github.com/JamzOJamz/Terramon";
    private const double DiscordClientCheckInterval = 2.5;

    private static readonly Item FakeItem = new();
    private static readonly bool[] LastHoveringInteractableText = new bool[Enum.GetValues<ButtonType>().Length];
    private static DateTime _lastDiscordClientCheck = DateTime.MinValue;
    private static bool _isDiscordClientRunning;

    public static void Setup()
    {
        On_Main.DrawVersionNumber += MainDrawVersionNumber_Detour;
    }

    private static void MainDrawVersionNumber_Detour(On_Main.orig_DrawVersionNumber orig, Color menucolor, float upbump)
    {
        orig(menucolor, upbump);

        var mod = Terramon.Instance;
        if (mod == null) return;

        // Check if Discord client is open
        if (DateTime.UtcNow - _lastDiscordClientCheck > TimeSpan.FromSeconds(DiscordClientCheckInterval))
        {
            _lastDiscordClientCheck = DateTime.UtcNow;
            _isDiscordClientRunning = DiscordInviteBeamer.IsClientRunning();
        }

        // Draw mod version
        var drawPos = new Vector2(15, 15);
        if (Main.showFrameRate)
            drawPos.Y += 22;
        if (ModLoader.HasMod("TerrariaOverhaul"))
            drawPos.Y = Main.screenHeight / 2f - 74;

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value,
            $"{mod.DisplayNameClean} v{mod.Version}", drawPos, Color.White, 0f, Vector2.Zero,
            1.07f, SpriteEffects.None, 0f, alphaMult: 0.76f);

        drawPos.Y += 30;

        // Draw buttons
        if (!WorldGen.generatingWorld)
        {
            DrawButton(ref drawPos, "Mod Config", ButtonType.ModConfig, OnModConfigClick);
        }

        DrawButton(ref drawPos, "Discord Server", ButtonType.Discord, OnDiscordClick);
        DrawButton(ref drawPos, "Terramon Wiki", ButtonType.Wiki, () => Utils.OpenToURL(WikiURL));
        DrawButton(ref drawPos, "YouTube", ButtonType.YouTube, () => Utils.OpenToURL(YouTubeURL));
        DrawButton(ref drawPos, "GitHub", ButtonType.GitHub, () => Utils.OpenToURL(GitHubURL));
    }

    private static void DrawButton(ref Vector2 drawPos, string text, ButtonType buttonType, Action onClick)
    {
        var textSize = FontAssets.MouseText.Value.MeasureString(text);
        textSize.Y *= 0.9f;

        var hovered = Main.MouseScreen.Between(drawPos, drawPos + textSize);
        var buttonIndex = (int)buttonType;

        if (hovered)
        {
            Main.LocalPlayer.mouseInterface = true;

            if (!LastHoveringInteractableText[buttonIndex])
                SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mouseLeftRelease = false;
                onClick?.Invoke();
            }

            LastHoveringInteractableText[buttonIndex] = true;
        }
        else
        {
            LastHoveringInteractableText[buttonIndex] = false;
        }

        var textColor = hovered ? new Color(237, 246, 255) : new Color(173, 173, 198);
        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, text, drawPos,
            textColor, 0f, Vector2.Zero, 1.02f, SpriteEffects.None, 0f, alphaMult: 0.76f);

        drawPos.Y += 30;
    }

    private static void OnModConfigClick()
    {
        var interfaceType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
        var modConfigList = interfaceType!
            .GetField("modConfigList", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        var modToSelectOnOpen = modConfigList!
            .GetType().GetField("ModToSelectOnOpen", BindingFlags.Instance | BindingFlags.Public);
        modToSelectOnOpen!.SetValue(modConfigList, Terramon.Instance);
        Main.menuMode = (int)interfaceType
            .GetField("modConfigListID", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
    }

    private static void OnDiscordClick()
    {
        if (_isDiscordClientRunning)
            Task.Run(() => DiscordInviteBeamer.Send(DiscordInviteCode));
        else
            Utils.OpenToURL(DiscordURL);
    }

    private static void DrawOutlinedStringOnMenu(SpriteBatch spriteBatch, DynamicSpriteFont font, string text,
        Vector2 position, Color drawColor, float rotation, Vector2 origin, float scale, SpriteEffects effects,
        float layerDepth, bool special = false, float alphaMult = 0.3f)
    {
        for (var i = 0; i < 5; i++)
        {
            var color = Color.Black;
            if (i == 4)
            {
                color = drawColor;
                if (special)
                {
                    color.R = (byte)((255 + color.R) / 2);
                    color.G = (byte)((255 + color.R) / 2);
                    color.B = (byte)((255 + color.R) / 2);
                }
            }

            color.A = (byte)(color.A * alphaMult);

            var offX = 0;
            var offY = 0;
            switch (i)
            {
                case 0:
                    offX = -2;
                    break;
                case 1:
                    offX = 2;
                    break;
                case 2:
                    offY = -2;
                    break;
                case 3:
                    offY = 2;
                    break;
            }

            spriteBatch.DrawString(font, text, position + new Vector2(offX, offY), color, rotation, origin, scale,
                effects, layerDepth);
        }
    }

    private enum ButtonType
    {
        ModConfig,
        Discord,
        Wiki,
        YouTube,
        GitHub
    }
}
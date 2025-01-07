using ReLogic.Graphics;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;

namespace Terramon.Content.GUI;

internal sealed class MenuSocialWidget
{
    private const string DiscordURL = "https://discord.gg/qDn5eW27c4"; // Terramon Mod, #rules-and-info
    private const string DiscordInviteCode = "qDn5eW27c4";
    private const string WikiURL = "https://terrariamods.wiki.gg/wiki/Terramon_Mod";
    private const string YouTubeURL = "https://www.youtube.com/@TerramonMod";
    private const string GitHubURL = "https://github.com/JamzOJamz/Terramon";

    private static readonly Item FakeItem = new();
    private static readonly bool[] LastHoveringInteractableText = new bool[4];
    private static DateTime _lastDiscordClientCheck = DateTime.MinValue;
    private const double DiscordClientCheckInterval = 5;
    private static bool _isDiscordClientRunning;

    public static void Setup()
    {
        On_Main.DrawVersionNumber += MainDrawVersionNumber_Detour;
    }

    private static void MainDrawVersionNumber_Detour(On_Main.orig_DrawVersionNumber orig, Color menucolor, float upbump)
    {
        orig(menucolor, upbump);

        // Wait until the mod is loaded by TML
        var mod = Terramon.Instance;
        if (mod == null) return;
        
        // Check if Discord client is open on the player's system every X seconds
        if (DateTime.UtcNow - _lastDiscordClientCheck > TimeSpan.FromSeconds(DiscordClientCheckInterval))
        {
            _lastDiscordClientCheck = DateTime.UtcNow;
            _isDiscordClientRunning = DiscordInviteBeamer.IsClientRunning();
        }

        var drawPos = new Vector2(15, 15);
        if (Main.showFrameRate)
            drawPos.Y += 22;
        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value,
            $"{mod.DisplayNameClean} v{mod.Version}", drawPos, Color.White, 0f, Vector2.Zero,
            1.07f, SpriteEffects.None, 0f, alphaMult: 0.76f);

        // Draw "Join the Discord community" text in blurple below the version number
        var discordText = "Discord Server";
        if (_isDiscordClientRunning)
            discordText += " \u2714"; // Checkmark
        var discordTextSize = FontAssets.MouseText.Value.MeasureString(discordText);
        discordTextSize.Y *= 0.9f;
        drawPos.Y += 30;
        var hovered = Main.MouseScreen.Between(drawPos, drawPos + discordTextSize);
        if (hovered)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!LastHoveringInteractableText[0])
                SoundEngine.PlaySound(SoundID.MenuTick);
            if (_isDiscordClientRunning)
            {
                FakeItem.SetDefaults(0, true);
                const string textValue = "[c/FFFFFF:Discord client detected!]\n[c/BABAC6:Click to go directly to the server!]";
                FakeItem.SetNameOverride(textValue);
                FakeItem.type = ItemID.IronPickaxe;
                FakeItem.scale = 0f;
                FakeItem.value = -1;
                Main.HoverItem = FakeItem;
                Main.instance.MouseText("");
                Main.mouseText = true;
            }

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mouseLeftRelease = false;
                
                if (_isDiscordClientRunning)
                {
                    Task.Run(() => DiscordInviteBeamer.Send(DiscordInviteCode));
                }
                else
                {
                    Utils.OpenToURL(DiscordURL);
                }
            }

            LastHoveringInteractableText[0] = true;
        }
        else
        {
            LastHoveringInteractableText[0] = false;
        }

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, discordText, drawPos,
            hovered ? new Color(237, 246, 255) : new Color(173, 173, 198), 0f, Vector2.Zero, 1.02f, SpriteEffects.None,
            0f, alphaMult: 0.76f);

        // Draw Terramon Wiki link text
        const string wikiText = "Terramon Wiki";
        var wikiTextSize = FontAssets.MouseText.Value.MeasureString(wikiText);
        wikiTextSize.Y *= 0.9f;
        drawPos.Y += 28;
        var hoveredWiki = Main.MouseScreen.Between(drawPos, drawPos + wikiTextSize);
        if (hoveredWiki)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!LastHoveringInteractableText[1])
                SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mouseLeftRelease = false;

                Utils.OpenToURL(WikiURL);
            }

            LastHoveringInteractableText[1] = true;
        }
        else
        {
            LastHoveringInteractableText[1] = false;
        }

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, wikiText, drawPos,
            hoveredWiki ? new Color(237, 246, 255) : new Color(173, 173, 198), 0f, Vector2.Zero, 1.02f,
            SpriteEffects.None,
            0f, alphaMult: 0.76f);

        // Draw YouTube link text
        const string youtubeText = "YouTube";
        var youtubeTextSize = FontAssets.MouseText.Value.MeasureString(youtubeText);
        youtubeTextSize.Y *= 0.9f;
        drawPos.Y += 28;
        var hoveredYoutube = Main.MouseScreen.Between(drawPos, drawPos + youtubeTextSize);
        if (hoveredYoutube)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!LastHoveringInteractableText[2])
                SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mouseLeftRelease = false;

                Utils.OpenToURL(YouTubeURL);
            }

            LastHoveringInteractableText[2] = true;
        }
        else
        {
            LastHoveringInteractableText[2] = false;
        }

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, youtubeText, drawPos,
            hoveredYoutube ? new Color(237, 246, 255) : new Color(173, 173, 198), 0f, Vector2.Zero, 1.02f,
            SpriteEffects.None,
            0f, alphaMult: 0.76f);

        // Draw GitHub link text
        const string githubText = "GitHub";
        var githubTextSize = FontAssets.MouseText.Value.MeasureString(githubText);
        githubTextSize.Y *= 0.9f;
        drawPos.Y += 28;
        var hoveredGithub = Main.MouseScreen.Between(drawPos, drawPos + githubTextSize);
        if (hoveredGithub)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!LastHoveringInteractableText[3])
                SoundEngine.PlaySound(SoundID.MenuTick);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mouseLeftRelease = false;

                Utils.OpenToURL(GitHubURL);
            }

            LastHoveringInteractableText[3] = true;
        }
        else
        {
            LastHoveringInteractableText[3] = false;
        }

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, githubText, drawPos,
            hoveredGithub ? new Color(237, 246, 255) : new Color(173, 173, 198), 0f, Vector2.Zero, 1.02f,
            SpriteEffects.None,
            0f, alphaMult: 0.76f);
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
}
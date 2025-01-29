using Terramon.Content.Items.PokeBalls;

namespace Terramon.Core.Systems;

/// <summary>
///     Displays a welcome message to the player when they enter the world.
/// </summary>
public class MOTDSystem : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Terramon.TimesLoaded != 1) return;

        var mod = Terramon.Instance;
        Main.NewText(
            $"Thank you for installing {mod.DisplayNameClean} v{mod.Version}! [i:{ModContent.ItemType<PokeBallItem>()}]\n\n" +
            "[c/C9C9E5:Make sure to customize the mod to your liking in the Mod Config menu and set up keybinds in the Controls menu.]");
    }
}
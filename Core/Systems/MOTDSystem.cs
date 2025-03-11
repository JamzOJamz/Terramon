using Terramon.Content.Items.PokeBalls;
using Terraria.Localization;

namespace Terramon.Core.Systems;

/// <summary>
///     Displays a welcome message to the player when they enter the world.
/// </summary>
public class MOTDSystem : ModPlayer
{
    private static readonly LocalizedText WelcomeMessage = Language.GetText("Mods.Terramon.Misc.MOTD");

    public override void OnEnterWorld()
    {
        if (Terramon.TimesLoaded != 1) return;

        var mod = Terramon.Instance;
        Main.NewText(WelcomeMessage.WithFormatArgs(mod.DisplayNameClean, mod.Version,
            ModContent.ItemType<PokeBallItem>()));
    }
}
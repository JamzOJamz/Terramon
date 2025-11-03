using Terramon.Content.Items.PokeBalls;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core.Systems;

/// <summary>
///     Displays a welcome message to the player when they enter the world for the first time or when the mod version has
///     been updated.
/// </summary>
public class MOTDPlayer : ModPlayer
{
    private const string SeenMotdKey = "seenMotd";
    private const int MessageDelayMs = 15000;

    private static readonly LocalizedText WelcomeMessage = Language.GetText("Mods.Terramon.Misc.MOTD");
    private static CancellationTokenSource _cancellationTokenSource = new();
    private Version _seenMotdVersion = new(0, 0, 0);

    public override void OnEnterWorld()
    {
        if (ShouldShowMotd())
            ShowMotdAfterDelay();
    }

    public static void CancelPendingMotd()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private bool ShouldShowMotd()
    {
        return Mod.Version > _seenMotdVersion;
    }

    private void ShowMotdAfterDelay()
    {
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(MessageDelayMs, _cancellationTokenSource.Token);
                Main.QueueMainThreadAction(DisplayWelcomeMessage);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private void DisplayWelcomeMessage()
    {
        if (Main.gameMenu) return;

        var formattedMessage = WelcomeMessage.WithFormatArgs(
            Mod.DisplayNameClean,
            Mod.Version,
            ModContent.ItemType<PokeBallItem>()
        );

        Main.NewText(formattedMessage);

        _seenMotdVersion = Mod.Version;
    }

    public override void SaveData(TagCompound tag)
    {
        tag[SeenMotdKey] = _seenMotdVersion.ToString();
    }

    public override void LoadData(TagCompound tag)
    {
        var versionString = tag.GetString(SeenMotdKey) ?? "0.0.0";
        _seenMotdVersion = Version.TryParse(versionString, out var version)
            ? version
            : new Version(0, 0, 0);
    }
}

internal class MOTDSystem : ModSystem
{
    public override void PreSaveAndQuit()
    {
        MOTDPlayer.CancelPendingMotd();
    }
}
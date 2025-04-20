using System.Security.Cryptography;
using System.Text;
using ReLogic.OS;
using SevenZipExtractor;
using Terramon.Helpers;
using Terraria.Localization;
using Terraria.ModLoader.UI;

namespace Terramon.Core.Battling.TurnBased;

/// <summary>
///     Manages the installation of the Pokémon Showdown battle simulator.
/// </summary>
public static class ShowdownInstaller
{
    private const string Version = "0.1";

    private static readonly Dictionary<PlatformType, PlatformConfig> PlatformConfigs = new()
    {
        {
            PlatformType.Windows,
            new PlatformConfig(
                "Showdown.exe",
                "win-x64",
                "7319a81510686c04bf42a223222d4925efbe8581"
            )
        }
    };

    private static readonly string[] DownloadHosts =
    [
        $"https://github.com/JamzOJamz/pokemon-showdown/releases/download/{Version}/"
    ];

    private static PlatformType CurrentPlatform => Platform.Current.Type;

    private static PlatformConfig CurrentConfig => PlatformConfigs.TryGetValue(CurrentPlatform, out var config)
        ? config
        : throw new NotSupportedException($"Platform {CurrentPlatform} is not supported.");

    private static string ArchiveName => $"pokemon-showdown-v{Version}-{CurrentConfig.ArchiveSuffix}.zip";
    private static string ExecutablePath => Path.Combine(Terramon.SavePath, CurrentConfig.ExecutableName);

    public static bool IsVerified { get; private set; }

    public static void VerifyInstallation()
    {
        if (!IsCurrentPlatformSupported())
        {
            Terramon.Instance.Logger.Info("Pokémon Showdown is not supported on this platform");
            return;
        }

        var needsRedownload = !File.Exists(ExecutablePath) || !VerifyExecutableHash();
        if (!needsRedownload)
        {
            Terramon.Instance.Logger.Info("Pokémon Showdown installation verified");
            IsVerified = true;
            return;
        }

        Terramon.Instance.Logger.Info(
            "Pokémon Showdown is not installed or the executable hash does not match. Downloading...");

        if (TryDownload())
        {
            if (VerifyExecutableHash())
            {
                IsVerified = true;
                return;
            }

            TryDeleteExecutable(); // Only delete if a download occurred, but the hash was bad
        }

        ModLoader.OnSuccessfulLoad += () =>
            Interface.infoMessage.Show(
                Language.GetTextValue("Mods.Terramon.Loading.NoBattleSimulatorInfoMessage"),
                0);
    }

    private static void TryDeleteExecutable()
    {
        try
        {
            if (File.Exists(ExecutablePath))
                File.Delete(ExecutablePath);
        }
        catch (Exception ex)
        {
            Terramon.Instance.Logger.Error(
                $"Failed to delete corrupted Pokémon Showdown executable: {ex.Message}");
        }
    }

    private static bool TryDownload()
    {
        using var client = new HttpClient();
        foreach (var host in DownloadHosts)
        {
            var url = GetDownloadUrl(host);
            using var memoryStream = new MemoryStream();
            try
            {
                client.DownloadAsync(url, memoryStream, new Progress<float>(progress =>
                {
                    Interface.loadMods.SubProgressText = Language.GetTextValue(
                        "Mods.Terramon.Loading.DownloadingBattleSimulator",
                        MathF.Round(progress * 100f, 1));
                })).Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException httpEx)
            {
                Terramon.Instance.Logger.Error(
                    $"Failed to download Pokémon Showdown from {url}! Error: {httpEx.Message}");
                continue;
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            var extracted = false;
            using (var archiveFile = new ArchiveFile(memoryStream, SevenZipFormat.Zip))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    if (entry.FileName != CurrentConfig.ExecutableName)
                        continue;

                    using var fileStream = File.Create(ExecutablePath);
                    entry.Extract(fileStream);
                    extracted = true;
                    break;
                }
            }

            if (!extracted)
            {
                Terramon.Instance.Logger.Error(
                    $"Failed to extract {CurrentConfig.ExecutableName} from archive {ArchiveName}.");
                continue;
            }

            if (File.Exists(ExecutablePath))
            {
                Terramon.Instance.Logger.Info("Pokémon Showdown downloaded and extracted successfully!");
                Interface.loadMods.SubProgressText = string.Empty;
                return true;
            }

            Terramon.Instance.Logger.Error("Pokémon Showdown was not found after extraction!");
        }

        // All hosts failed
        return false;
    }

    private static string GetDownloadUrl(string host)
    {
        return $"{host}{ArchiveName}";
    }

    private static bool VerifyExecutableHash()
    {
        using var fs = new FileStream(ExecutablePath, FileMode.Open);
        using var bs = new BufferedStream(fs);
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(bs);
        var formatted = new StringBuilder(2 * hash.Length);
        foreach (var b in hash) formatted.Append($"{b:X2}");
        var hashString = formatted.ToString();
        return hashString.Equals(CurrentConfig.ExecutableHash, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentPlatformSupported()
    {
        return PlatformConfigs.ContainsKey(CurrentPlatform);
    }

    /// <summary>
    ///     Holds platform-specific config values for Showdown installations.
    /// </summary>
    private class PlatformConfig(string executableName, string archiveSuffix, string executableHash)
    {
        public string ExecutableName { get; } = executableName;
        public string ArchiveSuffix { get; } = archiveSuffix;
        public string ExecutableHash { get; } = executableHash;
    }
}
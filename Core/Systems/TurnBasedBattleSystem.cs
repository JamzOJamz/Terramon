using ReLogic.OS;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using Showdown.NET;
using Showdown.NET.Simulator;

namespace Terramon.Core.Systems;

public class TurnBasedBattleSystem : ModSystem
{
    private static readonly string RuntimesPath = Path.Combine(Terramon.SavePath, "Runtimes");

    private static readonly Dictionary<PlatformType, (string Name, string Extension)> Runtimes = new()
    {
        [PlatformType.Windows] = ("ClearScriptV8.win-x64", ".dll"),
        [PlatformType.Linux] = ("ClearScriptV8.linux-x64", ".so"),
        [PlatformType.OSX] = ("ClearScriptV8.osx-arm64", ".dylib")
    };

    private MemoryStream _showdownArchiveStream;

    public override void OnModLoad()
    {
        Terramon.Instance.Logger.Info("Loading turn-based battle system");
        
        // Ensure runtimes path exists
        Directory.CreateDirectory(RuntimesPath);
        
        var (runtimeName, extension) = Runtimes[Platform.Current.Type];
        var runtimeLibraryPath = Path.Combine(RuntimesPath, $"{runtimeName}{extension}");
        
        // Extract runtime to disk
        if (!File.Exists(runtimeLibraryPath))
        {
            Terramon.Instance.Logger.Info($"Extracting {runtimeName} runtime...");

            using var runtimeArchiveStream = Mod.GetFileStream($"lib/{runtimeName}.zip");
            using var zipArchive = ZipArchive.Open(runtimeArchiveStream);
            zipArchive.ExtractToDirectory(RuntimesPath);
        }

        // Load embedded Pokémon Showdown archive into memory
        using (var originalStream = Mod.GetFileStream("lib/pokemon-showdown.zip"))
        {
            _showdownArchiveStream = new MemoryStream();
            originalStream.CopyTo(_showdownArchiveStream);
            _showdownArchiveStream.Position = 0;
        }
        
        // ClearScript (what Showdown.NET uses to run PS) does a lot of trial and error
        Logging.IgnoreExceptionContents("Microsoft.ClearScript");

        // Initialize Showdown.NET runtime
        ShowdownHost.InitFromArchive(_showdownArchiveStream, RuntimesPath);
        
        var stream = new BattleStream();

        Task.Run(async () =>
        {
            await foreach (var output in stream.ReadOutputsAsync()) Mod.Logger.Info(output);
        });

        stream.Write(">start {\"formatid\":\"gen7randombattle\"}");
        //stream.Write(">player p1 {\"name\":\"Alice\"}");
        //stream.Write(">player p2 {\"name\":\"Bob\"}");*/
    }

    public override void Unload()
    {
        ShowdownHost.Unload();
        _showdownArchiveStream?.Dispose();
    }
}
using ReLogic.OS;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using Showdown.NET;
using Showdown.NET.Simulator;

namespace Terramon.Core.Systems;

public class TurnBasedBattleSystem : ModSystem
{
    private static readonly string RuntimesPath = Path.Combine(Terramon.SavePath, "Runtimes");

    private static readonly Dictionary<PlatformType, string> Runtimes = new()
    {
        [PlatformType.Windows] = "ClearScriptV8.win-x64",
        [PlatformType.Linux] = "ClearScriptV8.linux-x64",
        [PlatformType.OSX] = "ClearScriptV8.osx-arm64"
    };

    private MemoryStream _showdownArchiveStream;

    public override void OnModLoad()
    {
        Terramon.Instance.Logger.Info("Loading turn-based battle system");
        
        // Ensure runtimes path exists
        Directory.CreateDirectory(RuntimesPath);
        
        var runtime = Runtimes[Platform.Current.Type];
        var runtimeDllPath = Path.Combine(RuntimesPath, $"{runtime}.dll");
        
        // Extract runtime to disk
        if (!File.Exists(runtimeDllPath))
        {
            Terramon.Instance.Logger.Info($"Extracting {runtime} runtime...");

            using var runtimeArchiveStream = Mod.GetFileStream($"lib/{runtime}.zip");
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
using System.Diagnostics;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using ReLogic.OS;
using Terramon.Core.IO;
using Terramon.Core.Serialization;
using Terramon.Helpers;

namespace Terramon.Core.Battling.TurnBased;

public sealed class NodeShowdownService : IShowdownService
{
    private static readonly EmbeddedArchive NodeArchive = new("lib/node-win-x64.zip",
        "4223da95bf56d85a7ec3352f064ecd1c31c2c8e1af66a4b28e375771b2cdc320");

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new LowerCaseContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    private static bool _isSetupComplete;
    private readonly List<string> _currentRecvPayload = [];

    private string _currentRecvType;
    private bool _debug;
    private ILog _logger;
    private Process _process;

    static NodeShowdownService()
    {
        // Suppresses exception messages caused by the disposing of the process from logging to the chat
        MonoModHooks.Add(
            typeof(Logging).GetMethod("AddChatMessage", BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(string), typeof(Color)]), (Action<string, Color> orig, string msg, Color color) =>
            {
                if (msg.StartsWith("The operation was canceled.")) return;
                orig(msg, color);
            });
    }

    private static PlatformType CurrentPlatform => Platform.Current.Type;

    public void Dispose()
    {
        if (_process == null || _process.HasExited) return;
        _process.CancelOutputRead();
        _process.Kill();
        _process.Dispose();
        _process = null;
    }

    public void WriteCommand(string type, object[] data)
    {
        if (_process == null || _process.HasExited)
            throw new InvalidOperationException("Showdown process is not running");

        switch (type)
        {
            case "start":
                if (data.Length != 1 || data[0] is not BattleOptions options)
                    throw new ArgumentException("Invalid arguments for start command");
                var optionsJson = JsonConvert.SerializeObject(options, JsonSettings);
                WriteToProcessStdin($">start {optionsJson}");
                break;
            default:
                throw new ArgumentException($"Unknown command type: {type}");
        }
    }

    public event Action<string, string[]> ReceiveMessage;

    private void WriteToProcessStdin(string command)
    {
        if (_debug)
            _logger.Debug($"Send: {command}");

        _process.StandardInput.WriteLine(command);
        _process.StandardInput.Flush();
    }

    public static async Task<NodeShowdownService> StartNewAsync(bool debug = false)
    {
        if (!_isSetupComplete)
            throw new InvalidOperationException("NodeShowdownService not set up. Call SetupEnvironment() first.");

        var instance = new NodeShowdownService();
        instance._debug = debug;

        await Task.Run(instance.SpawnProcess);

        return instance;
    }

    private void SpawnProcess()
    {
        if (!NodeArchive.IsExtractedAndValid() || !Terramon.ShowdownArchive.IsExtractedAndValid())
            throw new InvalidOperationException("One or more required files are missing or invalid.");

        var nodeExecutablePath = Path.Combine(NodeArchive.OutputDirectory, "node.exe");
        var launchScriptPath = Path.Combine(Terramon.ShowdownArchive.OutputDirectory, "launch-simulator");

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = nodeExecutablePath,
            Arguments = $"\"{launchScriptPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });

        if (_process == null)
            throw new InvalidOperationException("Could not start Showdown process");

        // Setup logger with the process ID
        if (_debug)
            _logger = LogManager.GetLogger($"{nameof(Terramon)}::{nameof(NodeShowdownService)}/{_process.Id}");

        // Redirect stdout to read the output
        _process.OutputDataReceived += ReadFromProcessStdout;
        _process.BeginOutputReadLine();

        ChildProcessTracker.AddProcess(_process); // Ensures the process is killed when the game exits
    }

    private void ReadFromProcessStdout(object sender, DataReceivedEventArgs e)
    {
        if (_debug)
            _logger.Debug($"Recv: {e.Data}");

        switch (e.Data)
        {
            // If no data, return early
            case null:
                return;
            // If it's an empty string, it means the message is complete
            case "":
            {
                if (_currentRecvType != null)
                    ReceiveMessage?.Invoke(_currentRecvType, _currentRecvPayload.ToArray());

                // Reset buffer for next message
                _currentRecvType = null;
                _currentRecvPayload.Clear();
                return;
            }
        }

        // If type hasn't been set, this is the type
        if (_currentRecvType == null)
            _currentRecvType = e.Data;
        else
            _currentRecvPayload.Add(e.Data);
    }

    public static void SetupEnvironment()
    {
        if (_isSetupComplete) return;

        // Check if the current platform is supported
        if (!IsCurrentPlatformSupported())
        {
            Terramon.Instance.Logger.Info("NodeShowdownService not supported on this platform, skipping setup");
            return;
        }

        // Extract the Node.js executable and Pokémon Showdown to the cache directory
        if (!NodeArchive.EnsureExtracted() || !Terramon.ShowdownArchive.EnsureExtracted())
            return;

        _isSetupComplete = true;
    }

    private static bool IsCurrentPlatformSupported()
    {
        return Platform.IsWindows; // Not supported on OSX or Linux (yet)
    }
}
namespace Terramon;

/*
 * This entry point is a Visual Studio workaround used by the
 * "Terraria (ProjectBootstrap)" launch profile. It bootstraps tModLoader,
 * resolves its dependencies, and invokes MonoLaunch directly so
 * MetadataUpdateHandler callbacks work correctly for in-game changes.
 * Rider does not require this workaround.
 */
#if DEBUG
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using Steamworks;

file static class ClientBootstrapper
{
    public static void Initialize(
        string tmlSteamRoot,
        string mainAssemblyPath,
        string depsJsonPath = null)
    {
        DepsJsonResolver.Initialize(
            tmlSteamRoot,
            mainAssemblyPath,
            depsJsonPath);

        RegisterSteamworksResolver();
    }

    private static void RegisterSteamworksResolver()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(SteamAPI).Assembly,
            DepsJsonResolver.ResolveNativeLibrary);
    }
}

file static class DepsJsonResolver
{
    private static readonly Dictionary<string, string> AssemblyPaths =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, LibPriority> AssemblyPriority =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> NativeLibraryPaths =
        new(StringComparer.OrdinalIgnoreCase);

    private static string _librariesRoot;
    private static string _mainAssemblyPath;
    private static readonly string CurrentRid = GetCurrentRid();

    public static void Initialize(
        string tmlSteamRoot,
        string mainAssemblyPath,
        string depsJsonPath = null)
    {
        _librariesRoot = Path.Combine(tmlSteamRoot, "Libraries");
        _mainAssemblyPath = mainAssemblyPath;
        depsJsonPath ??= Path.Combine(tmlSteamRoot, "tModLoader.deps.json");

        using var stream = File.OpenRead(depsJsonPath);
        using var doc = JsonDocument.Parse(stream);

        var targets = doc.RootElement.GetProperty("targets");
        var tfm = targets.EnumerateObject().First();
        var libraries = doc.RootElement.GetProperty("libraries");

        foreach (var lib in tfm.Value.EnumerateObject())
        {
            var slash = lib.Name.LastIndexOf('/');
            if (slash < 0)
                continue;

            var libName = lib.Name[..slash];
            var libVersion = lib.Name[(slash + 1)..];

            if (libName.Equals("tModLoader", StringComparison.OrdinalIgnoreCase))
            {
                Assign("tModLoader", _mainAssemblyPath, LibPriority.Project);
                continue;
            }

            var libTypeStr = "project";
            string nugetPath = null;

            if (libraries.TryGetProperty(lib.Name, out var libInfo))
            {
                libTypeStr = libInfo.GetProperty("type").GetString();

                if (libInfo.TryGetProperty("path", out var pathProp))
                    nugetPath = pathProp.GetString();
            }

            var priority = libTypeStr switch
            {
                "package" => LibPriority.Package,
                "reference" => LibPriority.Reference,
                _ => LibPriority.Project
            };

            if (lib.Value.TryGetProperty("runtime", out var runtime))
                foreach (var entry in runtime.EnumerateObject())
                {
                    var asmName = Path.GetFileNameWithoutExtension(entry.Name);

                    var fullPath = libTypeStr == "package" && nugetPath != null
                        ? Path.Combine(
                            _librariesRoot,
                            nugetPath,
                            entry.Name.Replace('/', Path.DirectorySeparatorChar))
                        : Path.Combine(
                            _librariesRoot,
                            libName,
                            libVersion,
                            Path.GetFileName(entry.Name));

                    Assign(asmName, fullPath, priority);
                }

            if (lib.Value.TryGetProperty("runtimeTargets", out var runtimeTargets))
                foreach (var entry in runtimeTargets.EnumerateObject())
                {
                    var assetType = entry.Value.GetProperty("assetType").GetString();

                    if (assetType == "native")
                    {
                        RegisterNativeLibrary(
                            libTypeStr,
                            nugetPath,
                            libName,
                            libVersion,
                            entry);

                        continue;
                    }

                    if (assetType != "runtime")
                        continue;

                    var rid = entry.Value.GetProperty("rid").GetString();

                    if (!RidMatches(rid))
                        continue;

                    var asmName = Path.GetFileNameWithoutExtension(entry.Name);

                    var fullPath = libTypeStr == "package" && nugetPath != null
                        ? Path.Combine(
                            _librariesRoot,
                            nugetPath,
                            entry.Name.Replace('/', Path.DirectorySeparatorChar))
                        : Path.Combine(
                            _librariesRoot,
                            libName,
                            libVersion,
                            entry.Name.Replace('/', Path.DirectorySeparatorChar));

                    AssemblyPaths[asmName] = fullPath;
                    AssemblyPriority[asmName] = LibPriority.Project;
                }
        }

        AssemblyLoadContext.Default.Resolving += ResolveAssembly;
    }

    private static void RegisterNativeLibrary(
        string libType,
        string nugetPath,
        string libName,
        string libVersion,
        JsonProperty entry)
    {
        var rid = entry.Value.GetProperty("rid").GetString();

        if (!RidMatches(rid))
            return;

        var path = libType == "package" && nugetPath != null
            ? Path.Combine(
                _librariesRoot,
                nugetPath,
                entry.Name.Replace('/', Path.DirectorySeparatorChar))
            : Path.Combine(
                _librariesRoot,
                libName,
                libVersion,
                entry.Name.Replace('/', Path.DirectorySeparatorChar));

        var filename = Path.GetFileName(entry.Name);
        var libraryName = Path.GetFileNameWithoutExtension(filename);

        NativeLibraryPaths[libraryName] = path;

        // NativeLibraryResolver receives "steam_api", while the actual
        // platform library is "libsteam_api.dylib" on macOS.
        if (libraryName.StartsWith("lib", StringComparison.OrdinalIgnoreCase))
            NativeLibraryPaths[libraryName[3..]] = path;

        // steam_api64.dll on Windows is imported as "steam_api".
        if (libraryName.Equals("steam_api64", StringComparison.OrdinalIgnoreCase))
            NativeLibraryPaths["steam_api"] = path;
    }

    public static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (!NativeLibraryPaths.TryGetValue(libraryName, out var path))
            return IntPtr.Zero;

        if (!File.Exists(path))
        {
            Console.Error.WriteLine(
                $"[DepsJsonResolver] Native library '{libraryName}' " +
                $"expected at '{path}', but the file does not exist.");

            return IntPtr.Zero;
        }

        if (NativeLibrary.TryLoad(path, out var library))
            return library;

        Console.Error.WriteLine(
            $"[DepsJsonResolver] Failed to load native library '{libraryName}' " +
            $"from '{path}'.");

        return IntPtr.Zero;
    }

    private static bool RidMatches(string rid)
    {
        if (rid.Equals(CurrentRid, StringComparison.OrdinalIgnoreCase))
            return true;

        return rid.StartsWith(
            CurrentRid + "-",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCurrentRid()
    {
        if (OperatingSystem.IsMacOS())
            return "osx";

        if (OperatingSystem.IsLinux())
            return "linux";

        if (OperatingSystem.IsWindows())
            return Environment.Is64BitProcess
                ? "win-x64"
                : "win-x86";

        throw new PlatformNotSupportedException();
    }

    private static void Assign(
        string asmName,
        string path,
        LibPriority priority)
    {
        if (AssemblyPriority.TryGetValue(asmName, out var existing) &&
            existing >= priority)
            return;

        AssemblyPaths[asmName] = path;
        AssemblyPriority[asmName] = priority;
    }

    private static Assembly ResolveAssembly(
        AssemblyLoadContext ctx,
        AssemblyName name)
    {
        if (AssemblyPaths.TryGetValue(name.Name!, out var path) &&
            File.Exists(path))
            return ctx.LoadFromAssemblyPath(path);

        Console.Error.WriteLine(
            $"[DepsJsonResolver] Could not resolve '{name.Name}'" +
            (path != null
                ? $" (expected at '{path}', file missing)"
                : " (no mapping found)"));

        return null;
    }

    private enum LibPriority
    {
        Reference = 0,
        Package = 1,
        Project = 2
    }
}

file static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2 ||
            string.IsNullOrEmpty(args[0]) ||
            string.IsNullOrEmpty(args[1]))
            throw new ArgumentException(
                "Expected args: <tMLSteamPath> <tMLPath>");

        var tmlSteamPath = args[0];
        var tmlPath = args[1];

        var tmlMainAssemblyPath = Path.IsPathRooted(tmlPath)
            ? tmlPath
            : Path.Combine(tmlSteamPath, tmlPath);

        ClientBootstrapper.Initialize(
            tmlSteamPath,
            tmlMainAssemblyPath);

        Environment.CurrentDirectory = tmlSteamPath;

        DoLaunch(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoLaunch(string[] args)
    {
        args = [.. args, "-console"];

        typeof(MonoLaunch)
            .GetMethod(
                "Main",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static)!
            .CreateDelegate<Action<string[]>>()
            .Invoke(args);
    }
}
#else
file static class Program
{
    public static int Main()
    {
        Console.Error.WriteLine("This entry point is only available in DEBUG builds.");
        return 1;
    }
}
#endif

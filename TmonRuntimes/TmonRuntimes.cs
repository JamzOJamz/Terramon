using Terraria.ModLoader;

namespace TmonRuntimes;

/// <summary>
///     Data-only dependency mod that includes the ClearScript managed assemblies and native V8
///     runtimes used by Terramon's turn-based battle engine.
/// </summary>
/// <remarks>
///     Keeping the ClearScript assemblies and platform-specific runtimes here keeps Terramon's
///     dependency files together and avoids bundling them with the main mod. The dependency
///     version is pinned to 7.5.0 because Showdown.NET uses Microsoft.ClearScript.Complete 7.5.0.
///     Terramon references this mod in its <c>build.txt</c>.
/// </remarks>
internal sealed class TmonRuntimes : Mod
{
}

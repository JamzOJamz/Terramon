using Terraria.ModLoader;

namespace TmonRuntimes;

/// <summary>
///     Data-only dependency mod that includes the native ClearScript V8 runtimes used by
///     Terramon's turn-based battle engine.
/// </summary>
/// <remarks>
///     Keeping these archives out of Terramon itself lets the platform-specific binaries be
///     updated (or reinstalled) independently of the main mod. Terramon strong-references this
///     mod in its <c>build.txt</c> and reads the archives from it.
/// </remarks>
internal sealed class TmonRuntimes : Mod
{
}

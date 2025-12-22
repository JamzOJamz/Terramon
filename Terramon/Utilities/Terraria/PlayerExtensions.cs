namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for Terraria's <see cref="Player" /> class.
/// </summary>
public static class PlayerExtensions
{
    /// <summary>
    ///     Gets the <see cref="TerramonPlayer" /> instance associated with this <see cref="Player" />.
    /// </summary>
    /// <param name="player">The player whose Terramon mod player data should be retrieved.</param>
    /// <returns>The <see cref="TerramonPlayer" /> tied to the given <see cref="Player" />.</returns>
    public static TerramonPlayer Terramon(this Player player) => player.GetModPlayer<TerramonPlayer>();

    /// <summary>
    ///     A wrapper for <see cref="Main.NewText(object, Color?)" /> that only sends the message if the player is the local
    ///     player.
    /// </summary>
    public static void NewText(this Player player, object o, Color? color = null)
    {
        if (player.whoAmI != Main.myPlayer) return;
        Main.NewText(o, color);
    }
}
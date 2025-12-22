using Terramon.Content.NPCs;

namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for Terraria's <see cref="NPC" /> class.
/// </summary>
public static class NPCExtensions
{
    /// <summary>
    ///     Gets the <see cref="PokemonNPC" /> instance associated with this <see cref="NPC" />.
    /// </summary>
    /// <param name="npc">The NPC whose <see cref="PokemonNPC" /> should be retrieved.</param>
    /// <returns>
    ///     The <see cref="PokemonNPC" /> instance backing the given <see cref="NPC" />.
    /// </returns>
    /// <exception cref="InvalidCastException">
    ///     Thrown if the NPC's <see cref="ModNPC" /> is not a <see cref="PokemonNPC" />.
    /// </exception>
    public static PokemonNPC Pokemon(this NPC npc) => (PokemonNPC)npc.ModNPC;
}
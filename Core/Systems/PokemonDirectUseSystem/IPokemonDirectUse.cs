namespace Terramon.Core.Systems.PokemonDirectUseSystem;

/// <summary>
///     Interface for items that can be used directly on a Pokémon.
/// </summary>
public interface IPokemonDirectUse
{
    /// <summary>
    ///     Determines if the item will have an effect on the given Pokémon. If this method returns false, the item will not be
    ///     usable on the Pokémon and <see cref="PokemonDirectUse" /> will not be called. By default returns true.
    /// </summary>
    /// <param name="data">The Pokémon to check.</param>
    bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return true;
    }

    /// <summary>
    ///     Called when the item is used directly on a compatible Pokémon. Any data changes should be done here.
    /// </summary>
    /// <param name="player">The player using the item.</param>
    /// <param name="data">The Pokémon the item is being used on.</param>
    /// <param name="amount">The amount of the item being used.</param>
    /// <returns>The amount of the item actually used.</returns>
    int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        return amount;
    }
}
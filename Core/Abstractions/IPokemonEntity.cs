namespace Terramon.Core.Abstractions;

/// <summary>
///     Represents a type of <see cref="Terraria.Entity" /> that represents a Pokémon in the database.
/// </summary>
public interface IPokemonEntity
{
    /// <summary>
    ///     The ID of the Pokémon (in the database) that this entity represents. Not to be confused with the entity type.
    /// </summary>
    ushort ID { get; }

    /// <summary>
    ///     The database schema of the Pokémon that this entity represents.
    /// </summary>
    DatabaseV2.PokemonSchema Schema { get; }

    /// <summary>
    ///     The Pokémon instance data tied to this entity. This should be initialized when the entity is spawned or otherwise
    ///     created in the world.
    /// </summary>
    PokemonData Data { get; set; }
}
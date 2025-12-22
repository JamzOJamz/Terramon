namespace Terramon.Utilities.Terraria;

/// <summary>
///     Utility methods for converting between <see cref="Gender" /> and Pokémon Showdown representations.
/// </summary>
public static class GenderUtils
{
    /// <summary>
    ///     Converts a <see cref="Gender" /> to its Showdown char representation ('M', 'F', 'N').
    /// </summary>
    public static char ToShowdownChar(this Gender gender) =>
        gender switch
        {
            Gender.Male => 'M',
            Gender.Female => 'F',
            Gender.Unspecified => 'N',
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };

    /// <summary>
    ///     Converts a Showdown char ('M', 'F', 'N') to its <see cref="Gender" /> representation.
    /// </summary>
    public static Gender FromShowdownChar(char? c) =>
        c switch
        {
            'M' => Gender.Male,
            'F' => Gender.Female,
            'N' or null => Gender.Unspecified,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };
}
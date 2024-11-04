using Terramon.Helpers;

namespace Terramon.ID;

public enum PokemonType
{
    Normal,
    Fire,
    Fighting,
    Water,
    Flying,
    Grass,
    Poison,
    Electric,
    Ground,
    Psychic,
    Rock,
    Ice,
    Bug,
    Dragon,
    Ghost,
    Dark,
    Steel,
    Fairy
}

public static class PokemonTypeExtensions
{
    /// <summary>
    ///     Returns the color of the type in hexadecimal format.
    ///     If the type is not recognized, returns white.
    ///     <para>Example: <c>TypeID.Fire.GetColor()</c> returns <c>"ed6657"</c></para>
    /// </summary>
    public static string GetHexColor(this PokemonType type)
    {
        return type switch
        {
            PokemonType.Normal => "cecfce",
            PokemonType.Fire => "ed6657",
            PokemonType.Fighting => "ffac59",
            PokemonType.Water => "74acf5",
            PokemonType.Flying => "add2f5",
            PokemonType.Grass => "82c274",
            PokemonType.Poison => "b884dd",
            PokemonType.Electric => "fcd659",
            PokemonType.Ground => "b88e6f",
            PokemonType.Psychic => "f584a8",
            PokemonType.Rock => "cbc7ad",
            PokemonType.Ice => "81dff7",
            PokemonType.Bug => "b8c26a",
            PokemonType.Dragon => "8d98ec",
            PokemonType.Ghost => "a284a2",
            PokemonType.Dark => "998b8c",
            PokemonType.Steel => "98c2d1",
            PokemonType.Fairy => "f5a2f5",
            _ => "ffffff"
        };
    }
    
    /// <summary>
    ///     Returns the color of the type as a <see cref="Color"/>.
    ///     If the type is not recognized, returns white.
    ///     <para>Example: <c>TypeID.Fire.GetColor()</c> returns <c>new Color(237, 102, 87)</c></para>
    /// </summary>
    public static Color GetColor(this PokemonType type)
    {
        return ColorUtils.FromHex(int.Parse(type.GetHexColor(), System.Globalization.NumberStyles.HexNumber));
    }
}
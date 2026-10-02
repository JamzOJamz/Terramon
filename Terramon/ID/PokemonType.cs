using System.Globalization;
using Terramon.Helpers;

namespace Terramon.ID;

public enum PokemonType : byte
{
    None,
    Normal,
    Fighting,
    Flying,
    Poison,
    Ground,
    Rock,
    Bug,
    Ghost,
    Steel,
    Fire,
    Water,
    Grass,
    Electric,
    Psychic,
    Ice,
    Dragon,
    Dark,
    Fairy,
    Stellar,
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
            PokemonType.Normal => "919aa2",
            PokemonType.Fighting => "ce416b",
            PokemonType.Flying => "8fa9de",
            PokemonType.Poison => "aa6bc8",
            PokemonType.Ground => "d97845",
            PokemonType.Rock => "c5b78c",
            PokemonType.Bug => "91c12f",
            PokemonType.Ghost => "91c12f",
            PokemonType.Steel => "5a8ea2",
            PokemonType.Fire => "ff9d55",
            PokemonType.Water => "5090d6",
            PokemonType.Grass => "63bc5a",
            PokemonType.Electric => "f4d23c",
            PokemonType.Psychic => "fa7179",
            PokemonType.Ice => "73cec0",
            PokemonType.Dragon => "0b6dc3",
            PokemonType.Dark => "5a5465",
            PokemonType.Fairy => "ec8fe6",
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
        return ColorUtils.FromHexRGB(uint.Parse(type.GetHexColor(), NumberStyles.AllowHexSpecifier));
    }
}
namespace Terramon.ID;

public enum TypeID : byte
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

public static class TypeIDExtensions
{
    /// <summary>
    ///     Returns the color of the type in hexadecimal format.
    ///     If the type is not recognized, returns white.
    ///     <para>Example: <c>TypeID.Fire.GetColor()</c> returns <c>"ed6657"</c></para>
    /// </summary>
    public static string GetColor(this TypeID type)
    {
        return type switch
        {
            TypeID.Normal => "cecfce",
            TypeID.Fire => "ed6657",
            TypeID.Fighting => "ffac59",
            TypeID.Water => "74acf5",
            TypeID.Flying => "add2f5",
            TypeID.Grass => "82c274",
            TypeID.Poison => "b884dd",
            TypeID.Electric => "fcd659",
            TypeID.Ground => "b88e6f",
            TypeID.Psychic => "f584a8",
            TypeID.Rock => "cbc7ad",
            TypeID.Ice => "81dff7",
            TypeID.Bug => "b8c26a",
            TypeID.Dragon => "8d98ec",
            TypeID.Ghost => "a284a2",
            TypeID.Dark => "998b8c",
            TypeID.Steel => "98c2d1",
            TypeID.Fairy => "f5a2f5",
            _ => "ffffff"
        };
    }
}
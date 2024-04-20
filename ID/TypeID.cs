// ReSharper disable MemberCanBePrivate.Global
namespace Terramon.ID;

public static class TypeID
{
    public const byte Normal = 0;
    public const byte Fire = 1;
    public const byte Fighting = 2;
    public const byte Water = 3;
    public const byte Flying = 4;
    public const byte Grass = 5;
    public const byte Poison = 6;
    public const byte Electric = 7;
    public const byte Ground = 8;
    public const byte Psychic = 9;
    public const byte Rock = 10;
    public const byte Ice = 11;
    public const byte Bug = 12;
    public const byte Dragon = 13;
    public const byte Ghost = 14;
    public const byte Dark = 15;
    public const byte Steel = 16;
    public const byte Fairy = 17;

    public static string GetColor(byte type)
    {
        return type switch
        {
            Normal => "9099a1",
            Fire => "f59847",
            Fighting => "ce4069",
            Water => "4aaeed",
            Flying => "8fa8dd",
            Grass => "70d669",
            Poison => "ab6ac8",
            Electric => "f3d23b",
            Ground => "d97746",
            Psychic => "f97176",
            Rock => "c7b78b",
            Ice => "74cec0",
            Bug => "90c12c",
            Dragon => "0a6dc4",
            Ghost => "5269ac",
            Dark => "5a5366",
            Steel => "5a8ea1",
            Fairy => "ec8fe6",
            _ => "ffffff"
        };
    }
}
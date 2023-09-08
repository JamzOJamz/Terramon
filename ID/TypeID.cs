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
            Grass => "70d669",
            Fire => "f59847",
            Water => "4aaeed",
            _ => "ffffff"
        };
    }
}
using ReLogic.Reflection;

namespace Terramon.ID;

public static class BallID
{
    public const byte MasterBall = 1;
    public const byte UltraBall = 2;
    public const byte GreatBall = 3;
    public const byte PokeBall = 4;
    public const byte PremierBall = 12;
    public static readonly IdDictionary Search = IdDictionary.Create(typeof(BallID), typeof(byte));
}
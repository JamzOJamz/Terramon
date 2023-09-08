using ReLogic.Reflection;

namespace Terramon.ID;

public static class BehaviourID
{
    public const byte Walking = 0;
    public const byte Bounce = 1;
    public const byte WanderingHover = 2;
    public static readonly IdDictionary Search = IdDictionary.Create(typeof(BehaviourID), typeof(byte));
}
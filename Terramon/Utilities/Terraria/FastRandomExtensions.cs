using Terraria.Utilities;

namespace Terramon.Utilities.Terraria;

internal static class FastRandomExtensions
{
    /// <summary>
    ///     Returns true or false with equal chance.
    /// </summary>
    public static bool NextBool(this ref FastRandom r)
    {
        return r.NextFloat() < .5;
    }

    /// <summary>
    ///     Generates a random value between <paramref name="minValue" /> (inclusive) and <paramref name="maxValue" />
    ///     (exclusive). <br />It will not return <paramref name="maxValue" />.
    /// </summary>
    public static float NextFloat(this ref FastRandom r, float minValue, float maxValue)
    {
        return r.NextFloat() * (maxValue - minValue) + minValue;
    }
}
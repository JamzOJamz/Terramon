using Terraria.Utilities;

namespace Terramon.Helpers;

public static class VanillaExtensions
{
    /// <summary>
    ///     A wrapper for <see cref="Main.NewText(object, Color?)" /> that only sends the message if the player is the local
    ///     player.
    /// </summary>
    public static void NewText(this Player player, object o, Color? color = null)
    {
        if (player.whoAmI != Main.myPlayer) return;
        Main.NewText(o, color);
    }

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

    /// <summary>
	/// Converts a <see cref="Gender" /> to its Showdown char representation ('M', 'F', 'N').
	/// </summary>
	public static char ToShowdownChar(this Gender gender) =>
        gender switch
        {
            Gender.Male => 'M',
            Gender.Female => 'F',
            Gender.Unspecified => 'N',
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };
}
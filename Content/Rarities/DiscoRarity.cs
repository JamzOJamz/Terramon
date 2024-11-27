namespace Terramon.Content.Rarities;

/// <summary>
///     A ModRarity that supports transitioning between different colors.
/// </summary>
public abstract class DiscoRarity : ModRarity
{
    /// <summary>
    ///     The colors to transition through.
    /// </summary>
    protected abstract Color[] Colors { get; }

    /// <summary>
    ///     The time it should take to complete one cycle of the animation, in seconds.
    /// </summary>
    protected virtual float Time => 1f;

    public override Color RarityColor => CalculateRarityColor();

    private Color CalculateRarityColor()
    {
        if (Colors.Length == 0)
            return Color.White;
        var progress = (float)Main.timeForVisualEffects / (Time * 60f);
        return Color.Lerp(Colors[(int)progress % Colors.Length], Colors[((int)progress + 1) % Colors.Length],
            progress % 1f);
    }
}
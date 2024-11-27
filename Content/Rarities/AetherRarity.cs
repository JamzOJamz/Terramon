namespace Terramon.Content.Rarities;

public class AetherRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(255, 84, 222), // Pink
        new Color(75, 123, 255), // Blue
        new Color(113, 60, 234) // Purple
    ];

    protected override float Time => 2f;
}
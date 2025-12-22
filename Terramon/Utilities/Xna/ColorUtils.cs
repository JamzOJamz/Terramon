namespace Terramon.Utilities.Xna;

/// <summary>
///     Provides utility methods for working with colors.
/// </summary>
public static class ColorUtils
{
    /// <summary>
    ///     Converts a hexadecimal color value in RGB format to a <see cref="Color" /> struct.
    /// </summary>
    /// <param name="hexValue">The hexadecimal color value to convert.</param>
    /// <returns>The <see cref="Color" /> struct representing the hexadecimal color value.</returns>
    /// <remarks>
    ///     The hexadecimal color value should be in the format 0xRRGGBB.
    ///     The alpha channel is set to 255 (fully opaque).
    /// </remarks>
    public static Color FromHexRGB(uint hexValue)
    {
        return new Color
        {
            PackedValue = (uint)((255 << 24) | ((byte)(hexValue & 0xFF) << 16) | ((byte)((hexValue >> 8) & 0xFF) << 8) |
                                 (byte)((hexValue >> 16) & 0xFF))
        };
    }

    /// <summary>
    ///     Converts an XNA Color to a hex string without the # symbol
    /// </summary>
    /// <param name="color">The XNA Color to convert</param>
    /// <returns>Hex string in format "RRGGBB" or "RRGGBBAA" if alpha is not 255</returns>
    public static string ToHexString(this Color color)
    {
        return color.A == 255
            ?
            // RGB format (no alpha)
            $"{color.R:X2}{color.G:X2}{color.B:X2}"
            :
            // RGBA format (include alpha)
            $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }

    /// <summary>
    ///     Shifts the hue of a color by a specified amount, optionally adjusting lightness and direction.
    /// </summary>
    /// <param name="color">The original color to modify.</param>
    /// <param name="amt">The amount to shift the hue (0-1 range).</param>
    /// <param name="shiftLumi">Optional adjustment to lightness (-1 to 1, default 0).</param>
    /// <param name="direction">
    ///     Optional hue shift direction: 
    ///     0 = automatic based on hue, 
    ///     1 = clockwise, 
    ///     -1 = counterclockwise.
    /// </param>
    /// <returns>A new <see cref="Color"/> with the hue shifted and lightness adjusted.</returns>
    /// <remarks>
    ///     Converts the color to HSL, shifts the hue with bidirectional wrapping, 
    ///     optionally modifies the lightness, and converts it back to RGB.
    /// </remarks>
    public static Color HueShift(this Color color, float amt, float shiftLumi = 0f, float direction = 0f)
    {
        // Thresholds for determining best hue shifting direction
        const float yellow = 0.23529411764f;
        const float blue = 0.94117647058f;

        var hsl = Main.rgbToHsl(color);

        /*
            Shift hue based on thresholds:
            - Counterclockwise before yellow (yellow → orange, red → magenta)
            - Clockwise before blue (lime → green, cyan → blue)
        */
        if (direction == 0f)
            direction = hsl.X is < yellow or > blue ? -1f : 1f;

        hsl.X = (hsl.X + amt * direction + 1f) % 1f; // Bidirectional wrapping
        hsl.Z = Math.Clamp(hsl.Z + shiftLumi, 0f, 1f); // Adjust lightness

        return Main.hslToRgb(hsl);
    }

    /// <summary>
    ///     Performs multiplicative blending between two colors, simulating how lighting affects a texture.
    ///     Each RGB component is multiplied together and normalized to the 0-255 range.
    /// </summary>
    /// <param name="lightColor">The lighting color that modulates the base color</param>
    /// <param name="baseColor">The base texture/material color to be lit</param>
    /// <returns>A new Color with the blended result, alpha is always set to 255</returns>
    public static Color MultiplyBlend(Color lightColor, Color baseColor)
    {
        return new Color(
            lightColor.R * baseColor.R / 255,
            lightColor.G * baseColor.G / 255,
            lightColor.B * baseColor.B / 255,
            255
        );
    }
}
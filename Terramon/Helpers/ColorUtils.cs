namespace Terramon.Helpers;

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
    /// Converts an XNA Color to a hex string without the # symbol
    /// </summary>
    /// <param name="color">The XNA Color to convert</param>
    /// <returns>Hex string in format "RRGGBB" or "RRGGBBAA" if alpha is not 255</returns>
    public static string ToHexString(this Color color)
    {
        return color.A == 255 ?
            // RGB format (no alpha)
            $"{color.R:X2}{color.G:X2}{color.B:X2}" :
            // RGBA format (include alpha)
            $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }
    public static Color Hueshift(this Color color, float amt, float shiftLumi = 0f, float direction = 0f)
    {
        // thresholds for determining best hueshifting direction
        const float Yellow = 0.23529411764f;
        const float Blue = 0.94117647058f;
        Vector3 col = Main.rgbToHsl(color);
        // everything before yellow should shift counterclockwise (yellow to orange, red to magenta)
        // everything before blue should shift clockwise (lime to green, cyan to blue)
        if (direction == 0f)
            direction = (col.X < Yellow || col.X > Blue) ? -1f : 1f;
        float changedHue = col.X + (amt * direction);
        col.X = ((changedHue % 1f) + 1f) % 1f; // bidirectional wrapping
        col.Z = Math.Clamp(col.Z + shiftLumi, 0f, 1f);
        return Main.hslToRgb(col);
    }
}
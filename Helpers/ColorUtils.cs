namespace Terramon.Helpers;

public static class ColorUtils
{
    public static Color FromHex(int hexValue)
    {
        var red = (byte)((hexValue >> 16) & 0xFF);
        var green = (byte)((hexValue >> 8) & 0xFF);
        var blue = (byte)(hexValue & 0xFF);
        return new Color(red, green, blue);
    }
}
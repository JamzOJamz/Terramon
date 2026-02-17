namespace Terramon.Content.Dusts;

public class ColorableDust : ModDust
{
    public override string Texture => "Terramon/Assets/Dusts/ColorableDust";

    public override Color? GetAlpha(Dust dust, Color lightColor)
    {
        return dust.color;
    }

    public override bool Update(Dust dust)
    {
        var num4 = dust.scale * 0.6f;
        if (num4 > 1f)
            num4 = 1f;

        const float brightnessMultiplier = 1.3f;

        var color = dust.color;
        var r = color.R / 255f * num4 * brightnessMultiplier;
        var g = color.G / 255f * num4 * brightnessMultiplier;
        var b = color.B / 255f * num4 * brightnessMultiplier;

        Lighting.AddLight((int)(dust.position.X / 16f), (int)(dust.position.Y / 16f), r, g, b);

        return true;
    }
}
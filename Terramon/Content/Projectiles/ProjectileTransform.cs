using Terramon.Core.ProjectileComponents;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.Projectiles;

/// <summary>
///     A <see cref="ProjectileComponent" /> that sets the width and height of a projectile.
/// </summary>
public class ProjectileTransform : ProjectileComponent
{
    public int Width = 20;
    public int Height = 20;
    public int DrawOffsetY = 0;

    public override void SetDefaults(Projectile proj)
    {
        base.SetDefaults(proj);
        if (!Enabled) return;
        proj.width = Width;
        proj.height = Height;
        proj.ModProjectile.DrawOriginOffsetY = DrawOffsetY;
    }
}
using Terramon.Core.ProjectileComponents;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.Projectiles;

/// <summary>
///     A <see cref="ProjectileComponent" /> to control the visual behaviour of Pokémon pets.
/// </summary>
public class ProjectileVisuals : ProjectileComponent
{
    private float _dustTimer;
    public float DamperAmount = 0; //0 = no effect, 1 = full effect
    public float DustFrequency = 20; //how many frames until dust is spawned
    public int DustID = -1;
    public float DustOffsetX = 0;
    public float DustOffsetY = 0;
    public Vector3 LightColor = Vector3.One;
    public float LightStrength = 0f;
    public Vector3 ShinyLightColor = Vector3.One;

    public override void AI(Projectile proj)
    {
        base.AI(proj);

        if (!Enabled) return;

        var petProj = (PokemonPet)proj.ModProjectile;

        if (LightStrength > 0)
            Lighting.AddLight(proj.Center,
                (petProj.Data is { IsShiny: true } ? ShinyLightColor : LightColor) * LightStrength *
                (Main.raining || proj.wet ? 1 - DamperAmount : 1));

        if (DustID <= -1) return;
        if (_dustTimer >= DustFrequency)
        {
            Dust.NewDust(
                proj.position + new Vector2(proj.spriteDirection == 1 ? proj.width - DustOffsetX : DustOffsetX,
                    DustOffsetY), 1, 1, DustID);
            _dustTimer = 0;
        }
        else
        {
            _dustTimer++;
        }
    }
}
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
    private int _cachedHeight;
    private float _dustTimer;
    public float DamperAmount = 0; //0 = no effect, 1 = full effect
    public float DustFrequency = 20; //how many frames until dust is spawned
    public int DustID = -1;
    public float DustOffsetX = 0;
    public float DustOffsetY = 0;
    public Vector3 LightColor = Vector3.One;
    public float LightStrength = 0f;
    public Vector3 ShinyLightColor = Vector3.One;

    public override void SetDefaults(Projectile proj)
    {
        base.SetDefaults(proj);
        if (!Enabled) return;
        _cachedHeight = proj.height;
    }

    public override void AI(Projectile proj)
    {
        base.AI(proj);

        if (!Enabled) return;

        var petProj = (PokemonPet)proj.ModProjectile;
        //var texture = TextureAssets.Projectile[proj.type].Value;

        if (LightStrength > 0)
            Lighting.AddLight(proj.Center,
                (petProj.Data is { IsShiny: true } ? ShinyLightColor : LightColor) * LightStrength *
                (Main.raining || proj.wet ? 1 - DamperAmount : 1));

        if (DustID <= -1) return;
        if (_dustTimer >= DustFrequency)
        {
            var effects = petProj.CustomSpriteDirection.HasValue
                ? petProj.CustomSpriteDirection.Value == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
                : proj.spriteDirection == -1
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;
            var yOff = _cachedHeight - proj.height;
            Dust.NewDustPerfect(proj.position + new Vector2(
                effects == SpriteEffects.FlipHorizontally ? proj.width - DustOffsetX : DustOffsetX,
                DustOffsetY - yOff), DustID);
            _dustTimer = 0;
        }
        else
        {
            _dustTimer++;
        }
    }
}
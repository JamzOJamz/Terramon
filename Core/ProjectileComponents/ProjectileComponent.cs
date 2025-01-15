using Terramon.Content.Projectiles;

namespace Terramon.Core.ProjectileComponents;

/// <summary>
///     A <see cref="GlobalProjectile" /> that can be enabled and disabled at will.
/// </summary>
public abstract class ProjectileComponent : GlobalProjectile
{
    protected bool Enabled { get; private set; }

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.ModProjectile is PokemonPet;
    }
    
    protected virtual void OnEnabled(Projectile projectile)
    {
    }

    protected virtual void OnDisabled(Projectile projectile)
    {
    }

    public void SetEnabled(Projectile projectile, bool value)
    {
        if (Enabled == value) return;

        Enabled = value;

        if (value)
            OnEnabled(projectile);
        else
            OnDisabled(projectile);
    }
}
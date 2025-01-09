namespace Terramon.Core.ProjectileComponents;

/// <summary>
///     A <see cref="GlobalProjectile" /> that can be enabled and disabled at will.
/// </summary>
public abstract class ProjectileComponent : GlobalProjectile
{
    public bool Enabled { get; private set; }

    public override bool InstancePerEntity => true;

    protected virtual void OnEnabled(Projectile item)
    {
    }

    protected virtual void OnDisabled(Projectile item)
    {
    }

    public void SetEnabled(Projectile item, bool value)
    {
        if (Enabled == value) return;

        Enabled = value;

        if (value)
            OnEnabled(item);
        else
            OnDisabled(item);
    }
}
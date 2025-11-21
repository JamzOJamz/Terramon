using Terraria.GameContent.Drawing;
using Terraria.Graphics.Renderers;

namespace Terramon.Content.Particles;

/// <summary>
///     Provides an interface by which to create custom <see cref="ParticleOrchestrator"/> particles.
///     
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TerramonParticle<T> : ABasicParticle, ILoadable where T : ABasicParticle, ILoadable, new()
{
    internal ParticlePool<T> Pool;
    public virtual int InitialPoolSize => 32;
    public virtual void SetDefaults() { }
    public virtual void OnSpawn() { }
    public sealed override void FetchFromPool()
    {
        base.FetchFromPool();
        SetDefaults();
    }
    public void Load(Mod mod)
        => Pool = new ParticlePool<T>(InitialPoolSize, () => new());
    public void Unload()
        => Pool = null;
}

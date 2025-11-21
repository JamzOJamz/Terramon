using Terraria.GameContent.Drawing;

namespace Terramon.Content.Particles;

public static class ParticleSystem
{
    public static void RequestParticleSpawn<T>(ParticleOrchestraSettings settings, bool clientOnly = true) where T : TerramonParticle<T>, new()
    {
        settings.IndexOfPlayerWhoInvokedThis = (byte)Main.myPlayer;

        SpawnParticleDirect<T>(settings);

        if (clientOnly)
            return;

        // Send packet to spawn same particle for every other client
    }

    private static void SpawnParticleDirect<T>(ParticleOrchestraSettings settings) where T : TerramonParticle<T>, new()
    {
        if (Main.dedServ)
            return;

        var particle = ModContent.GetInstance<T>().Pool.RequestParticle();
        particle.LocalPosition = settings.PositionInWorld;
        particle.Velocity = settings.MovementVector;

        particle.OnSpawn();

        Main.ParticleSystem_World_OverPlayers.Add(particle);
    }
}

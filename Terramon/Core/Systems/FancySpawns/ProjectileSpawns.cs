using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using Terramon.Content.NPCs;

namespace Terramon.Core.Systems.FancySpawns;

public sealed class ProjectileSpawns : GlobalProjectile
{
    public static List<JObject> ProjectileKillRules { get; } = [];
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        foreach (var rule in CollectionsMarshal.AsSpan(ProjectileKillRules))
        {
            var ruleMet = NPCSpawnController.EvaluateNumeric(projectile.type, rule.GetValue("FromProjectile"), out _);
            if (!ruleMet)
                continue;
            var roll = Main.rand.NextFloat() <= (float)rule.GetValue("Chance");
            if (roll)
            {
                var c = projectile.Center;
                NPC.NewNPC(projectile.GetSource_Death(), (int)c.X, (int)c.Y, (int)rule.GetValue("Type"));
            }
        }
    }
}

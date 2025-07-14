using Terraria.Audio;

namespace Terramon.Core.Systems.RealtimeCombatSystem;

public class RealtimeCombatGlobalNPC : GlobalNPC
{
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Main.netMode == NetmodeID.Server || !IsValidForEXPGain(npc)) return;

        var terramonPlayer = TerramonPlayer.LocalPlayer;
        var activePet = terramonPlayer.ActivePetProjectile;

        if (activePet == null)
            return;

        // Play a positive gain sound
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/realtime_exp_gain")
        {
            Volume = 0.5f,
            PitchRange = (-0.1f, 0.1f)
        }, activePet.Projectile.position);

        var expAmount = Main.rand.Next(3, 11); // TODO: Implement actual EXP gain formula based on enemy HP
        var expGainColor = GetEXPGainCombatTextColor();

        // Show combat text above the Pokémon
        CombatText.NewText(activePet.Projectile.getRect(), expGainColor, $"+{expAmount} EXP. Point{(expAmount > 1 ? "s" : "")}");

        // Particle effect
        for (var j = 0; j < 16; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(activePet.Projectile.Center + speed * 16, DustID.PortalBolt,
                activePet.Projectile.velocity + new Vector2(0, Main.rand.NextFloat(-1.75f, -1.25f)),
                newColor: expGainColor);
            d.noGravity = true;
        }
    }

    private static Color GetEXPGainCombatTextColor()
    {
        // Cool blue-green spectrum gradient
        var colors = new[]
        {
            new Color(82, 153, 255),
            new Color(82, 204, 255),
            new Color(82, 255, 153)
        };

        const float time = 1f;
        var progress = (float)Main.timeForVisualEffects / (time * 60f);

        var indexA = (int)progress % colors.Length;
        var indexB = (indexA + 1) % colors.Length;

        var lerpFactor = progress % 1f;

        return Color.Lerp(colors[indexA], colors[indexB], lerpFactor);
    }

    private static bool IsValidForEXPGain(NPC npc)
    {
        return npc.CanBeChasedBy()
               && npc.life <= 0
               && npc.playerInteraction[Main.myPlayer];
    }
}
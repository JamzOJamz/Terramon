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

        var expAmount = CalculateEXPGain(npc);
        var expGainColor = GetEXPGainCombatTextColor();
        
        // Actually gain the EXP for the active Pokémon
        var activeData = terramonPlayer.GetActivePokemon();
        activeData.GainExperience(expAmount, out var levelsGained, out _);

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
        
        // Display level-up message if applicable
        if (levelsGained > 0)
        {
            SoundEngine.PlaySound(SoundID.Item20);
            CombatText.NewText(activePet.Projectile.getRect(), Color.White, "Level Up!", true);
        }
    }
    
    private static int CalculateEXPGain(NPC npc)
    {
        const float baseExpScale = 0.18f; // Adjust this value as needed
   
        var baseExp = npc.lifeMax * baseExpScale;
        var randomFactor = Main.rand.NextFloat(0.8f, 1.2f);
        var finalExp = baseExp * randomFactor;
   
        return Math.Max(1, (int)Math.Round(finalExp));
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
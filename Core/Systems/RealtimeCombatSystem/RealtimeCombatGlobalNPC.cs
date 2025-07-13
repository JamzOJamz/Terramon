using Terramon.Content.Items;
using Terraria.Audio;

namespace Terramon.Core.Systems.RealtimeCombatSystem;

public class RealtimeCombatGlobalNPC : GlobalNPC
{
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Main.netMode == NetmodeID.Server || npc.life > 0 || !npc.CanBeChasedBy() || !npc.playerInteraction[Main.myPlayer]) return;

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
        
        // Just placeholder for now
        var expAmount = Main.rand.Next(3, 11);
        
        // Show combat text above the Pokémon
        CombatText.NewText(activePet.Projectile.getRect(), new Color(64, 156, 255), $"+{expAmount} EXP.");
        
        // Particle effect
        for (var j = 0; j < 16; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(activePet.Projectile.Center + speed * 16, DustID.FrostHydra, new Vector2(0, -1.5f));
            d.noGravity = true;
        }
    }
}
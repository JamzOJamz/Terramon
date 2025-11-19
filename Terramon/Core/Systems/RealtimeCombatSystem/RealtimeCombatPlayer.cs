using Terraria.Audio;

namespace Terramon.Core.Systems.RealtimeCombatSystem;

public class RealtimeCombatPlayer : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        var terramonPlayer = Player.Terramon();
        var activePet = terramonPlayer.ActivePetProjectile;

        if (activePet == null)
            return;

        // Play a hit sound
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/hit_normal_damage")
        {
            Volume = 0.165f,
            PitchVariance = 0.12f
        }, activePet.Projectile.position);

        // Display text in chat
        var transferredDamage = info.SourceDamage / 3;
        //Main.NewText($"Received {info.SourceDamage} damage, Pokémon will receive {transferredDamage}");

        // Show combat text above the Pokémon
        CombatText.NewText(activePet.Projectile.getRect(), GetDamageCombatTextColor(), transferredDamage);

        // Apply damage to the Pokémon
        var activeData = terramonPlayer.GetActivePokemon();
        activeData.Damage((ushort)transferredDamage, true);

        // Register realtime combat hit on Pokémon pet projectile
        activePet.RealtimeHit();
    }

    private static Color GetDamageCombatTextColor()
    {
        // Subtle violet gradient
        var colors = new[]
        {
            new Color(183, 100, 255),
            new Color(201, 100, 255),
            new Color(217, 100, 255)
        };

        const float time = 1f;
        var progress = (float)Main.timeForVisualEffects / (time * 60f);

        var indexA = (int)progress % colors.Length;
        var indexB = (indexA + 1) % colors.Length;

        var lerpFactor = progress % 1f;

        return Color.Lerp(colors[indexA], colors[indexB], lerpFactor);
    }
}
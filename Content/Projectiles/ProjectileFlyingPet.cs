using Terramon.Core.ProjectileComponents;
using Terraria.DataStructures;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Terramon.Content.Projectiles;

public class ProjectileFlyingPet : ProjectileComponent
{
    public int FrameCount = 2;
    public int FrameTime = 10;
    
    public override void SetDefaults(Projectile proj)
    {
        base.SetDefaults(proj);
        if (!Enabled) return;

        proj.CloneDefaults(ProjectileID.LilHarpy);

        proj.ModProjectile.AIType = ProjectileID.LilHarpy;
        
        Main.projFrames[proj.type] = FrameCount;
        ProjectileID.Sets.CharacterPreviewAnimations[proj.type] = new SettingsForCharacterPreview
        {
            Offset = new Vector2(2f, -6f),
            SpriteDirection = -1,
            CustomAnimation = CustomAnimation
        };
        
        var petProj = (PokemonPet)proj.ModProjectile;
        petProj.FindFrame = FindFrame;
    }
    
    private static void CustomAnimation(Projectile proj, bool walking)
    {
        var petProj = (PokemonPet)proj.ModProjectile;
        if (!walking)
        {
            petProj.CustomFrameCounter = 0;
        }
        else
        {
            petProj.CustomFrameCounter++;
        }
    }
    
    public override void AI(Projectile projectile)
    {
        if (!Enabled) return;
        
        var petProj = (PokemonPet)projectile.ModProjectile;
        petProj.CustomFrameCounter++;
    }

    private void FindFrame(PokemonPet proj)
    {
        if (proj.CustomFrameCounter < FrameTime * FrameCount)
            proj.Projectile.frame = (int)Math.Floor(proj.CustomFrameCounter / (float)FrameTime) % FrameCount;
        else
        {
            proj.CustomFrameCounter = 0;
            proj.Projectile.frame = 0;
        }
    }
}
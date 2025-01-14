using Terramon.Core.ProjectileComponents;
using Terraria.DataStructures;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Terramon.Content.Projectiles;

public class ProjectileGenericPet : ProjectileComponent
{
    private bool _customAnimationIsWalking;
    public string AnimationType = "StraightForward";
    public int ExtraStopFrame = -1;
    public int FrameCount = 2;
    public int FrameTime = 10;
    public bool IsClassic = true; //TODO: remove once all classic pokemon sprites are replaced with custom ones
    public float StopThreshold = 0.15f;
    public float WalkSpeedModifier = 1f;

    public override void SetDefaults(Projectile proj)
    {
        base.SetDefaults(proj);
        if (!Enabled) return;

        proj.CloneDefaults(ProjectileID.BlueChickenPet);

        proj.ModProjectile.AIType = ProjectileID.BlueChickenPet;

        Main.projFrames[proj.type] = FrameCount;
        ProjectileID.Sets.CharacterPreviewAnimations[proj.type] = new SettingsForCharacterPreview
        {
            Offset = new Vector2(2f, 0),
            SpriteDirection = -1,
            CustomAnimation = CustomAnimation
        };

        var petProj = (PokemonPet)proj.ModProjectile;
        if (IsClassic) petProj.CustomFrameCounter = FrameTime;
        petProj.FindFrame = FindFrame;
    }

    private void CustomAnimation(Projectile proj, bool walking)
    {
        var petProj = (PokemonPet)proj.ModProjectile;
        if (!walking)
            petProj.CustomFrameCounter = IsClassic ? FrameTime : 0;
        else
            petProj.CustomFrameCounter++;
        _customAnimationIsWalking = walking;
    }

    public override void AI(Projectile projectile)
    {
        if (!Enabled) return;

        var petProj = (PokemonPet)projectile.ModProjectile;

        if (projectile.ai[0] != 1 && WalkSpeedModifier < 1f) // If not flying to catch up to player
            projectile.velocity.X *= WalkSpeedModifier + (1f - WalkSpeedModifier) / 1.5f;

        // If the pet is moving, increase the frame counter.
        // If not moving, continue to increase the frame counter until it reaches the end of the current animation cycle.
        if (Math.Abs(projectile.velocity.X) > StopThreshold || petProj.CustomFrameCounter != 0)
            petProj.CustomFrameCounter++;
    }

    private void FindFrame(PokemonPet proj)
    {
        switch (AnimationType)
        {
            // Animates all frames in a sequential order
            case "StraightForward" when proj.CustomFrameCounter < FrameTime * FrameCount && proj.CustomFrameCounter > 0:
                // Check if animation is ended on extra stop frame
                if (ExtraStopFrame != -1 && Math.Abs(proj.Projectile.velocity.X) <= StopThreshold &&
                    !proj.Projectile.isAPreviewDummy && proj.CustomFrameCounter / (float)FrameTime == ExtraStopFrame)
                {
                    proj.CustomFrameCounter = 0;
                    proj.Projectile.frame = IsClassic ? 1 : 0;
                    break;
                }

                proj.Projectile.frame = (int)Math.Floor(proj.CustomFrameCounter / (float)FrameTime) % FrameCount;
                break;
            case "StraightForward":
                proj.CustomFrameCounter = 0;
                proj.Projectile.frame = IsClassic ? 1 : 0;
                break;
            // Same as StraightForward, but skips the first frame (which is idle only)
            case "IdleForward"
                when proj.CustomFrameCounter < FrameTime * (FrameCount - 1) && proj.CustomFrameCounter > 0:

                // Check if animation is ended on extra stop frame
                if (ExtraStopFrame != -1 && Math.Abs(proj.Projectile.velocity.X) <= StopThreshold &&
                    !proj.Projectile.isAPreviewDummy && proj.CustomFrameCounter / (float)FrameTime == ExtraStopFrame)
                {
                    proj.CustomFrameCounter = 0;
                    proj.Projectile.frame = 0;
                    break;
                }

                proj.Projectile.frame = ((int)Math.Floor(proj.CustomFrameCounter / (float)FrameTime) + 1) % FrameCount;
                break;
            case "IdleForward":
                proj.CustomFrameCounter = 0;
                var flag = proj.Projectile.isAPreviewDummy
                    ? _customAnimationIsWalking
                    : Math.Abs(proj.Projectile.velocity.X) > StopThreshold;
                proj.Projectile.frame = flag ? 1 : 0;
                break;
            // Alternates between frame sequences
            case "Alternate":
            {
                var cycleLength = FrameCount + 1;
                var alternateFrame = proj.CustomFrameCounter / FrameTime % cycleLength;

                // Check if animation is ended
                if (Math.Abs(proj.Projectile.velocity.X) <= StopThreshold && !proj.Projectile.isAPreviewDummy)
                    switch (cycleLength)
                    {
                        case 4 when alternateFrame is 0 or 2:
                        case 6 when alternateFrame is 0 or 3:
                            proj.CustomFrameCounter = 0;
                            proj.Projectile.frame = 0;
                            break;
                    }

                proj.Projectile.frame = cycleLength switch
                {
                    4 => alternateFrame switch
                    {
                        0 or 2 => 0,
                        1 => 1,
                        3 => 2,
                        _ => 0
                    },
                    6 => alternateFrame switch
                    {
                        0 or 3 => 0,
                        1 => 1,
                        2 => 2,
                        4 => 3,
                        5 => 4,
                        _ => 0
                    },
                    _ => 0
                };
                break;
            }
            default:
                proj.Projectile.frame = IsClassic ? 1 : 0;
                break;
        }
    }
}
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
/*
    public float WalkSpeedModifier = 1f;
*/

    public override void SetDefaults(Projectile proj)
    {
        if (!Enabled) return;

        proj.CloneDefaults(ProjectileID.BlueChickenPet);
        proj.aiStyle = 0;

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

    public override void AI(Projectile p)
    {
        if (!Enabled) return;

        // Hardcode the entity's height to 20 (cloned AI compatibility)
        p.height = 20;

        var petProj = (PokemonPet)p.ModProjectile;

        /*if (projectile.ai[0] != 1 && WalkSpeedModifier < 1f) // If not flying to catch up to player
            projectile.velocity.X *= WalkSpeedModifier + (1f - WalkSpeedModifier) / 1.5f;*/

        // If the pet is moving, increase the frame counter.
        // If not moving, continue to increase the frame counter until it reaches the end of the current animation cycle.
        if (Math.Abs(p.velocity.X) > StopThreshold || petProj.CustomFrameCounter != 0)
            petProj.CustomFrameCounter++;

        Player o = Main.player[p.owner];

        if (!o.active)
        {
            p.active = false;
            return;
        }

        bool playerLeft = false;
        bool playerRight = false;
        bool playerDown = false;
        bool goingIntoTileX = false;

        var battle = o.Terramon().BattleClient;
        bool inBattle = battle != null && battle.BattleOngoing;
        bool hasCustomTarget = petProj.CustomTargetPosition.HasValue;

        Vector2 targetPosition = hasCustomTarget ? petProj.CustomTargetPosition.Value : o.position;
        Vector2 targetSize = hasCustomTarget ? Vector2.Zero : o.Size;
        Vector2 targetCenter = targetPosition + targetSize * 0.5f;
        Vector2 targetVelo = hasCustomTarget ? Vector2.Zero : o.velocity;
        int extraDistance = hasCustomTarget ? 0 : 85;

        Vector2 projCenter = p.Center;
        Vector2 toPlayer = targetCenter - projCenter;
        float distToPlayerSq = toPlayer.LengthSquared();

        if (Math.Abs(toPlayer.X) > 8f)
        {
            if (targetCenter.X < projCenter.X - extraDistance)
                playerLeft = true;
            else if (targetCenter.X > projCenter.X + extraDistance)
                playerRight = true;
        }
        else
        {
            p.velocity.X *= 0.8f;
        }

        if (p.ai[1] == 0f)
        {
            if (o.rocketDelay2 > 0)
            {
                p.ai[0] = 1f;
            }
            const float teleportDistance = 2000f * 2000f;
            const float fastDistance = 500f * 500f; // i think???

            if (distToPlayerSq > teleportDistance)
            {
                p.position = targetPosition - (p.Size * 0.5f);
            }
            else if (distToPlayerSq > fastDistance || Math.Abs(toPlayer.Y) > 300f)
            {
                if (toPlayer.Y > 0f && p.velocity.Y < 0f)
                    p.velocity.Y = 0f;
                if (toPlayer.Y < 0f && p.velocity.Y > 0f)
                    p.velocity.Y = 0f;
                p.ai[0] = 1f;
            }
        }

        bool flying = p.ai[0] != 0f;
        if (flying)
        {
            const float num184 = 200f * 200f;
            p.tileCollide = false;
            float num185 = targetCenter.X - projCenter.X;
            float num192 = targetCenter.Y - projCenter.Y;
            float num193 = MathF.Sqrt(distToPlayerSq);
            float num183 = 0.4f;
            float num195 = 12f;
            float roughLength = Math.Abs(targetVelo.X) + Math.Abs(targetVelo.Y);

            if (num195 < roughLength)
                num195 = roughLength;

            if (distToPlayerSq < num184 && targetVelo.Y == 0f && p.position.Y + p.height <= targetPosition.Y + targetSize.Y && !Collision.SolidCollision(p.position, p.width, p.height))
            {
                p.ai[0] = 0f;
                if (p.velocity.Y < -6f)
                    p.velocity.Y = -6f;
            }
            if (distToPlayerSq < 60f * 60f)
            {
                num185 = p.velocity.X;
                num192 = p.velocity.Y;
            }
            else
            {
                num193 = num195 / num193;
                num185 *= num193;
                num192 *= num193;
            }
            if (p.velocity.X < num185)
            {
                p.velocity.X += num183;
                if (p.velocity.X < 0f)
                    p.velocity.X += num183 * 1.5f;
            }
            if (p.velocity.X > num185)
            {
                p.velocity.X -= num183;
                if (p.velocity.X > 0f)
                    p.velocity.X -= num183 * 1.5f;
            }
            if (p.velocity.Y < num192)
            {
                p.velocity.Y += num183;
                if (p.velocity.Y < 0f)
                    p.velocity.Y += num183 * 1.5f;
            }
            if (p.velocity.Y > num192)
            {
                p.velocity.Y -= num183;
                if (p.velocity.Y > 0f)
                    p.velocity.Y -= num183 * 1.5f;
            }

            if (p.velocity.X > 0f)
                p.spriteDirection = -1;
            else if (p.velocity.X < -0.5f)
                p.spriteDirection = 1;

            p.rotation = MathHelper.Clamp(p.velocity.X * 0.025f, -0.4f, 0.4f);
        }
        else
        {
            bool flag6 = false;
            if (p.ai[1] != 0f)
            {
                playerLeft = false;
                playerRight = false;
            }
            else if (!flag6)
            {
                p.rotation = 0f;
            }

            p.tileCollide = true;
            float maxXSpeed = 6f;
            float num72 = 0.2f;
            float roughLength = Math.Abs(targetVelo.X) + Math.Abs(targetVelo.Y);
            if (maxXSpeed < roughLength)
            {
                maxXSpeed = roughLength;
                num72 = 0.3f;
            }

            if (playerLeft)
            {
                if (p.velocity.X > -3.5)
                {
                    p.velocity.X -= num72;
                }
                else
                {
                    p.velocity.X -= num72 * 0.25f;
                }
            }
            else if (playerRight)
            {
                if (p.velocity.X < 3.5)
                {
                    p.velocity.X += num72;
                }
                else
                {
                    p.velocity.X += num72 * 0.25f;
                }
            }
            else
            {
                p.velocity.X *= 0.9f;
                if (p.velocity.X >= 0f - num72 && p.velocity.X <= num72)
                {
                    p.velocity.X = 0f;
                }
            }
            if (playerLeft || playerRight)
            {
                int centerTileX = (int)projCenter.X / 16;
                int centerTileY = (int)projCenter.Y / 16;
                if (playerLeft)
                    centerTileX--;
                if (playerRight)
                    centerTileX++;
                centerTileX += (int)p.velocity.X;
                if (WorldGen.SolidTile(centerTileX, centerTileY))
                {
                    goingIntoTileX = true;
                }
            }
            if (targetPosition.Y + targetSize.Y - 8f > p.position.Y + p.height)
            {
                playerDown = true;
            }
            Collision.StepUp(ref p.position, ref p.velocity, p.width, p.height, ref p.stepSpeed, ref p.gfxOffY);
            if (p.velocity.Y == 0f)
            {
                if (!playerDown && (p.velocity.X < 0f || p.velocity.X > 0f))
                {
                    int num75 = (int)(p.position.X + p.width / 2) / 16;
                    int j3 = (int)(p.position.Y + p.height / 2) / 16 + 1;
                    if (playerLeft)
                    {
                        num75--;
                    }
                    if (playerRight)
                    {
                        num75++;
                    }
                    WorldGen.SolidTile(num75, j3);
                }
                if (goingIntoTileX)
                {
                    int bottomTileX = (int)(p.position.X + p.width / 2) / 16;
                    int bottomTileY = (int)(p.position.Y + p.height) / 16;
                    Tile t = Main.tile[bottomTileX, bottomTileY];
                    if (WorldGen.SolidTileAllowBottomSlope(bottomTileX, bottomTileY) || t.IsHalfBlock || t.Slope > 0)
                    {
                        try
                        {
                            bottomTileX = (int)(p.position.X + p.width / 2) / 16;
                            bottomTileY = (int)(p.position.Y + p.height / 2) / 16;
                            if (playerLeft)
                            {
                                bottomTileX--;
                            }
                            if (playerRight)
                            {
                                bottomTileX++;
                            }
                            bottomTileX += (int)p.velocity.X;
                            if (!WorldGen.SolidTile(bottomTileX, bottomTileY - 1) && !WorldGen.SolidTile(bottomTileX, bottomTileY - 2))
                            {
                                p.velocity.Y = -5.1f;
                            }
                            else if (!WorldGen.SolidTile(bottomTileX, bottomTileY - 2))
                            {
                                p.velocity.Y = -7.1f;
                            }
                            else if (WorldGen.SolidTile(bottomTileX, bottomTileY - 5))
                            {
                                p.velocity.Y = -11.1f;
                            }
                            else if (WorldGen.SolidTile(bottomTileX, bottomTileY - 4))
                            {
                                p.velocity.Y = -10.1f;
                            }
                            else
                            {
                                p.velocity.Y = -9.1f;
                            }
                        }
                        catch
                        {
                            p.velocity.Y = -9.1f;
                        }
                    }
                }
            }
            if (p.velocity.X > maxXSpeed)
                p.velocity.X = maxXSpeed;
            if (p.velocity.X < 0f - maxXSpeed)
                p.velocity.X = 0f - maxXSpeed;

            if (inBattle && p.velocity.X == 0f)
            {
                var pokeAvatar = battle.Foe.SyncedEntity;
                if (pokeAvatar is Player plr)
                {
                    var pet = plr.Terramon().ActivePetProjectile;
                    if (pet != null)
                        pokeAvatar = pet.Projectile;
                }
                int oppoDir = pokeAvatar.direction;
                p.direction = -oppoDir;
                p.spriteDirection = oppoDir;
            }
            else if (p.velocity.X < 0f || (p.velocity.X < -num72 && playerLeft))
            {
                p.direction = -1;
                p.spriteDirection = 1;
            }
            else if (p.velocity.X > 0f || (p.velocity.X > num72 && playerRight))
            {
                p.direction = 1;
                p.spriteDirection = -1;
            }

            p.velocity.Y += 0.4f;
            if (p.velocity.Y > 10f)
            {
                p.velocity.Y = 10f;
            }
        }
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
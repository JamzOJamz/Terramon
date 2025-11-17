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
        if (!Enabled) return;

        proj.CloneDefaults(ProjectileID.LilHarpy);
        proj.aiStyle = 0;
        proj.tileCollide = false;
        
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
    
    public override void AI(Projectile p)
    {
        if (!Enabled) return;
        
        var petProj = (PokemonPet)p.ModProjectile;
        petProj.CustomFrameCounter++;

        Player o = Main.player[p.owner];

        if (!o.active)
        {
            p.active = false;
            return;
        }

        float speed = 0.4f;
        const float teleportDistance = 2000f;
        bool shouldTeleport = false;

        var battle = o.Terramon().BattleClient;
        bool inBattle = battle != null && battle.BattleOngoing;
        bool hasCustomTarget = petProj.CustomTargetPosition.HasValue;

        Vector2 targetPosition = hasCustomTarget ? petProj.CustomTargetPosition.Value : o.position;
        Vector2 targetSize = hasCustomTarget ? Vector2.Zero : o.Size;
        Vector2 targetCenter = targetPosition + targetSize * 0.5f;
        Vector2 targetVelo = hasCustomTarget ? Vector2.Zero : o.velocity;
        int targetDir = hasCustomTarget ? 0 : o.direction;

        Vector2 projCenter = p.Center;
        float extraDistance = hasCustomTarget ? 0f : 60f;
        float randomFactor = hasCustomTarget ? 0f : 1f;

        float toPlayerX = targetCenter.X - projCenter.X;
        float toPlayerY = targetCenter.Y - projCenter.Y;

        if (randomFactor != 0f)
        {
            toPlayerY += Main.rand.Next(-10, 21) * randomFactor;
            toPlayerX += Main.rand.Next(-10, 21) * randomFactor;
        }

        toPlayerX += extraDistance * -targetDir;
        toPlayerY -= extraDistance;

        float distToPlayer = (float)Math.Sqrt(toPlayerX * toPlayerX + toPlayerY * toPlayerY);
        float num141 = (distToPlayer >= 400f) ? 10f : 6f;
        if (distToPlayer < 100f && targetVelo.Y == 0f && p.position.Y + p.height <= targetPosition.Y + targetSize.Y && !Collision.SolidCollision(p.position, p.width, p.height))
        {
            p.ai[0] = 0f;
            if (p.velocity.Y < -6f)
                p.velocity.Y = -6f;
        }
        if (distToPlayer < 50f)
        {
            if (Math.Abs(p.velocity.X) > 2f || Math.Abs(p.velocity.Y) > 2f)
                p.velocity *= 0.99f;
            speed = 0.01f;
        }
        else
        {
            if (distToPlayer < 100f)
                speed = 0.1f;
            if (distToPlayer > teleportDistance)
                shouldTeleport = true;

            distToPlayer = num141 / distToPlayer;
            toPlayerX *= distToPlayer;
            toPlayerY *= distToPlayer;
        }
        if (p.velocity.X < toPlayerX)
        {
            p.velocity.X += speed;
            if (speed > 0.05f && p.velocity.X < 0f)
                p.velocity.X += speed;
        }
        if (p.velocity.X > toPlayerX)
        {
            p.velocity.X -= speed;
            if (speed > 0.05f && p.velocity.X > 0f)
                p.velocity.X -= speed;
        }
        if (p.velocity.Y < toPlayerY)
        {
            p.velocity.Y += speed;
            if (speed > 0.05f && p.velocity.Y < 0f)
                p.velocity.Y += speed * 2f;
        }
        if (p.velocity.Y > toPlayerY)
        {
            p.velocity.Y -= speed;
            if (speed > 0.05f && p.velocity.Y > 0f)
                p.velocity.Y -= speed * 2f;
        }

        if (inBattle)
        {
            var pokeAvatar = battle.Foe.SyncedEntity;
            if (pokeAvatar is Player plr)
            {
                var pet = plr.Terramon().ActivePetProjectile;
                if (pet != null)
                    pokeAvatar = pet.Projectile;
            }
            Vector2 oppoCenter = pokeAvatar.Center;
            p.spriteDirection = p.direction = (oppoCenter.X < projCenter.X) ? 1 : -1;
        }
        else if (p.velocity.X > 0.25)
            p.spriteDirection = p.direction = -1;
        else if (p.velocity.X < -0.25)
            p.spriteDirection = p.direction = 1;
        p.rotation = p.velocity.X * 0.05f;
        if (shouldTeleport)
        {
            for (int k = 0; k < 12; k++)
            {
                float dustSpeedX = 1f - Main.rand.NextFloat() * 2f;
                float dustSpeedY = 1f - Main.rand.NextFloat() * 2f;
                Dust d = Dust.NewDustDirect(p.position, p.width, p.height, DustID.Smoke, dustSpeedX, dustSpeedY);
                d.noLightEmittence = true;
                d.noGravity = true;
            }
            p.Center = targetCenter;
            p.velocity = Vector2.Zero;
            if (Main.myPlayer == p.owner)
                p.netUpdate = true;
        }
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
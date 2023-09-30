using Terraria.ID;

namespace Terramon.Content.AI;

public class BounceAI : AIController
{
    private readonly float bounceFrequency;
    private readonly float bounceMaxRange;
    private readonly float bounceMinRange;
    private readonly float changeDirectionChance;
    private readonly float fallSpeedMultiplier;
    private readonly float horizontalSpeedMax;
    private readonly float horizontalSpeedMin;
    private readonly float jumpSpeedMultiplier;
    private readonly int maxJumpClearance;
    private bool hasFirstDir;

    public BounceAI(NPC npc, float jumpSpeedMultiplier = 1f, float fallSpeedMultiplier = 1f, float bounceMinRange = -7f,
        float bounceMaxRange = -5f, float bounceFrequency = 50f, float horizontalSpeedMin = 2f,
        float horizontalSpeedMax = 3.5f, float changeDirectionChance = 1f, int maxJumpClearance = -1) : base(npc)
    {
        this.jumpSpeedMultiplier = jumpSpeedMultiplier;
        this.fallSpeedMultiplier = fallSpeedMultiplier;
        this.bounceMinRange = bounceMinRange;
        this.bounceMaxRange = bounceMaxRange;
        this.bounceFrequency = bounceFrequency;
        this.horizontalSpeedMin = horizontalSpeedMin;
        this.horizontalSpeedMax = horizontalSpeedMax;
        this.changeDirectionChance = changeDirectionChance;
        this.maxJumpClearance = maxJumpClearance;
    }

    private ref float AI_State => ref NPC.ai[0];
    private ref float AI_Timer => ref NPC.ai[1];
    private ref float AI_JumpVelocity => ref NPC.ai[2];
    private ref float AI_JumpDirection => ref NPC.ai[3];

    public override void AI()
    {
        if (NPC.collideY) NPC.velocity.Y = 0;
        switch (AI_State)
        {
            case (float)ActionState.Idle:
                Idle();
                break;
            case (float)ActionState.Jump:
                Jump();
                break;
        }
    }

    private void Idle()
    {
        var isGrounded = NPC.velocity.Y == 0;
        if (isGrounded)
        {
            NPC.velocity.X *= 0.7f;
            AI_Timer++;
        }
        else
        {
            AI_Timer = 0;
        }

        if ((int)AI_Timer == (int)(bounceFrequency / 2) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Main.rand.NextFloat() <= changeDirectionChance || !hasFirstDir)
            {
                AI_JumpDirection = Main.rand.NextBool() ? 1 : -1;
                if (!hasFirstDir) hasFirstDir = true;
            }

            if (maxJumpClearance != -1)
            {
                if (Collision.SolidCollision(NPC.Left - new Vector2(8, maxJumpClearance), 8, 8))
                    AI_JumpDirection = -1;
                else if (Collision.SolidCollision(NPC.Right + new Vector2(8, -maxJumpClearance), 8, 8))
                    AI_JumpDirection = 1;
            }

            NPC.netUpdate = true;
        }

        NPC.spriteDirection = (int)AI_JumpDirection * -1;
        if (AI_Timer <= bounceFrequency) return;
        if (isGrounded) AI_State = (float)ActionState.Jump;
        AI_Timer = 0;
    }

    private void Jump()
    {
        AI_Timer++;
        if (AI_Timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            NPC.velocity.Y = Main.rand.NextFloat(bounceMinRange, bounceMaxRange);
            var jumpStrength = Main.rand.NextFloat(horizontalSpeedMin, horizontalSpeedMax);
            AI_JumpVelocity = AI_JumpDirection == 1 ? -jumpStrength : jumpStrength;
            NPC.netUpdate = true;
        }

        NPC.velocity.X = AI_JumpVelocity;
        if (NPC.velocity.Y > 0) NPC.velocity.Y *= fallSpeedMultiplier;
        else NPC.velocity.Y *= jumpSpeedMultiplier;
        if (NPC.velocity.Y != 0) return;
        AI_State = (float)ActionState.Idle;
        AI_Timer = 0;
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        switch (NPC.frameCounter)
        {
            case < 10:
                NPC.frame.Y = (int)Frame.One * frameHeight;
                break;
            case < 20:
                NPC.frame.Y = (int)Frame.Two * frameHeight;
                break;
            default:
                NPC.frameCounter = 0;
                break;
        }
    }

    private enum ActionState
    {
        Idle,
        Jump
    }

    private enum Frame
    {
        One,
        Two
    }
}
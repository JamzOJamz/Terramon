using Terraria;
using Terraria.ID;

namespace Terramon.Content.AI;

public class WalkingAI : AIController
{
    private readonly int stopFrequency;
    private readonly float walkSpeed;

    private int collideTimer;

    public WalkingAI(NPC npc, float walkSpeed = 1f, int stopFrequency = 200) : base(npc)
    {
        this.walkSpeed = walkSpeed;
        this.stopFrequency = stopFrequency;
    }

    private ref float AI_State => ref NPC.ai[0];
    private ref float AI_Timer => ref NPC.ai[1];
    private ref float AI_WalkDir => ref NPC.ai[2];

    public override void AI()
    {
        switch (AI_State)
        {
            case (float)ActionState.Idle:
                Idle();
                break;
            case (float)ActionState.Walking:
                Walking();
                break;
        }
    }

    private void Idle()
    {
        if (NPC.velocity.Y == 0)
        {
            NPC.velocity.X *= 0.85f;
            AI_Timer++;
        }

        if (AI_Timer != 120) return;
        AI_State = (float)ActionState.Walking;
        AI_Timer = 0;
    }

    private void Walking()
    {
        AI_Timer++;
        if (Main.netMode != NetmodeID.MultiplayerClient)
            switch (AI_Timer)
            {
                case 1:
                    AI_WalkDir = Main.rand.NextBool() ? 1 : -1;
                    NPC.netUpdate = true;
                    break;
                case >= 120 when Main.rand.NextBool(stopFrequency):
                    AI_State = (float)ActionState.Idle;
                    AI_Timer = 0;
                    NPC.netUpdate = true;
                    return;
            }

        if (NPC.collideX)
        {
            if (collideTimer < 10)
            {
                NPC.velocity.Y = -2f;
            }
            else if (NPC.velocity.Y == 0)
            {
                AI_WalkDir *= -1;
                NPC.netUpdate = true;
            }

            collideTimer++;
        }
        else
        {
            collideTimer = 0;
        }

        NPC.velocity.X = AI_WalkDir * walkSpeed;
        NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
    }

    public override void FindFrame(int frameHeight)
    {
        if (AI_State == (float)ActionState.Idle)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y = (int)Frame.Two * frameHeight;
            return;
        }

        NPC.frameCounter++;
        if (NPC.frameCounter < FrameSpeed)
            NPC.frame.Y = (int)Frame.One * frameHeight;
        else if (NPC.frameCounter < FrameSpeed * 2)
            NPC.frame.Y = (int)Frame.Two * frameHeight;
        else
            NPC.frameCounter = 0;
    }

    private enum ActionState
    {
        Idle,
        Walking
    }

    private enum Frame
    {
        One,
        Two
    }
}
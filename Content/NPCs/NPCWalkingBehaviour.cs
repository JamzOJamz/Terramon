using Terramon.Core.NPCComponents;
using Terraria.ID;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> for adding basic walking AI to an NPC.
/// </summary>
public class NPCWalkingBehaviour : NPCComponent
{
    private const int FrameSpeed = 10;
    private int collideTimer;
    public int FrameCount = 2;
    private NPC NPC;
    public int StopFrequency = 200;
    public float WalkSpeed = 1f;

    protected override bool CacheInstances => true;

    private ref float AI_State => ref NPC.ai[0];
    private ref float AI_Timer => ref NPC.ai[1];
    private ref float AI_WalkDir => ref NPC.ai[2];

    public override void OnEnabled(NPC npc)
    {
        NPC = npc;
    }

    public override void SetDefaults(NPC npc)
    {
        base.SetDefaults(npc);
        if (!Enabled) return;
        Main.npcFrameCount[npc.type] = FrameCount;
    }

    public override void AI(NPC npc)
    {
        if (!Enabled) return;

        // Smooth walking over slopes
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

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
                case >= 120 when Main.rand.NextBool(StopFrequency):
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

        NPC.velocity.X = AI_WalkDir * WalkSpeed;
        NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled) return;

        if (AI_State == (float)ActionState.Idle && !NPC.IsABestiaryIconDummy)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y = (int)Frame.One * frameHeight;
            return;
        }

        NPC.frameCounter++;

        switch (FrameCount)
        {
            case 2:
                switch (NPC.frameCounter)
                {
                    case < FrameSpeed:
                        NPC.frame.Y = (int)Frame.Two * frameHeight;
                        break;
                    case < FrameSpeed * 2:
                        NPC.frame.Y = (int)Frame.One * frameHeight;
                        break;
                    default:
                        NPC.frameCounter = 0;
                        break;
                }

                break;
            case 3:
                switch (NPC.frameCounter)
                {
                    case < FrameSpeed:
                        NPC.frame.Y = (int)Frame.Two * frameHeight;
                        break;
                    case < FrameSpeed * 2:
                        NPC.frame.Y = (int)Frame.One * frameHeight;
                        break;
                    case < FrameSpeed * 3:
                        NPC.frame.Y = (int)Frame.Three * frameHeight;
                        break;
                    case < FrameSpeed * 4:
                        NPC.frame.Y = (int)Frame.One * frameHeight;
                        break;
                    default:
                        NPC.frameCounter = 0;
                        break;
                }

                break;
        }
    }

    private enum ActionState
    {
        Idle,
        Walking
    }

    private enum Frame
    {
        One,
        Two,
        Three
    }
}
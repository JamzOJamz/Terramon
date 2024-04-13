using System;
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
    public int FrameTime = 10;
    private int collideTimer;
    public int FrameCount = 2;
    private NPC NPC;
    public int StopFrequency = 200;
    public float WalkSpeed = 1f;
    public string AnimationType = "StraightForward";
    public bool IsClassic = true; //TODO: remove once all classic pokemon sprites are replaced with custom ones

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

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (!Enabled) return;

        npc.velocity.X = 0;
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
        
        if (NPC.velocity.X != 0)
            NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;

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
        
        // Define constants
        const float Acceleration = 0.05f; // Acceleration rate
        const float Deceleration = 0.2f; // Deceleration rate

        // Update velocity based on AI behavior (this part may be called each frame)
        if (AI_WalkDir != 0) {
            // Accelerate towards the desired direction
            NPC.velocity.X += AI_WalkDir * Acceleration;
    
            // Clamp velocity to maximum speed
            NPC.velocity.X = Math.Clamp(NPC.velocity.X, -WalkSpeed, WalkSpeed);
        } else
        {
            switch (NPC.velocity.X)
            {
                // Decelerate when no input is given
                case > 0:
                    NPC.velocity.X -= Deceleration;
                    break;
                case < 0:
                    NPC.velocity.X += Deceleration;
                    break;
            }

            // Ensure velocity doesn't change direction when slowing down
            if (Math.Abs(NPC.velocity.X) < Deceleration)
                NPC.velocity.X = 0;
        }

        NPC.spriteDirection = AI_WalkDir == 1 ? 1 : -1;
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled) return;

        if (AI_State == (float)ActionState.Idle && !NPC.IsABestiaryIconDummy)
        {
            NPC.frameCounter = IsClassic ? FrameTime : 0;
            NPC.frame.Y = IsClassic ? frameHeight : 0;
            return;
        }

        NPC.frameCounter++;

        switch (AnimationType)
        {
            case "StraightForward":
                if (NPC.frameCounter < FrameTime * FrameCount)
                    NPC.frame.Y = (int)Math.Floor(NPC.frameCounter / FrameTime) * frameHeight;
                else
                    NPC.frameCounter = 0;
                break;
            case "IdleForward": //same as StraightForward but it skips the first frame (which is idle only)
                /*if (NPC.frameCounter < FrameTime)
                    NPC.frameCounter = FrameTime;*/
                if (NPC.frameCounter < FrameTime * FrameCount)
                    NPC.frame.Y = (int)Math.Floor(NPC.frameCounter / FrameTime) * frameHeight;
                else
                    NPC.frameCounter = FrameTime;
                break;
            case "Alternate":
                if (NPC.frameCounter >= FrameTime * 2 && NPC.frameCounter < FrameTime * (FrameCount * 2))
                {
                    if (Math.Floor(NPC.frameCounter / FrameTime) % 2 > 0)
                        NPC.frame.Y = ((int)Math.Floor(NPC.frameCounter / FrameTime) / 2) * frameHeight;
                    else
                        NPC.frame.Y = 0;
                }
                else                                   //set it to FrameTime so it skips the first frame
                    NPC.frameCounter = FrameTime * 2; //(since each frame gets a first frame this would play the first frame 3 times in a row)
                break;
            default:
                NPC.frame.Y = 0;
                break;
        }
    }

    private enum ActionState
    {
        Idle,
        Walking
    }
}
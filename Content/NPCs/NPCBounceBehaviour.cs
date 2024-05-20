using System;
using Terramon.Core.NPCComponents;
using Terraria.ID;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> for adding bouncing AI to an NPC.
/// </summary>
public class NPCBounceBehaviour : NPCComponent
{
    public float BounceFrequency = 50f;
    public float BounceMaxRange = -5f;
    public float BounceMinRange = -7f;
    public float ChangeDirectionChance = 1f;
    public float FallSpeedMultiplier = 1f;
    public int FrameCount = 2;
    public int FrameTime = 10;
    private bool hasFirstDir;
    public float HorizontalSpeedMax = 3.5f;
    public float HorizontalSpeedMin = 2f;
    public float JumpSpeedMultiplier = 1f;
    public int MaxJumpClearance = -1;
    private NPC NPC;

    private ref float AI_State => ref NPC.ai[0];
    private ref float AI_Timer => ref NPC.ai[1];
    private ref float AI_JumpVelocity => ref NPC.ai[2];
    private ref float AI_JumpDirection => ref NPC.ai[3];

    protected override void OnEnabled(NPC npc)
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

        if ((int)AI_Timer == (int)(BounceFrequency / 2) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Main.rand.NextFloat() <= ChangeDirectionChance || !hasFirstDir)
            {
                AI_JumpDirection = Main.rand.NextBool() ? 1 : -1;
                if (!hasFirstDir) hasFirstDir = true;
            }

            if (MaxJumpClearance != -1)
            {
                if (Collision.SolidCollision(NPC.Left - new Vector2(8, MaxJumpClearance), 8, 8))
                    AI_JumpDirection = -1;
                else if (Collision.SolidCollision(NPC.Right + new Vector2(8, -MaxJumpClearance), 8, 8))
                    AI_JumpDirection = 1;
            }

            NPC.netUpdate = true;
        }

        NPC.spriteDirection = (int)AI_JumpDirection * -1;
        if (AI_Timer <= BounceFrequency) return;
        if (isGrounded) AI_State = (float)ActionState.Jump;
        AI_Timer = 0;
    }

    private void Jump()
    {
        AI_Timer++;
        if (AI_Timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            NPC.velocity.Y = Main.rand.NextFloat(BounceMinRange, BounceMaxRange);
            var jumpStrength = Main.rand.NextFloat(HorizontalSpeedMin, HorizontalSpeedMax);
            AI_JumpVelocity = AI_JumpDirection == 1 ? -jumpStrength : jumpStrength;
            NPC.netUpdate = true;
        }

        NPC.velocity.X = AI_JumpVelocity;
        if (NPC.velocity.Y > 0) NPC.velocity.Y *= FallSpeedMultiplier;
        else NPC.velocity.Y *= JumpSpeedMultiplier;
        if (NPC.velocity.Y != 0) return;
        AI_State = (float)ActionState.Idle;
        AI_Timer = 0;
    }

    /// <summary>
    ///     Determines the frame of the NPC based on its current state.
    /// </summary>
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled) return;

        NPC.frameCounter++;

        if (NPC.frameCounter < FrameTime * FrameCount)
            NPC.frame.Y = (int)Math.Floor(NPC.frameCounter / FrameTime) * frameHeight;
        else
            NPC.frameCounter = 0;
    }

    private enum ActionState
    {
        Idle,
        Jump
    }
}
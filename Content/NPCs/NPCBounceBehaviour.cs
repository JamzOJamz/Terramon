using System;
using Terramon.Core.NPCComponents;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> for adding bouncing AI to an NPC.
/// </summary>
public sealed class NPCBounceBehaviour : NPCAIComponent
{
    private bool _hasFirstDir;
    public float BounceFrequency = 50f;
    public float BounceMaxRange = -5f;
    public float BounceMinRange = -7f;
    public float ChangeDirectionChance = 1f;
    public float FallSpeedMultiplier = 1f;
    public float HorizontalSpeedMax = 3.5f;
    public float HorizontalSpeedMin = 2f;
    public float JumpSpeedMultiplier = 1f;
    public int MaxJumpClearance = -1;

    private ref float AIState => ref NPC.ai[0];
    private ref float AITimer => ref NPC.ai[1];
    private ref float AIJumpVelocity => ref NPC.ai[2];
    private ref float AIJumpDirection => ref NPC.ai[3];

    public override void AI(NPC npc)
    {
        if (!Enabled || PlasmaState) return;

        if (NPC.collideY) NPC.velocity.Y = 0;
        switch (AIState)
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
            AITimer++;
        }
        else
        {
            AITimer = 0;
        }

        if ((int)AITimer == (int)(BounceFrequency / 2))
        {
            if (Random.NextFloat() <= ChangeDirectionChance || !_hasFirstDir)
            {
                AIJumpDirection = Random.NextBool().ToDirectionInt();
                if (!_hasFirstDir) _hasFirstDir = true;
            }

            if (MaxJumpClearance != -1)
            {
                if (Collision.SolidCollision(NPC.Left - new Vector2(8, MaxJumpClearance), 8, 8))
                    AIJumpDirection = -1;
                else if (Collision.SolidCollision(NPC.Right + new Vector2(8, -MaxJumpClearance), 8, 8))
                    AIJumpDirection = 1;
            }

            //NPC.netUpdate = true;
        }

        NPC.spriteDirection = (int)AIJumpDirection * -1;
        if (AITimer <= BounceFrequency) return;
        if (isGrounded) AIState = (float)ActionState.Jump;
        AITimer = 0;
    }

    private void Jump()
    {
        AITimer++;
        if (AITimer == 1)
        {
            NPC.velocity.Y = Random.NextFloat(BounceMinRange, BounceMaxRange);
            var jumpStrength = Random.NextFloat(HorizontalSpeedMin, HorizontalSpeedMax);
            AIJumpVelocity = AIJumpDirection == 1 ? -jumpStrength : jumpStrength;
            //NPC.netUpdate = true;
        }
        
        if (MathF.Abs(NPC.velocity.X) < 0.1f || AITimer == 1) NPC.velocity.X = AIJumpVelocity;
        if (NPC.velocity.Y > 0) NPC.velocity.Y *= FallSpeedMultiplier;
        else NPC.velocity.Y *= JumpSpeedMultiplier;
        if (NPC.velocity.Y != 0) return;
        AIState = (float)ActionState.Idle;
        AITimer = 0;
    }

    /// <summary>
    ///     Determines the frame of the NPC based on its current state.
    /// </summary>
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled || PlasmaState) return;

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
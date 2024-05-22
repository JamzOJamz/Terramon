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
    private bool _hasFirstDir;
    public float HorizontalSpeedMax = 3.5f;
    public float HorizontalSpeedMin = 2f;
    public float JumpSpeedMultiplier = 1f;
    public int MaxJumpClearance = -1;
    private NPC _npc;

    private ref float AIState => ref _npc.ai[0];
    private ref float AITimer => ref _npc.ai[1];
    private ref float AIJumpVelocity => ref _npc.ai[2];
    private ref float AIJumpDirection => ref _npc.ai[3];

    protected override void OnEnabled(NPC npc)
    {
        _npc = npc;
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

        if (_npc.collideY) _npc.velocity.Y = 0;
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
        var isGrounded = _npc.velocity.Y == 0;
        if (isGrounded)
        {
            _npc.velocity.X *= 0.7f;
            AITimer++;
        }
        else
        {
            AITimer = 0;
        }

        if ((int)AITimer == (int)(BounceFrequency / 2) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Main.rand.NextFloat() <= ChangeDirectionChance || !_hasFirstDir)
            {
                AIJumpDirection = Main.rand.NextBool() ? 1 : -1;
                if (!_hasFirstDir) _hasFirstDir = true;
            }

            if (MaxJumpClearance != -1)
            {
                if (Collision.SolidCollision(_npc.Left - new Vector2(8, MaxJumpClearance), 8, 8))
                    AIJumpDirection = -1;
                else if (Collision.SolidCollision(_npc.Right + new Vector2(8, -MaxJumpClearance), 8, 8))
                    AIJumpDirection = 1;
            }

            _npc.netUpdate = true;
        }

        _npc.spriteDirection = (int)AIJumpDirection * -1;
        if (AITimer <= BounceFrequency) return;
        if (isGrounded) AIState = (float)ActionState.Jump;
        AITimer = 0;
    }

    private void Jump()
    {
        AITimer++;
        if (AITimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _npc.velocity.Y = Main.rand.NextFloat(BounceMinRange, BounceMaxRange);
            var jumpStrength = Main.rand.NextFloat(HorizontalSpeedMin, HorizontalSpeedMax);
            AIJumpVelocity = AIJumpDirection == 1 ? -jumpStrength : jumpStrength;
            _npc.netUpdate = true;
        }

        _npc.velocity.X = AIJumpVelocity;
        if (_npc.velocity.Y > 0) _npc.velocity.Y *= FallSpeedMultiplier;
        else _npc.velocity.Y *= JumpSpeedMultiplier;
        if (_npc.velocity.Y != 0) return;
        AIState = (float)ActionState.Idle;
        AITimer = 0;
    }

    /// <summary>
    ///     Determines the frame of the NPC based on its current state.
    /// </summary>
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled) return;

        _npc.frameCounter++;

        if (_npc.frameCounter < FrameTime * FrameCount)
            _npc.frame.Y = (int)Math.Floor(_npc.frameCounter / FrameTime) * frameHeight;
        else
            _npc.frameCounter = 0;
    }

    private enum ActionState
    {
        Idle,
        Jump
    }
}
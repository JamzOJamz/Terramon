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
    public string AnimationType = "StraightForward";
    private int _collideTimer;
    public int FrameCount = 2;
    public int FrameTime = 10;
    public bool IsClassic = true; //TODO: remove once all classic pokemon sprites are replaced with custom ones
    private NPC _npc;
    public int StopFrequency = 200;
    public float WalkSpeed = 1f;

    private ref float AIState => ref _npc.ai[0];
    private ref float AITimer => ref _npc.ai[1];
    private ref float AIWalkDir => ref _npc.ai[2];

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

        // Smooth walking over slopes
        Collision.StepUp(ref _npc.position, ref _npc.velocity, _npc.width, _npc.height, ref _npc.stepSpeed, ref _npc.gfxOffY);

        switch (AIState)
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
        if (_npc.velocity.Y == 0)
        {
            _npc.velocity.X *= 0.85f;
            AITimer++;
        }

        if (_npc.velocity.X != 0)
            _npc.spriteDirection = _npc.velocity.X > 0 ? 1 : -1;

        if (AITimer != 120) return;
        AIState = (float)ActionState.Walking;
        AITimer = 0;
    }

    private void Walking()
    {
        AITimer++;
        if (Main.netMode != NetmodeID.MultiplayerClient)
            switch (AITimer)
            {
                case 1:
                    AIWalkDir = Main.rand.NextBool() ? 1 : -1;
                    _npc.netUpdate = true;
                    break;
                case >= 120 when Main.rand.NextBool(StopFrequency):
                    AIState = (float)ActionState.Idle;
                    AITimer = 0;
                    _npc.netUpdate = true;
                    return;
            }

        if (_npc.collideX)
        {
            if (_collideTimer < 10)
            {
                _npc.velocity.Y = -2f;
            }
            else if (_npc.velocity.Y == 0)
            {
                AIWalkDir *= -1;
                _npc.netUpdate = true;
            }

            _collideTimer++;
        }
        else
        {
            _collideTimer = 0;
        }

        // Define constants
        const float acceleration = 0.05f; // Acceleration rate
        const float deceleration = 0.2f; // Deceleration rate

        // Update velocity based on AI behavior (this part may be called each frame)
        if (AIWalkDir != 0)
        {
            // Accelerate towards the desired direction
            _npc.velocity.X += AIWalkDir * acceleration;

            // Clamp velocity to maximum speed
            _npc.velocity.X = Math.Clamp(_npc.velocity.X, -WalkSpeed, WalkSpeed);
        }
        else
        {
            switch (_npc.velocity.X)
            {
                // Decelerate when no input is given
                case > 0:
                    _npc.velocity.X -= deceleration;
                    break;
                case < 0:
                    _npc.velocity.X += deceleration;
                    break;
            }

            // Ensure velocity doesn't change direction when slowing down
            if (Math.Abs(_npc.velocity.X) < deceleration)
                _npc.velocity.X = 0;
        }

        _npc.spriteDirection = AIWalkDir == 1 ? 1 : -1;
    }

    /// <summary>
    ///     Determines the frame of the NPC based on its current state.
    /// </summary>
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled) return;

        if (AIState == (float)ActionState.Idle && !_npc.IsABestiaryIconDummy)
        {
            _npc.frameCounter = IsClassic ? FrameTime : 0;
            _npc.frame.Y = IsClassic ? frameHeight : 0;
            return;
        }

        _npc.frameCounter++;

        switch (AnimationType)
        {
            case "StraightForward": // Animates all frames in a sequential order
                if (_npc.frameCounter < FrameTime * FrameCount)
                    _npc.frame.Y = (int)Math.Floor(_npc.frameCounter / FrameTime) * frameHeight;
                else
                    _npc.frameCounter = 0;
                break;
            case "IdleForward": // Same as StraightForward, but skips the first frame (which is idle only)
                if (_npc.frameCounter < FrameTime * FrameCount)
                    _npc.frame.Y = (int)Math.Floor(_npc.frameCounter / FrameTime) * frameHeight;
                else
                    _npc.frameCounter = FrameTime;
                break;
            case "Alternate": // Alternates between frame sequences
                var cycleLength = FrameCount + 1;
                var alternateFrame = (int)(_npc.frameCounter / FrameTime) % cycleLength;
                _npc.frame.Y = cycleLength switch
                {
                    4 => alternateFrame switch
                    {
                        0 or 2 => 0 * frameHeight,
                        1 => 1 * frameHeight,
                        3 => 2 * frameHeight,
                        _ => _npc.frame.Y
                    },
                    6 => alternateFrame switch
                    {
                        0 or 3 => 0 * frameHeight,
                        1 => 1 * frameHeight,
                        2 => 2 * frameHeight,
                        4 => 3 * frameHeight,
                        5 => 4 * frameHeight,
                        _ => _npc.frame.Y
                    },
                    _ => _npc.frame.Y
                };
                break;
            default:
                _npc.frame.Y = 0;
                break;
        }
    }

    private enum ActionState
    {
        Idle,
        Walking
    }
}
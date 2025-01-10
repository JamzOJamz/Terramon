using Terramon.Core.NPCComponents;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> for adding basic walking AI to an NPC.
/// </summary>
public sealed class NPCWalkingBehaviour : NPCAIComponent
{
    private int _collideTimer;
    public AnimType AnimationType = AnimType.StraightForward;
    public bool IsClassic = true; //TODO: remove once all classic pokemon sprites are replaced with custom ones
    public int StopFrequency = 225;
    public float WalkSpeed = 1f;

    private ref float AIState => ref NPC.ai[0];
    private ref float AITimer => ref NPC.ai[1];
    private ref float AIWalkDir => ref NPC.ai[2];

    public override void AI(NPC npc)
    {
        if (!Enabled || PlasmaState) return;
        
        // Smooth walking over slopes
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
            ref NPC.gfxOffY);

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
        if (NPC.velocity.Y == 0)
        {
            NPC.velocity.X *= 0.85f;
            AITimer++;
        }

        if (NPC.velocity.X != 0)
            NPC.spriteDirection = (NPC.velocity.X > 0).ToDirectionInt();

        if (AITimer != 120) return;
        AIState = (float)ActionState.Walking;
        AITimer = 0;
    }

    private void Walking()
    {
        AITimer++;
        switch (AITimer)
        {
            case 1:
                AIWalkDir = Random.NextBool().ToDirectionInt();
                break;
            case >= 120 when Random.Next(StopFrequency) == 0:
                AIState = (float)ActionState.Idle;
                AITimer = 0;
                return;
        }

        if (NPC.collideX)
        {
            if (_collideTimer < 10)
                NPC.velocity.Y = -2f;
            else if (NPC.velocity.Y == 0)
            {
                AIWalkDir *= -1;
                _collideTimer = 0;
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
            NPC.velocity.X += AIWalkDir * acceleration;

            // Clamp velocity to maximum speed
            NPC.velocity.X = Math.Clamp(NPC.velocity.X, -WalkSpeed, WalkSpeed);
        }
        else
        {
            switch (NPC.velocity.X)
            {
                // Decelerate when no input is given
                case > 0:
                    NPC.velocity.X -= deceleration;
                    break;
                case < 0:
                    NPC.velocity.X += deceleration;
                    break;
            }

            // Ensure velocity doesn't change direction when slowing down
            if (Math.Abs(NPC.velocity.X) < deceleration)
                NPC.velocity.X = 0;
        }

        NPC.spriteDirection = (int)AIWalkDir;
    }

    /// <summary>
    ///     Determines the frame of the NPC based on its current state.
    /// </summary>
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled || PlasmaState) return;

        if (AIState == (float)ActionState.Idle && !NPC.IsABestiaryIconDummy)
        {
            NPC.frameCounter = IsClassic ? FrameTime : 0;
            NPC.frame.Y = IsClassic ? frameHeight : 0;
            return;
        }

        NPC.frameCounter++;

        switch (AnimationType)
        {
            case AnimType.StraightForward: // Animates all frames in a sequential order
                if (NPC.frameCounter < FrameTime * FrameCount)
                    NPC.frame.Y = (int)Math.Floor(NPC.frameCounter / FrameTime) * frameHeight;
                else
                    NPC.frameCounter = 0;
                break;
            case AnimType.IdleForward: // Same as StraightForward, but skips the first frame (which is idle only)
                if (NPC.frameCounter < FrameTime * (FrameCount - 1))
                    NPC.frame.Y = ((int)Math.Floor(NPC.frameCounter / FrameTime) + 1) * frameHeight;
                else
                    NPC.frameCounter = 0;
                break;
            case AnimType.Alternate: // Alternates between frame sequences
                var cycleLength = FrameCount + 1;
                var alternateFrame = (int)(NPC.frameCounter / FrameTime) % cycleLength;
                NPC.frame.Y = cycleLength switch
                {
                    4 => alternateFrame switch
                    {
                        0 or 2 => 0 * frameHeight,
                        1 => 1 * frameHeight,
                        3 => 2 * frameHeight,
                        _ => NPC.frame.Y
                    },
                    6 => alternateFrame switch
                    {
                        0 or 3 => 0 * frameHeight,
                        1 => 1 * frameHeight,
                        2 => 2 * frameHeight,
                        4 => 3 * frameHeight,
                        5 => 4 * frameHeight,
                        _ => NPC.frame.Y
                    },
                    _ => NPC.frame.Y
                };
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
    public enum AnimType : byte
    {
        None,
        StraightForward,
        IdleForward,
        Alternate
    }
}
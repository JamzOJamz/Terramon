using System;
using System.IO;
using Terraria.ID;

namespace Terramon.Content.AI;

public class WanderingHoverAI : AIController
{
    private float endTime;

    private float moveSpeed = 0.5f;
    private float startTime;

    public WanderingHoverAI(NPC npc) : base(npc)
    {
    }

    private ref float AI_State => ref NPC.ai[0];
    private ref float AI_Timer => ref NPC.ai[1];
    private ref float AI_MoveDirectionX => ref NPC.ai[2];
    private ref float AI_MoveDirectionY => ref NPC.ai[3];

    public override void AI()
    {
        switch (AI_State)
        {
            case (float)ActionState.Hover:
                Hover();
                break;
        }
    }

    private void Hover()
    {
        AI_Timer++;
        if (AI_Timer == 1 || AI_Timer % 260 == 0)
        {
            startTime = AI_Timer;
            endTime = AI_Timer + 260;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var tileCoords = NPC.position.ToTileCoordinates();
                tileCoords.Y += 12;
                var tile = Main.tile[tileCoords.X, tileCoords.Y];
                var tileSolid = tile.HasTile && Main.tileSolid[tile.TileType];
                var angle = tileSolid
                    ? Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi)
                    : Main.rand.NextFloat(0f, MathHelper.Pi);
                var direction =
                    new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                AI_MoveDirectionX = direction.X;
                AI_MoveDirectionY = direction.Y;
                moveSpeed = Main.rand.NextFloat(0.5f, 1.25f) / (tileSolid ? 1f : 1.5f);
                NPC.netUpdate = true;
            }
        }

        var useVelX = AI_Timer < endTime
            ? MathHelper.Lerp(NPC.velocity.X, AI_MoveDirectionX * moveSpeed,
                (AI_Timer - startTime) / (endTime - startTime))
            : AI_MoveDirectionX * moveSpeed;
        var useVelY = AI_Timer < endTime
            ? MathHelper.Lerp(NPC.velocity.Y, AI_MoveDirectionY * moveSpeed,
                (AI_Timer - startTime) / (endTime - startTime))
            : AI_MoveDirectionY * moveSpeed;
        NPC.velocity = new Vector2(useVelX, useVelY);
        NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(moveSpeed);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        moveSpeed = reader.ReadSingle();
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        switch (NPC.frameCounter)
        {
            case < FrameSpeed:
                NPC.frame.Y = (int)Frame.One * frameHeight;
                break;
            case < FrameSpeed * 2:
                NPC.frame.Y = (int)Frame.Two * frameHeight;
                break;
            default:
                NPC.frameCounter = 0;
                break;
        }
    }

    private enum ActionState
    {
        Hover
    }

    private enum Frame
    {
        One,
        Two
    }
}
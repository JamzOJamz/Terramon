using System;
using System.IO;
using Terraria.ID;

namespace Terramon.Content.AI;

public class WanderingHoverAi(NPC npc) : AIController(npc)
{
    private float _endTime;

    private float _moveSpeed = 0.5f;
    private float _startTime;

    private ref float AIState => ref NPC.ai[0];
    private ref float AITimer => ref NPC.ai[1];
    private ref float AIMoveDirectionX => ref NPC.ai[2];
    private ref float AIMoveDirectionY => ref NPC.ai[3];

    public override void AI()
    {
        switch (AIState)
        {
            case (float)ActionState.Hover:
                Hover();
                break;
        }
    }

    private void Hover()
    {
        AITimer++;
        if (AITimer == 1 || AITimer % 260 == 0)
        {
            _startTime = AITimer;
            _endTime = AITimer + 260;
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
                AIMoveDirectionX = direction.X;
                AIMoveDirectionY = direction.Y;
                _moveSpeed = Main.rand.NextFloat(0.5f, 1.25f) / (tileSolid ? 1f : 1.5f);
                NPC.netUpdate = true;
            }
        }

        var useVelX = AITimer < _endTime
            ? MathHelper.Lerp(NPC.velocity.X, AIMoveDirectionX * _moveSpeed,
                (AITimer - _startTime) / (_endTime - _startTime))
            : AIMoveDirectionX * _moveSpeed;
        var useVelY = AITimer < _endTime
            ? MathHelper.Lerp(NPC.velocity.Y, AIMoveDirectionY * _moveSpeed,
                (AITimer - _startTime) / (_endTime - _startTime))
            : AIMoveDirectionY * _moveSpeed;
        NPC.velocity = new Vector2(useVelX, useVelY);
        NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(_moveSpeed);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        _moveSpeed = reader.ReadSingle();
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
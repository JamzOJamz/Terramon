using System;
using System.IO;
using Terramon.Core.NPCComponents;
using Terraria.ModLoader.IO;

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> for adding bouncing AI to an NPC.
/// </summary>
public sealed class NPCWanderingHoverBehaviour : NPCAIComponent
{
    private float _endTime;
    private float _moveSpeed = 0.5f;
    private float _startTime;
    
    private ref float AIState => ref NPC.ai[0];
    private ref float AITimer => ref NPC.ai[1];
    private ref float AIMoveDirectionX => ref NPC.ai[2];
    private ref float AIMoveDirectionY => ref NPC.ai[3];

    public override void SetDefaults(NPC npc)
    {
        base.SetDefaults(npc);
        if (!Enabled) return;
        
        npc.noGravity = true;
    }

    public override void AI(NPC npc)
    {
        if (!Enabled || PlasmaState) return;
        
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
                    ? Random.NextFloat(MathHelper.Pi, MathHelper.TwoPi)
                    : Random.NextFloat(0f, MathHelper.Pi);
                var direction =
                    new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                AIMoveDirectionX = direction.X;
                AIMoveDirectionY = direction.Y;
                _moveSpeed = Random.NextFloat(0.5f, 1.25f) / (tileSolid ? 1f : 1.5f);
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
        NPC.spriteDirection = (NPC.velocity.X > 0).ToDirectionInt();
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        if (!Enabled) return;
        
        base.SendExtraAI(npc, bitWriter, binaryWriter);
        binaryWriter.Write(_moveSpeed);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        if (!Enabled) return;
        
        base.ReceiveExtraAI(npc, bitReader, binaryReader);
        _moveSpeed = binaryReader.ReadSingle();
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!Enabled || PlasmaState) return;
        
        NPC.frameCounter++;
        if (NPC.frameCounter < FrameTime * FrameCount)
            NPC.frame.Y = (int)Math.Floor(NPC.frameCounter / FrameTime) * frameHeight;
        else
            NPC.frameCounter = 0;
    }
}
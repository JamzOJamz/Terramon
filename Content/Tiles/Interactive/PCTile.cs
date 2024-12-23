using EasyPacketsLib;
using ReLogic.Content;
using Terramon.Content.Commands;
using Terramon.Content.Packets;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Interactive;

public abstract class PCTile : ModTile
{
    private static Asset<Texture2D> _screenGlowTexture;

    static PCTile()
    {
        // Catch the event when the player toggles their inventory
        On_Player.ToggleInv += static (orig, self) =>
        {
            var otherPcId = TerramonPlayer.LocalPlayer.ActivePCTileEntityID;
            if (TerramonPlayer.LocalPlayer.ActivePCTileEntityID != -1 &&
                TileEntity.ByID.TryGetValue(otherPcId, out var entity) && entity is PCTileEntity
                {
                    PoweredOn: true
                } pc)
                pc.ToggleOnOff();

            orig(self);
        };
    }

    public override string Texture => "Terramon/Assets/Tiles/Interactive/" + GetType().Name;

    public override string HighlightTexture => "Terramon/Assets/Tiles/Interactive/PCTile_Highlight";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;
        Main.tileLighted[Type] = true;
        TileID.Sets.HasOutlines[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
        TileObjectData.newTile.Origin = new Point16(0, 2);
        TileObjectData.newTile.HookPostPlaceMyPlayer =
            new PlacementHook(ModContent.GetInstance<PCTileEntity>().Hook_AfterPlacement, -1, 0, true);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.addTile(Type);

        AddMapEntry(Color.White, CreateMapEntryName());
    }

    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
    {
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
    }

    public override bool RightClick(int i, int j)
    {
        if (!TileUtils.TryGetTileEntityAs(i, j, out PCTileEntity te) ||
            (te.PoweredOn && te.User != Main.myPlayer)) return false;
        
        // Starter Pokémon should be chosen before using the PC
        var player = Main.LocalPlayer;
        var modPlayer = player.GetModPlayer<TerramonPlayer>();
        if (!modPlayer.HasChosenStarter)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"), TerramonCommand.ChatColorYellow);
            return false;
        }

        // If already using a PC, turn off that one first
        var otherPcId = modPlayer.ActivePCTileEntityID;
        var differentPc = false;
        if (otherPcId != -1 && otherPcId != te.ID && TileEntity.ByID.TryGetValue(otherPcId, out var otherTe) &&
            otherTe is PCTileEntity { PoweredOn: true } otherPcTe)
        {
            otherPcTe.ToggleOnOff();
            differentPc = true;
        }

        // Toggle this PC on/off
        te.ToggleOnOff();

        // Play the appropriate sound for the action
        if (differentPc)
            SoundEngine.PlaySound(SoundID.MenuTick);
        else
            SoundEngine.PlaySound(
                new SoundStyle(te.PoweredOn ? "Terramon/Sounds/ls_pc_on" : "Terramon/Sounds/ls_pc_off")
                {
                    Volume = 0.54f
                });
        if (!te.PoweredOn)
        {
            SoundEngine.PlaySound(SoundID.MenuClose); // Could maybe be removed but it fits well
        }
        else
        {
            // Some code for opening a UI cleanly after right clicking the PC
            Main.mouseRightRelease = false;

            if (player.sign > -1)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                player.sign = -1;
                Main.editSign = false;
                Main.npcChatText = string.Empty;
            }

            if (Main.editChest)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = string.Empty;
            }

            if (player.editedChestName)
            {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1,
                    NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
                player.editedChestName = false;
            }

            if (player.talkNPC > -1)
            {
                player.SetTalkNPC(-1);
                Main.npcChatCornerItem = 0;
                Main.npcChatText = string.Empty;
            }
        }

        // Open the player inventory
        if (te.PoweredOn)
            Main.playerInventory = true;

        return true;
    }

    public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
    {
        var drawingTile = Main.tile[i, j];
        var leftX = i - drawingTile.TileFrameX / 18 % 2;
        var topY = j - drawingTile.TileFrameY / 18 % 3;
        var rightX = leftX + 1;
        var bottomY = topY + 2;
        if (rightX != i || bottomY != j || !TileUtils.TryGetTileEntityAs(i, j, out PCTileEntity te) ||
            !te.PoweredOn) return;
        var zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
        if (Main.drawToScreen) zero = Vector2.Zero;
        var drawPos = new Vector2(leftX * 16 - (int)Main.screenPosition.X + 6,
            topY * 16 - (int)Main.screenPosition.Y + 10) + zero;
        spriteBatch.Draw(_screenGlowTexture.Value, drawPos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None,
            0f);
    }

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        if (!TileUtils.TryGetTileEntityAs(i, j, out PCTileEntity te) || !te.PoweredOn) return;
        var tile = Main.tile[i, j];
        var topY = j - tile.TileFrameY / 18 % 3;
        if (j - topY == 2) return; // Bottom tile row doesn't emit light
        r = 140 / 255f * 0.7f;
        g = 193 / 255f * 0.7f;
        b = 236 / 255f * 0.7f;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        ModContent.GetInstance<PCTileEntity>().Kill(i, j);
    }

    public override void Load()
    {
        if (_screenGlowTexture == null && !Main.dedServ)
            _screenGlowTexture = ModContent.Request<Texture2D>("Terramon/Assets/Tiles/Interactive/PCTile_Glow");
    }

    public override void Unload()
    {
        _screenGlowTexture = null;
    }
}

public sealed class PCTileEntity : ModTileEntity
{
    public bool PoweredOn;
    public int User = -1;

    public void ToggleOnOff()
    {
        var player = Main.LocalPlayer; // This method is only ever called on the local client, so this is safe
        PoweredOn = !PoweredOn;
        User = PoweredOn ? player.whoAmI : -1;
        player.GetModPlayer<TerramonPlayer>().ActivePCTileEntityID = PoweredOn ? ID : -1;

        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        // Assume we are in multiplayer as a client here
        Mod.SendPacket(new ControlPCTileRpc(ID, PoweredOn), -1, Main.myPlayer, true);
    }

    public override void Update()
    {
        switch (Main.netMode)
        {
            case NetmodeID.SinglePlayer when Main.myPlayer == User:
            {
                // Check if the player is still nearby
                var player = Main.player[User];
                var pos = player.position;
                if (player.active && !(Vector2.Distance(Position.ToWorldCoordinates(8, 0), pos) > 94f)) return;
                PoweredOn = false;
                User = -1;
                player.GetModPlayer<TerramonPlayer>().ActivePCTileEntityID = -1;
                SoundEngine.PlaySound(SoundID.MenuClose);
                break;
            }
            case NetmodeID.Server when User != -1:
            {
                // Check if the player is still nearby (server-side)
                var player = Main.player[User];
                var pos = player.position;
                if (player.active && !(Vector2.Distance(Position.ToWorldCoordinates(8, 0), pos) > 94f)) return;
                PoweredOn = false;
                User = -1;
                player.GetModPlayer<TerramonPlayer>().ActivePCTileEntityID = -1;
                // Send a packet to all clients to turn off the PC
                Mod.SendPacket(new ControlPCTileRpc(ID, false), -1, Main.myPlayer);
                break;
            }
        }
    }

    public override bool IsTileValidForEntity(int i, int j)
    {
        var tile = Framing.GetTileSafely(i, j);
        return tile.HasTile && ModContent.GetModTile(tile.TileType) is PCTile;
    }

    // ReSharper disable once ParameterHidesMember
    public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return Place(i, j);
        NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 3);
        NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
        return -1;
    }
}
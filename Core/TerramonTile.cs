using Terraria.ID;

namespace Terramon.Core;

public class TerramonTile : GlobalTile
{
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || type != TileID.Trees || fail || noItem)
            return; // Only run if not a multiplayer client, tile is destroyed, should drop an item, and is a tree.
        //var tile = Main.tile[i, j];
        Main.NewText($"Tile at {i}, {j} with type {type} was killed.");
    }
}
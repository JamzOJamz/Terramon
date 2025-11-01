using MonoMod.Cil;
using Terramon.Content.Items;
using Terraria.Enums;
using Terraria.Utilities;

namespace Terramon.Core.Systems;

public class TreeDropsSystem : ModSystem
{
    public override void PostSetupContent()
    {
        TreeDropsGlobalTile.ApricornItems =
            ModContent.GetContent<ApricornItem>().Where(item => !TerramonItemAPI.Sets.Unobtainable.Contains(item.Type))
                .ToArray();
    }
}

public class TreeDropsGlobalTile : GlobalTile
{
    private static bool _vanillaTreeShakeFailed;

    /// <summary>
    ///     The denominator (1/x) for the probability of apricorns falling from a tree when it is shaken.
    ///     Constant value of 8, meaning a 1/8 or approximately 12.5% chance.
    /// </summary>
    /*private const int
        ApricornDropChanceDenominator = 8; // TODO: Make this configurable through a gameplay config option*/

    public static ApricornItem[] ApricornItems { private get; set; }

    public override void Load()
    {
        On_WorldGen.ShakeTree += WorldGenShakeTree_Detour;
        IL_WorldGen.ShakeTree += HookShakeTree;
    }

    public override void Unload()
    {
        ApricornItems = null;
    }

    /// <summary>
    ///     Handles dropping assorted apricorns from trees when they are cut down.
    /// </summary>
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || WorldGen.noTileActions || WorldGen.gen ||
            type != TileID.Trees || fail || noItem)
            return;

        WorldGen.GetTreeBottom(i, j, out var x, out var y);
        var treeType = WorldGen.GetTreeType(Main.tile[x, y].TileType);
        if (treeType is not (TreeTypes.Forest or TreeTypes.Snow or TreeTypes.Hallowed)) return;

        var shouldDrop = WorldGen.genRand.NextBool(5);
        if (!shouldDrop) return;

        var randomApricorn = ApricornItems[WorldGen.genRand.Next(ApricornItems.Length)].Type;
        Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 32, 32,
            randomApricorn,
            randomApricorn == ModContent.ItemType<RedApricorn>() && WorldGen.genRand.NextBool(6, 10)
                ? 2
                : 1); // TODO: Red Apricorns should be more common until Apricorn Trees are implemented
    }

    private static void HookShakeTree(ILContext il)
    {
        try
        {
            // Start the Cursor at the start
            var c = new ILCursor(il);

            // Try to find where massive if else chain ends to inject code
            c.GotoNext(i => i.MatchLdcI4(12), i => i.MatchCallvirt(typeof(UnifiedRandom), "Next"),
                i => i.MatchBrtrue(out _), i => i.MatchLdloc3(), i => i.MatchLdcI4(12));

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the result of the random number before the delegate consumes it
            c.EmitDup();

            // Check result of the random number
            c.EmitDelegate<Action<int>>(result =>
            {
                if (result == 0) return;
                _vanillaTreeShakeFailed = true;
            });

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the tree type before the delegate consumes it
            c.EmitDup();

            // Check tree type
            c.EmitDelegate<Action<TreeTypes>>(type =>
            {
                if (type == TreeTypes.Ash) return;
                _vanillaTreeShakeFailed = true;
            });
        }
        catch
        {
            MonoModHooks.DumpIL(Terramon.Instance, il);
        }
    }

    private static void WorldGenShakeTree_Detour(On_WorldGen.orig_ShakeTree orig, int i, int j)
    {
        if (ApricornItems.Length == 0)
        {
            orig(i, j);
            return;
        }

        WorldGen.GetTreeBottom(i, j, out var x, out var y);
        var numTreeShakes = WorldGen.numTreeShakes;
        var treeShakeX = WorldGen.treeShakeX;
        var treeShakeY = WorldGen.treeShakeY;
        for (var k = 0; k < numTreeShakes; k++)
        {
            if (treeShakeX[k] != x || treeShakeY[k] != y) continue;
            orig(i, j);
            return;
        }

        _vanillaTreeShakeFailed = false;
        orig(i, j); // Call the original method to shake the tree
        if (!_vanillaTreeShakeFailed) return;

        var treeType = WorldGen.GetTreeType(Main.tile[x, y].TileType);
        if (!WorldGen.genRand.NextBool(5) ||
            treeType is not (TreeTypes.Forest or TreeTypes.Snow or TreeTypes.Hallowed)) return;
        y--;
        while (y > 10 && Main.tile[x, y].HasTile && TileID.Sets.IsShakeable[Main.tile[x, y].TileType]) y--;
        y++;
        if (!WorldGen.IsTileALeafyTreeTop(x, y) || Collision.SolidTiles(x - 2, x + 2, y - 2, y + 2))
            return;
        var randomApricorn = ApricornItems[WorldGen.genRand.Next(ApricornItems.Length)].Type;
        Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Rectangle(x * 16, y * 16, 16, 16),
            randomApricorn, WorldGen.genRand.Next(1, 3));
    }
}
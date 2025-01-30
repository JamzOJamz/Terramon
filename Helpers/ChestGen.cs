// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace Terramon.Helpers;

/// <summary>
///     Provides a set of methods for adding and removing loot from chests.
/// </summary>
internal static class ChestGen
{
    /// <summary>
    ///     Adds loot to chests based on the given parameters.
    /// </summary>
    /// <param name="itemID">The item ID to add.</param>
    /// <param name="chestID">The chest ID to target (-1 for all chests).</param>
    /// <param name="minimumStack">Minimum stack size to add.</param>
    /// <param name="maximumStack">Maximum stack size to add.</param>
    /// <param name="chance">Probability to add per chest (0-1, where 1=100%).</param>
    /// <param name="excludeDuplicates">Skip chests already containing this item.</param>
    public static void AddChestLoot(int itemID, int chestID = -1, int minimumStack = 1, int maximumStack = 1,
        float chance = 1f, bool excludeDuplicates = false)
    {
        AddChestLoot(itemID,
            chestID == -1 ? static _ => true : chest => Main.tile[chest.x, chest.y].TileFrameX / 36 == chestID, minimumStack,
            maximumStack, chance,
            excludeDuplicates);
    }

    /// <summary>
    ///     Adds loot to chests based on the given parameters.
    /// </summary>
    /// <param name="itemID">The item ID to add.</param>
    /// <param name="chestIDs">The chest IDs to target (null for all chests).</param>
    /// <param name="minimumStack">Minimum stack size to add.</param>
    /// <param name="maximumStack">Maximum stack size to add.</param>
    /// <param name="chance">Probability to add per chest (0-1, where 1=100%).</param>
    /// <param name="excludeDuplicates">Skip chests already containing this item.</param>
    public static void AddChestLoot(int itemID, List<int> chestIDs, int minimumStack = 1, int maximumStack = 1,
        float chance = 1f, bool excludeDuplicates = false)
    {
        AddChestLoot(itemID, Predicate, minimumStack, maximumStack, chance, excludeDuplicates);
        return;

        bool Predicate(Chest chest)
        {
            return chestIDs == null || chestIDs.Contains(Main.tile[chest.x, chest.y].TileFrameX / 36);
        }
    }

    /// <summary>
    ///     Adds loot to chests based on the given parameters, using a predicate to determine target chests.
    /// </summary>
    /// <param name="itemID">The item ID to add.</param>
    /// <param name="chestPredicate">A function that returns true if the chest should be targeted.</param>
    /// <param name="minimumStack">Minimum stack size to add.</param>
    /// <param name="maximumStack">Maximum stack size to add.</param>
    /// <param name="chance">Probability to add per chest (0-1, where 1=100%).</param>
    /// <param name="excludeDuplicates">Skip chests already containing this item.</param>
    public static void AddChestLoot(int itemID, Func<Chest, bool> chestPredicate, int minimumStack = 1,
        int maximumStack = 1,
        float chance = 1f, bool excludeDuplicates = false)
    {
        if (maximumStack < minimumStack)
            maximumStack = minimumStack;
        if (chance is < 0f or > 1f)
            chance = 1f;

        for (var chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
        {
            // Skip if chance check fails
            if (WorldGen.genRand.NextFloat() > chance)
                continue;

            // Get the current chest and skip if it doesn't exist or is not a container
            var chest = Main.chest[chestIndex];
            if (chest == null || Main.tile[chest.x, chest.y].TileType != TileID.Containers)
                continue;

            // Skip if the chest does not match the predicate condition
            if (!chestPredicate(chest))
                continue;

            for (var inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
            {
                // Skip chest if item already exists and duplicates are excluded
                if (chest.item[inventoryIndex].type == itemID && excludeDuplicates)
                    break;

                // Skip non-matching and non-air slots
                if (!chest.item[inventoryIndex].IsAir && chest.item[inventoryIndex].type != itemID)
                    continue;

                // Determine stack size and add the item
                var amount = WorldGen.genRand.Next(minimumStack, maximumStack + 1);
                if (amount == 0)
                    break;

                if (chest.item[inventoryIndex].IsAir)
                {
                    chest.item[inventoryIndex].SetDefaults(itemID);
                    chest.item[inventoryIndex].stack = amount;
                }
                else
                {
                    chest.item[inventoryIndex].stack += amount;
                }

                break;
            }
        }
    }

    /*public static void RemoveChestLoot(int itemID, int chestID = -1, int minimumAmount = 1, int maximumAmount = 1,
        float chance = 1f)
    {
        RemoveChestLoot(itemID, chestID == -1 ? null : [chestID], minimumAmount, maximumAmount,
            chance);
    }

    public static void RemoveChestLoot(int itemID, List<int> chestIDs, int minimumAmount = 1, int maximumAmount = 1,
        float chance = 1f)
    {
        if (maximumAmount < minimumAmount)
            maximumAmount = minimumAmount;
        if (chance is < 0f or > 1f)
            chance = 1f;

        for (var chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
        {
            //if the chance for this chest is unsuccessful, skip this chest
            if (WorldGen.genRand.Next(0, 10000) <= chance)
                continue;

            var chest = Main.chest[chestIndex];
            //check if the chest actually exists
            if (chest != null && Main.tile[chest.x, chest.y].TileType == TileID.Containers)
                //make sure the chest is the right type that we are looking for
                if (chestIDs == null || chestIDs.Contains(Main.tile[chest.x, chest.y].TileFrameX / 36))
                    //check all slots
                    for (var inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        //if slot contains our item
                        if (chest.item[inventoryIndex].type == itemID)
                        {
                            var amount = WorldGen.genRand.Next(minimumAmount, maximumAmount + 1);

                            //break if the amount is 0 (shouldn't try to remove an item)
                            if (amount == 0)
                                break;

                            //remove item if stack will be less than 1
                            if (chest.item[inventoryIndex].stack - amount <= 0)
                                chest.item[inventoryIndex].SetDefaults();
                            else
                                chest.item[inventoryIndex].stack -= amount;

                            break;
                        }
        }
    }*/
}

public class ChestID
{
    public const short Default = 0;
    public const short Gold = 1;
    public const short Gold_Locked = 2;
    public const short Shadow = 3;
    public const short Shadow_Locked = 4;
    public const short Barrel = 5;
    public const short TrashCan = 6;
    public const short Ebonwood = 7;
    public const short RichMahogany = 8;
    public const short Pearlwood = 9;
    public const short Ivy = 10;
    public const short Frozen = 11;
    public const short LivingWood = 12;
    public const short Skyware = 13;
    public const short Shadewood = 14;
    public const short WebCovered = 15;
    public const short Lihzahrd = 16;
    public const short Water = 17;
    public const short Jungle = 18;
    public const short Corruption = 19;
    public const short Crimson = 20;
    public const short Hallowed = 21;
    public const short Ice = 22;
    public const short Jungle_Locked = 23;
    public const short Corruption_Locked = 24;
    public const short Crimson_Locked = 25;
    public const short Hallowed_Locked = 26;
    public const short Ice_Locked = 27;
    public const short Dynasty = 28;
    public const short Honey = 29;
    public const short Steampunk = 30;
    public const short PalmWood = 31;
    public const short Mushroom = 32;
    public const short BorealWood = 33;
    public const short Slime = 34;
    public const short GreenDungeon = 35;
    public const short GreenDungeon_Locked = 36;
    public const short PinkDungeon = 37;
    public const short PinkDungeon_Locked = 38;
    public const short BlueDungeon = 39;
    public const short BlueDungeon_Locked = 40;
    public const short Bone = 41;
    public const short Cactus = 42;
    public const short Flesh = 43;
    public const short Obsidian = 44;
    public const short Pumpkin = 45;
    public const short Spooky = 46;
    public const short Glass = 47;
    public const short Martian = 48;
    public const short Meteorite = 49;
    public const short Granite = 50;
    public const short Marble = 51;
    public const short Crystal = 52;
    public const short GoldPirate = 53;

    public static readonly List<int> Dungeon =
    [
        GreenDungeon, GreenDungeon_Locked, PinkDungeon, PinkDungeon_Locked, BlueDungeon, BlueDungeon_Locked, Gold_Locked
    ];

    public static readonly List<int> DungeonLocked =
        [GreenDungeon_Locked, PinkDungeon_Locked, BlueDungeon_Locked, Gold_Locked];

    public static readonly List<int> DungeonUnlocked = [GreenDungeon, PinkDungeon, BlueDungeon];

    public static readonly List<int> DungeonSpecial =
        [Corruption_Locked, Crimson_Locked, Ice_Locked, Jungle_Locked, Hallowed_Locked];

    public static readonly List<int> LivingTrees = [LivingWood, Ivy];
}
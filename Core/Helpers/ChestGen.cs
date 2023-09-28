using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace GenUtils
{
    class ChestGen
    {
        public static void AddChestLoot(int itemID, int chestID = -1, int minimumStack = 1, int maximumStack = 1, int chance = 10000, bool excludeDuplicates = false) =>
            AddChestLoot(itemID, chestID == -1 ? null : new List<int>() { chestID }, minimumStack, maximumStack, chance, excludeDuplicates);

        public static void AddChestLoot(int itemID, List<int> chestIDs, int minimumStack = 1, int maximumStack = 1, int chance = 10000, bool excludeDuplicates = false)
        {
            if (maximumStack < minimumStack)
                maximumStack = minimumStack;

            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                //if the drop chance for this chest is unsuccessful, skip this chest
                if (WorldGen.genRand.Next(0, 10000) < chance)
                    continue;

                Chest chest = Main.chest[chestIndex];
                //check if the chest actually exists
                if (chest != null && Main.tile[chest.x, chest.y].TileType == TileID.Containers)
                {
                    //make sure the chest is the right type that we are looking for
                    if (chestIDs == null || chestIDs.Contains(Main.tile[chest.x, chest.y].TileFrameX / 36))
                    {
                        //check all slots
                        for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        {
                            //if slot contains our item and duplicates are disabled, skip this chest
                            if (chest.item[inventoryIndex].type == itemID && excludeDuplicates)
                                break;
                            //if slot is empty or already contains our item
                            else if ((chest.item[inventoryIndex].IsAir || chest.item[inventoryIndex].type == itemID))
                            {
                                var amount = WorldGen.genRand.Next(minimumStack, maximumStack + 1);

                                //break if the amount is 0 (shouldn't try to add an item)
                                if (amount == 0)
                                    break;

                                //only create new item if none exists
                                if (chest.item[inventoryIndex].IsAir)
                                {
                                    chest.item[inventoryIndex].SetDefaults(itemID);
                                    chest.item[inventoryIndex].stack = 0;
                                }

                                chest.item[inventoryIndex].stack += amount;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveChestLoot(int itemID, int chestID = -1, int minimumAmount = 1, int maximumAmount = 1, int chance = 10000) =>
            RemoveChestLoot(itemID, chestID == -1 ? null : new List<int>() { chestID }, minimumAmount, maximumAmount, chance);

        public static void RemoveChestLoot(int itemID, List<int> chestIDs, int minimumAmount = 1, int maximumAmount = 1, int chance = 10000)
        {
            if (maximumAmount < minimumAmount)
                maximumAmount = minimumAmount;

            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                //if the chance for this chest is unsuccessful, skip this chest
                if (WorldGen.genRand.Next(0, 10000) <= chance)
                    continue;

                Chest chest = Main.chest[chestIndex];
                //check if the chest actually exists
                if (chest != null && Main.tile[chest.x, chest.y].TileType == TileID.Containers)
                {
                    //make sure the chest is the right type that we are looking for
                    if (chestIDs == null || chestIDs.Contains(Main.tile[chest.x, chest.y].TileFrameX / 36))
                    {
                        //check all slots
                        for (int inventoryIndex = 0; inventoryIndex < Chest.maxItems; inventoryIndex++)
                        {
                            //if slot contains our item
                            if (chest.item[inventoryIndex].type == itemID)
                            {
                                var amount = WorldGen.genRand.Next(minimumAmount, maximumAmount + 1);

                                //break if the amount is 0 (shouldn't try to remove an item)
                                if (amount == 0)
                                    break;

                                //remove item if stack will be less than 1
                                if (chest.item[inventoryIndex].stack - amount <= 0)
                                    chest.item[inventoryIndex].SetDefaults(0);
                                else
                                    chest.item[inventoryIndex].stack -= amount;

                                break;
                            }
                        }
                    }
                }
            }
        }
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
        public static List<int> Dungeon = new List<int> { GreenDungeon, GreenDungeon_Locked, PinkDungeon, PinkDungeon_Locked, BlueDungeon, BlueDungeon_Locked, Gold_Locked };
        public static List<int> DungeonLocked = new List<int> { GreenDungeon_Locked, PinkDungeon_Locked, BlueDungeon_Locked, Gold_Locked };
        public static List<int> DungeonUnlocked = new List<int> { GreenDungeon, PinkDungeon, BlueDungeon };
        public static List<int> DungeonSpecial = new List<int> { Corruption_Locked, Crimson_Locked, Ice_Locked, Jungle_Locked, Hallowed_Locked };
        public static List<int> Locked = new List<int> { BlueDungeon_Locked, Corruption_Locked, Crimson_Locked, Gold_Locked, GreenDungeon_Locked, Hallowed_Locked, Ice_Locked, Jungle_Locked, PinkDungeon_Locked, Shadow_Locked };
        public static List<int> LivingTrees = new List<int> { LivingWood, Ivy };
    }
}

using Terramon.Content.Items;
using Terramon.Helpers;
using Terraria.IO;
using Terraria.Localization;
using Terraria.WorldBuilding;

namespace Terramon.Core;

public class TerramonItemPass(string name, float loadWeight) : GenPass(name, loadWeight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Language.GetTextValue("Mods.Terramon.WorldGen.ItemsPass");

        var rareCandyType = ModContent.ItemType<RareCandy>();
        ChestGen.AddChestLoot(rareCandyType, ChestID.Gold, 1, 3, 7500);
        ChestGen.AddChestLoot(rareCandyType, minimumStack: 1, maximumStack: 2, chance: 3500,
            excludeDuplicates: true);
        ChestGen.AddChestLoot(ModContent.ItemType<WaterStone>(), ChestID.Water, chance: 8500);
        ChestGen.AddChestLoot(ModContent.ItemType<ThunderStone>(), ChestID.Gold_Locked, chance: 500);
        ChestGen.AddChestLoot(ModContent.ItemType<FireStone>(), ChestID.Shadow_Locked, chance: 1000);
        ChestGen.AddChestLoot(ModContent.ItemType<LeafStone>(), ChestID.LivingTrees, chance: 1500);
        ChestGen.AddChestLoot(ModContent.ItemType<MoonStone>(), ChestID.Gold, chance: 250);
    }
}
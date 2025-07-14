using Terramon.Content.Items;
using Terramon.Content.Items.Evolutionary;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Items.Vitamins;
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

        AddChestLoot_PokeBalls();
        AddChestLoot_RareCandies();
    }

    private static void AddChestLoot_RareCandies()
    {
        var rareCandyType = ModContent.ItemType<RareCandy>();
        ChestGen.AddChestLoot(rareCandyType, ChestID.Gold, 1, 3, 0.75f);
        ChestGen.AddChestLoot(rareCandyType, minimumStack: 1, maximumStack: 2, chance: 0.35f,
            excludeDuplicates: true);
        ChestGen.AddChestLoot(ModContent.ItemType<WaterStone>(), ChestID.Water, chance: 0.85f);
        ChestGen.AddChestLoot(ModContent.ItemType<ThunderStone>(), ChestID.Gold_Locked, chance: 0.05f);
        ChestGen.AddChestLoot(ModContent.ItemType<FireStone>(), ChestID.Shadow_Locked, chance: 0.1f);
        ChestGen.AddChestLoot(ModContent.ItemType<LeafStone>(), ChestID.LivingTrees, chance: 0.15f);
        ChestGen.AddChestLoot(ModContent.ItemType<MoonStone>(), ChestID.Gold, chance: 0.025f);
    }

    private static void AddChestLoot_PokeBalls()
    {
        ChestGen.AddChestLoot(ModContent.ItemType<PokeBallItem>(),
            chest => chest.y < Main.worldSurface && Main.tile[chest.x, chest.y].TileFrameX / 36 == ChestID.Default, 3,
            5, 0.5f);
    }
}
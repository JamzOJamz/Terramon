using Terraria.Localization;

namespace Terramon.Core.Systems;

public class RecipeSystem : ModSystem
{
    private LocalizedText anySilverBarText;
    private LocalizedText anyGoldBarText;

    public override void AddRecipeGroups()
    {
        anySilverBarText = Mod.GetLocalization("RecipeGroup.AnySilverBar");
        anyGoldBarText = Mod.GetLocalization("RecipeGroup.AnyGoldBar");

        // Use vanilla item names since tML merges recipe groups with the same names
        RecipeGroup.RegisterGroup(nameof(ItemID.SilverBar), new RecipeGroup(() => anySilverBarText.Value, [
            ItemID.SilverBar,
            ItemID.TungstenBar
        ]));
        
        RecipeGroup.RegisterGroup(nameof(ItemID.GoldBar), new RecipeGroup(() => anyGoldBarText.Value, [
            ItemID.GoldBar,
            ItemID.PlatinumBar
        ]));
    }
}

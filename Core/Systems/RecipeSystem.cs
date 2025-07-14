namespace Terramon.Core.Systems;

public class RecipeSystem : ModSystem
{
    public override void AddRecipeGroups()
    {
        RecipeGroup.RegisterGroup($"{nameof(Terramon)}:SilverBar", new RecipeGroup(() => "Any Silver Bar", [
            ItemID.SilverBar,
            ItemID.TungstenBar
        ]));

        RecipeGroup.RegisterGroup($"{nameof(Terramon)}:GoldBar", new RecipeGroup(() => "Any Gold Bar", [
            ItemID.GoldBar,
            ItemID.PlatinumBar
        ]));
    }
}
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Terramon.Core;
global using Terraria;
global using Terraria.ModLoader;
using Terramon.Content.Configs;
using Terramon.Content.Items.KeyItems;

namespace Terramon;

public class Terramon : Mod
{
    public static Terramon Instance => ModContent.GetInstance<Terramon>();

    public static DatabaseV2 DatabaseV2 { get; private set; }
    
    /*
     * TODO:
     * This will be removed at a later date.
     * It exists because there are Pokémon in the DB that shouldn't be loaded as mod content (yet).
     */
    public const ushort MaxPokemonID = 151;

    public static bool RollShiny(Player player)
    {
        var shinyChance = ModContent.GetInstance<GameplayConfig>().ShinySpawnRate;
        if (player.HasItemInInventoryOrOpenVoidBag(ModContent.ItemType<ShinyCharm>())) shinyChance /= 2;
        return Main.rand.NextBool(shinyChance);
    }

    public static Gender RollGender(ushort id)
    {
        var genderRate = DatabaseV2.GetPokemon(id).GenderRate;
        return genderRate >= 0
            ? Main.rand.NextBool(genderRate, 8) ? Gender.Female : Gender.Male
            : Gender.Unspecified;
    }

    public override void Load()
    {
        /*var modLoaderAssembly = typeof(ModContent).Assembly;
        var uiModItemInitialize = modLoaderAssembly.GetType("Terraria.ModLoader.UI.UIModItem")
            ?.GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(uiModItemInitialize, UIModItemInitialize_Detour);*/
        
        // Load the database
        var dbStream = GetFileStream("Assets/Data/PokemonDB.json");
        DatabaseV2 = DatabaseV2.Parse(dbStream);
    }

    /*private static void UIModItemInitialize_Detour(orig_UIModItemInitialize orig, object self)
    {
        orig(self);
        var modLoaderAssembly = typeof(ModContent).Assembly;
        var modNameField = modLoaderAssembly.GetType("Terraria.ModLoader.UI.UIModItem")
            ?.GetField("_modName", BindingFlags.NonPublic | BindingFlags.Instance);
        var modIconField = modLoaderAssembly.GetType("Terraria.ModLoader.UI.UIModItem")
            ?.GetField("_modIcon", BindingFlags.NonPublic | BindingFlags.Instance);
        var modName = ((UIText)modNameField?.GetValue(self))?.Text;
        if (modName == null || !modName.StartsWith("Terramon Mod")) return;
        var modIcon = (UIImage)modIconField?.GetValue(self);
        var iconPath = ModContent.GetInstance<ClientConfig>().ModIconType switch
        {
            ModIconType.Alternate => "icon_alt",
            ModIconType.Classic => "icon_classic",
            _ => "icon"
        };
        modIcon?.SetImage(ModContent.Request<Texture2D>("Terramon/" + iconPath, AssetRequestMode.ImmediateLoad));
    }*/

    public override void Unload()
    {
        DatabaseV2 = null;
    }

    //private delegate void orig_UIModItemInitialize(object self);
}
global using Terraria.ModLoader;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.Databases;
using Terramon.ID;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace Terramon;

public class Terramon : Mod
{
    /*public static Terramon Instance { get; private set; }

    public Terramon()
    {
        Instance = this;
    }*/

    public static PokemonDB Database { get; private set; }

    public static bool RollShiny()
    {
        return Main.rand.NextBool(ModContent.GetInstance<GameplayConfig>().ShinySpawnRate);
    }

    public static byte RollGender(ushort id)
    {
        var genderRate = Database.GetPokemon(id).GenderRate;
        return genderRate >= 0
            ? Main.rand.NextBool(genderRate, 8) ? GenderID.Female : GenderID.Male
            : GenderID.Unknown;
    }

    public override void Load()
    {
        var modLoaderAssembly = typeof(ModContent).Assembly;
        var uiModItemInitialize = modLoaderAssembly.GetType("Terraria.ModLoader.UI.UIModItem")
            ?.GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(uiModItemInitialize, UIModItemInitialize_Detour);
        Database = LoadPokemonDatabase();
    }

    private static void UIModItemInitialize_Detour(orig_UIModItemInitialize orig, object self)
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
    }

    private PokemonDB LoadPokemonDatabase()
    {
        var stream = GetFileStream("Content/Databases/pokemon-db.tmon");
        return PokemonDB.Deserialize(stream);
    }

    public override void Unload()
    {
        Database = null;
        //Instance = null;
    }

    private delegate void orig_UIModItemInitialize(object self);
}
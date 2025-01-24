namespace Terramon.Core.Systems;

public class KeybindSystem : ModSystem
{
    public static ModKeybind HubKeybind { get; private set; }
    public static ModKeybind ToggleSidebarKeybind { get; private set; }
    public static ModKeybind TogglePokemonKeybind { get; private set; }

    public static ModKeybind NextPokemonKeybind { get; private set; }
    public static ModKeybind PrevPokemonKeybind { get; private set; }

    public override void Load() {
        ToggleSidebarKeybind = KeybindLoader.RegisterKeybind(Mod, "ToggleSidebar", "F");
        HubKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedex", "L");
        TogglePokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "TogglePokemon", "OemQuotes");
        NextPokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "NextPokemon", "OemCloseBrackets");
        PrevPokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "PrevPokemon", "OemOpenBrackets");
    }

    public override void Unload() {
        ToggleSidebarKeybind = null;
        HubKeybind = null;
        
        TogglePokemonKeybind = null;
        NextPokemonKeybind = null;
        PrevPokemonKeybind = null;
    }
}

namespace Terramon.Core.Systems;

public class KeybindSystem : ModSystem
{
    public static ModKeybind HubKeybind { get; private set; }
    public static ModKeybind OpenPokedexEntryKeybind { get; private set; }
    public static ModKeybind TogglePartyKeybind { get; private set; }
    public static ModKeybind TogglePokemonKeybind { get; private set; }
    public static ModKeybind NextPokemonKeybind { get; private set; }
    public static ModKeybind PrevPokemonKeybind { get; private set; }

    public override void Load() {
        TogglePartyKeybind = KeybindLoader.RegisterKeybind(Mod, "ToggleParty", "F");
        HubKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedex", "P");
        OpenPokedexEntryKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedexEntry", "O");
        TogglePokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "TogglePokemon", "OemQuotes");
        NextPokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "NextPokemon", "OemCloseBrackets");
        PrevPokemonKeybind = KeybindLoader.RegisterKeybind(Mod, "PrevPokemon", "OemOpenBrackets");
    }

    public override void Unload() {
        TogglePartyKeybind = null;
        HubKeybind = null;
        
        TogglePokemonKeybind = null;
        NextPokemonKeybind = null;
        PrevPokemonKeybind = null;
    }
}

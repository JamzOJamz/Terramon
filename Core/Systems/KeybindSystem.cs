namespace Terramon.Core.Systems;

public class KeybindSystem : ModSystem
{
    public static ModKeybind HubKeybind { get; private set; }
    public static ModKeybind TogglePartyKeybind { get; private set; }
    public static ModKeybind OpenPokedexEntryKeybind { get; private set; }

    public override void Load() {
        TogglePartyKeybind = KeybindLoader.RegisterKeybind(Mod, "ToggleParty", "F");
        HubKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedex", "P");
        OpenPokedexEntryKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedexEntry", "O");
    }

    public override void Unload() {
        TogglePartyKeybind = null;
        HubKeybind = null;
    }
}
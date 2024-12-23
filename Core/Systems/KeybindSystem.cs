namespace Terramon.Core.Systems;

public class KeybindSystem : ModSystem
{
    public static ModKeybind HubKeybind { get; private set; }
    public static ModKeybind ToggleSidebarKeybind { get; private set; }

    public override void Load() {
        ToggleSidebarKeybind = KeybindLoader.RegisterKeybind(Mod, "ToggleSidebar", "F");
        HubKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenPokedex", "L");
    }

    public override void Unload() {
        ToggleSidebarKeybind = null;
        HubKeybind = null;
    }
}
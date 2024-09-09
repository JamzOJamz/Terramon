namespace Terramon.Core.Systems;

public class KeybindSystem : ModSystem
{
    public static ModKeybind ToggleSidebarKeybind { get; private set; }

    public override void Load() {
        ToggleSidebarKeybind = KeybindLoader.RegisterKeybind(Mod, "ToggleSidebar", "F");
    }

    public override void Unload() {
        ToggleSidebarKeybind = null;
    }
}
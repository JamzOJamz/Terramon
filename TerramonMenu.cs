using ReLogic.Content;

namespace Terramon;

public class TerramonMenu : ModMenu
{
    public override string DisplayName => "Terramon Mod";

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Audio/Music/menu_theme");

    public override Asset<Texture2D> Logo =>
        ModContent.Request<Texture2D>("Terramon/logo", AssetRequestMode.ImmediateLoad);
}
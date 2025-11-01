using System.Reflection;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;

namespace Terramon.Core.Systems;

[Autoload(Side = ModSide.Client)]
public class AnimatedIconSystem : ModSystem
{
    private const int FrameDelay = 12; // 60fps / 12 = 5fps
    private static UIImage _modIcon;

    private static readonly string[] IconFramePaths =
    [
        "Terramon/icon",
        "Terramon/Assets/Misc/AnimatedIcon_1",
        "Terramon/Assets/Misc/AnimatedIcon_2",
        "Terramon/Assets/Misc/AnimatedIcon_3",
        "Terramon/Assets/Misc/AnimatedIcon_4",
        "Terramon/Assets/Misc/AnimatedIcon_5",
        "Terramon/Assets/Misc/AnimatedIcon_6",
        "Terramon/Assets/Misc/AnimatedIcon_7"
    ];

    private static Asset<Texture2D>[] _iconFrameTextures;

    private static int _iconFrameTimer;

    public override void Load()
    {
        // Don't initialize the system if the animated mod icon is disabled in the config
        if (!ClientConfig.Instance.AnimatedModIcon) return;

        // Load icon frames from the specified paths
        _iconFrameTextures = new Asset<Texture2D>[IconFramePaths.Length];
        for (var i = 0; i < IconFramePaths.Length; i++)
            _iconFrameTextures[i] = ModContent.Request<Texture2D>(IconFramePaths[i]);
        
        Type uiModItemType;
        
        // Concise Mods List compatibility
        if (ModLoader.TryGetMod("ConciseModList", out var conciseModList))
        {
            var conciseModListAssembly = conciseModList.Code;
            uiModItemType = conciseModListAssembly.GetType("ConciseModList.ConciseUIModItem");
        }
        else
        {
            uiModItemType = typeof(UIModItem);
        }
        
        // If the UIModItem type is not found, the system cannot be initialized so return
        if (uiModItemType == null) return;

        var uiModItemInitialize = uiModItemType!.GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(uiModItemInitialize, UIModItemInitialize_Detour);
        
        var uiModsUpdate = typeof(UIMods)
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(uiModsUpdate, UIModsUpdate_Detour);
    }

    public override void Unload()
    {
        // The system was not initialized, so there's nothing to unload
        if (_iconFrameTextures == null) return;

        // Unload icon frames
        for (var i = 0; i < IconFramePaths.Length; i++)
            _iconFrameTextures[i] = null;
        _iconFrameTextures = null;
    }

    private static void UIModItemInitialize_Detour(OrigUIModItemInitialize orig, UIModItem self)
    {
        orig(self);

        if (self._mod.Name is not nameof(Terramon)) return;
        _modIcon = self._modIcon;
    }

    private static void UIModsUpdate_Detour(OrigUIModsUpdate orig, UIMods self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (_modIcon == null)
        {
            _iconFrameTimer = 0;
            return;
        }

        _iconFrameTimer++;
        if (_iconFrameTimer % FrameDelay != 0) return;
        var frameIndex = _iconFrameTimer / FrameDelay % _iconFrameTextures.Length;
        _modIcon.SetImage(_iconFrameTextures[frameIndex].Value);
    }

    private delegate void OrigUIModItemInitialize(UIModItem self);

    private delegate void OrigUIModsUpdate(UIMods self, GameTime gameTime);
}
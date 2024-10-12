using System.Collections.Generic;
using System.Reflection;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public class HubUI : SmartUIState
{
    private static bool _playerInventoryOpen;

    static HubUI()
    {
        var playerToggleInvMethod = typeof(Player).GetMethod("ToggleInv", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(playerToggleInvMethod, PlayerToggleInv_Detour);
    }

    public static bool Active { get; private set; }
    public override bool Visible => Active;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    }

    public static void SetActive(bool active)
    {
        if (Active == active) return;
        if (!Active && active) _playerInventoryOpen = Main.playerInventory;
        if (active)
            IngameFancyUI.OpenUIState(UILoader.GetUIState<HubUI>());
        else
            IngameFancyUIClose();
        if (Active && !active) Main.playerInventory = _playerInventoryOpen;
        Active = active;
        SoundEngine.PlaySound(new SoundStyle(active ? "Terramon/Sounds/dex_open" : "Terramon/Sounds/dex_close")
        {
            Volume = 0.48f
        });
    }

    public static void ToggleActive()
    {
        SetActive(!Active);
    }

    public override void OnInitialize()
    {
        var backPanel = new UITextPanel<LocalizedText>(Language.GetText("UI.Back"), 0.7f, true)
        {
            HAlign = 0.5f
        };
        backPanel.Width.Set(440f, 0);
        backPanel.Height.Set(50f, 0);
        backPanel.Top.Set(-74f, 1f);
        backPanel.OnMouseOver += FadedMouseOver;
        backPanel.OnMouseOut += FadedMouseOut;
        backPanel.OnLeftClick += (_, _) => { SetActive(false); };
        backPanel.SetSnapPoint("Back", 0);

        Append(backPanel);
    }

    private static void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ((UIPanel)evt.Target).BackgroundColor = new Color(73, 94, 171);
        ((UIPanel)evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    private static void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        ((UIPanel)evt.Target).BackgroundColor = new Color(63, 82, 151) * 0.8f;
        ((UIPanel)evt.Target).BorderColor = Color.Black;
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        // Recalculate inventory slots even if it is not visible
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (!inventoryParty.Visible) inventoryParty.Recalculate();

        Recalculate();
    }

    /// <summary>
    ///     The same as <see cref="IngameFancyUI.Close" /> but does not play the <see cref="SoundID.MenuClose" /> sound.
    /// </summary>
    private static void IngameFancyUIClose()
    {
        Main.inFancyUI = false;
        //SoundEngine.PlaySound(SoundID.MenuClose); // Commented out to prevent the sound from playing
        var flag = !Main.gameMenu;
        var flag2 = Main.InGameUI.CurrentState is not UIVirtualKeyboard;
        var flag3 = false;
        var keyboardContext = UIVirtualKeyboard.KeyboardContext;
        if ((uint)(keyboardContext - 2) <= 1u)
            flag3 = true;

        if (flag && !(flag2 || flag3))
            flag = false;

        if (flag)
            Main.playerInventory = true;

        if (!Main.gameMenu && Main.InGameUI.CurrentState is UIEmotesMenu)
            Main.playerInventory = false;

        Main.LocalPlayer.releaseInventory = false;
        Main.InGameUI.SetState(null);
        UILinkPointNavigator.Shortcuts.FANCYUI_SPECIAL_INSTRUCTIONS = 0;
    }

    /// <summary>
    ///     Prevents the player from closing the inventory while the hub UI is active, instead closing the hub UI itself.
    /// </summary>
    private static void PlayerToggleInv_Detour(OrigPlayerToggleInv orig, object self)
    {
        if (Main.playerInventory || !Active)
        {
            orig(self);
            return;
        }

        SetActive(false);
    }

    private delegate void OrigPlayerToggleInv(object self);
}
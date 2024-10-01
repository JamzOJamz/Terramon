using System.Collections.Generic;
using System.Reflection;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class HubUI : SmartUIState
{
    static HubUI()
    {
        var playerToggleInvMethod = typeof(Player).GetMethod("ToggleInv", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(playerToggleInvMethod, PlayerToggleInv_Detour);
        var mainDrawInterfaceResourcesBuffsMethod = typeof(Main).GetMethod("DrawInterface_Resources_Buffs", BindingFlags.Instance | BindingFlags.Public);
        MonoModHooks.Add(mainDrawInterfaceResourcesBuffsMethod, MainDrawInterfaceResourcesBuffs_Detour);
    }

    public static bool Active { get; private set; }

    public override bool Visible => Active;
    
    private static bool _playerInventoryOpen;

    private static void SetActive(bool active)
    {
        if (Active == active) return;
        if (!Active && active) _playerInventoryOpen = Main.playerInventory;
        Active = active;
        if (_playerInventoryOpen) Main.playerInventory = !active;
        SoundEngine.PlaySound(new SoundStyle(active ? "Terramon/Sounds/dex_open" : "Terramon/Sounds/dex_close")
        {
            Volume = 0.48f
        });
    }

    public static void ToggleActive()
    {
        SetActive(!Active);
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    }

    public override void InformLayers(List<GameInterfaceLayer> layers)
    {
        // Don't do anything if the hub UI is not active
        if (!Active) return;

        var highIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Death Text"));

        // Iterate all layers below this one and disable them
        for (var i = highIndex - 1; i >= 0; i--)
        {
            var t = layers[i];
            if (t.Name is "Vanilla: Resource Bars" or "Vanilla: Mouse Text" or "Vanilla: Interface Logic 1"
                    or "Vanilla: Interface Logic 2" or "Vanilla: Interface Logic 3" or "Vanilla: Interface Logic 4" ||
                t.Name == UILoader.GetLayerName(this))
                continue;
            t.Active = false;
        }
    }

    public override void OnInitialize()
    {
        var panel = new UIPanel();
        panel.Left.Set(0, 0);
        panel.Top.Set(0, 0);
        panel.Width.Set(200, 0);
        panel.Height.Set(200, 0);

        Append(panel);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        // Recalculate inventory slots even if it is not visible
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (!inventoryParty.Visible) inventoryParty.Recalculate();

        //Recalculate();
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
    
    /// <summary>
    ///     Prevents the game from drawing buff icons beneath the hotbar while the hub UI is active.
    /// </summary>
    private static void MainDrawInterfaceResourcesBuffs_Detour(OrigMainDrawInterfaceResourcesBuffs orig, object self)
    {
        if (!Active) orig(self);
    }
    
    private delegate void OrigMainDrawInterfaceResourcesBuffs(object self);
}
using Terramon.Content.Configs;
using Terramon.Content.GUI;
using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public class UILinkManager : ILoadable
{
    //reference position for first slot in the row (default Pokédex position)
    private static readonly Vector2 FirstSlotPos = new(51, 291);
    
    public void Load(Mod mod)
    {
        SetupPartyUIPage();
        SetupPCUIPage();
        
        //add delegate to inventory updateevent
        var invPage = UILinkPointNavigator.Pages[GamepadPageID.Inventory];
        invPage.UpdateEvent += UpdateInventory;
    }

    public void Unload()
    {
        RemovePage(TerramonPageID.Party);
        RemovePage(TerramonPageID.PC);
        UILinkPointNavigator.Pages[GamepadPageID.Inventory].UpdateEvent -= UpdateInventory;
    }

    private static void RemovePage(int pageId)
    {
        foreach (var point in UILinkPointNavigator.Pages[pageId].LinkMap)
            UILinkPointNavigator.Points.Remove(point.Key);
        UILinkPointNavigator.Pages.Remove(pageId);
    }


    private static void SetupPartyUIPage()
    {
        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        var hasAutoTrash = ModLoader.HasMod("AutoTrash");
        
        var slotOffset = hasAutoTrash ? 1 : 0;
        slotOffset += reducedMotion ? 1 : 0;
        
        
        
        //Add new page to control party slot items
        var partyPage = new UILinkPage();
        partyPage.PageOnLeft = GamepadPageID.CraftSmall;
        partyPage.PageOnRight = GamepadPageID.Ammo;
        
        //Add tooltips for special inventory interactions (e.g. switch page)
        partyPage.OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[56].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"]) + PlayerInput.BuildCommand(Lang.misc[64].Value, true, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"]);
        //TODO: actually add these interactions
        
        //add party slots
        for (int i = TerramonPointID.Party0; i <= TerramonPointID.Party5; i++) {
            UILinkPoint newPoint = new UILinkPoint(i, enabled: true, i - 1, i + 1, 43 + (i - 9600 - slotOffset), -1);
            partyPage.LinkMap.Add(i, newPoint);
        }
        
        //add navigation for party end slots
        if (!hasAutoTrash && !reducedMotion)
            partyPage.LinkMap[TerramonPointID.Party5].Right = GamepadPointID.TrashItem;
        
        //add other inventory buttons (collapse button and pokedex button)
        partyPage.LinkMap.Add(TerramonPointID.PartyCollapse, new UILinkPoint(TerramonPointID.PartyCollapse, enabled: true, TerramonPointID.HubUI, -1, -1, -1));
        partyPage.LinkMap[TerramonPointID.PartyCollapse].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //select
        
        partyPage.LinkMap.Add(TerramonPointID.HubUI, new UILinkPoint(TerramonPointID.HubUI, enabled: true,
            -1, reducedMotion ? TerramonPointID.Party0 : TerramonPointID.PartyCollapse, -1, -1));
        partyPage.LinkMap[TerramonPointID.HubUI].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //select
        
        //add update events for new link points (anything that needs changing during runtime)
        partyPage.UpdateEvent += delegate
        {
            //whether the party is functionally compressed
            var compressedState = InventoryParty.IsCompressed && !InventoryParty.InPCMode;
            
            //have to set position in case gui scale has changed (even though slot position doesn't)
            for (int i = 0; i <= 5; i++)
            {
                //not using TerramonPointID here as it would be lengthy + wouldn't improve readability anyways
                partyPage.LinkMap[9600 + i].Position =
                    (FirstSlotPos + new Vector2(48 * (3 + i - slotOffset), 0)) * Main.UIScale;
                
                //add navigation to bestiary button
                if (i >= 4)
                    partyPage.LinkMap[9600 + i].Down = GamepadPointID.BestiaryMenu;
                else //we need to reset this in case it was modified by other methods (the PC)
                    partyPage.LinkMap[9600 + i].Down = -1;
                
                //set controller hints based on slot state
                var heldPokemon = TooltipOverlay.GetHeldPokemon(out var source);
                if (heldPokemon != null)
                {
                    if (TerramonPlayer.LocalPlayer.Party[i] != null && heldPokemon != TerramonPlayer.LocalPlayer.Party[i])
                        partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[66].Value, false,
                        PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //swap
                    else
                        partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[65].Value, false,
                            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //place
                }
                else if (TerramonPlayer.LocalPlayer.Party[i] != null)
                {
                    if (Main.mouseItem.ModItem is IPokemonDirectUse item)
                    {
                        if (item.AffectedByPokemonDirectUse(TerramonPlayer.LocalPlayer.Party[i]))
                            partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[79].Value, false,
                                    PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //use
                        else
                            partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => "";
                    }
                    else
                        partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[54].Value, false,
                            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //take
                }
                else
                    partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => "";
            }

            //left party end slot (might nav to journey button)
            partyPage.LinkMap[TerramonPointID.Party0].Left = 
                !reducedMotion ? TerramonPointID.PartyCollapse : Main.GameModeInfo.IsJourneyMode ? GamepadPointID.CreativeMenuToggle : TerramonPointID.HubUI;

            //collapse button position + navigation
            var collapseButtonOffset = compressedState || reducedMotion ? 6 : 0;
            if (hasAutoTrash) collapseButtonOffset =+ 1;
            
            partyPage.LinkMap[TerramonPointID.PartyCollapse].Position = (FirstSlotPos + new Vector2(48 * (2 + collapseButtonOffset), 0)) * Main.UIScale;
            partyPage.LinkMap[TerramonPointID.PartyCollapse].Left = reducedMotion && !compressedState ? TerramonPointID.Party5 : TerramonPointID.HubUI;

            if (compressedState || reducedMotion)
            {
                partyPage.LinkMap[TerramonPointID.PartyCollapse].Down = GamepadPointID.BestiaryMenu;
                if (hasAutoTrash)
                {
                    partyPage.LinkMap[TerramonPointID.PartyCollapse].Right = -1;
                    partyPage.LinkMap[TerramonPointID.PartyCollapse].Up = 47;
                }
                else
                {
                    partyPage.LinkMap[TerramonPointID.PartyCollapse].Right = GamepadPointID.TrashItem;
                    partyPage.LinkMap[TerramonPointID.PartyCollapse].Up = 48;
                }
            }
            else
            {
                partyPage.LinkMap[TerramonPointID.PartyCollapse].Down = -1;
                partyPage.LinkMap[TerramonPointID.PartyCollapse].Right = TerramonPointID.Party0;
                partyPage.LinkMap[TerramonPointID.PartyCollapse].Up = 42;
            }

            //pokedex icon
            var dexOffset = new Vector2(Main.GameModeInfo.IsJourneyMode && !hasAutoTrash ? 1 : 0, Main.GameModeInfo.IsJourneyMode && hasAutoTrash ? 1 : 0);
            partyPage.LinkMap[TerramonPointID.HubUI].Position = (FirstSlotPos + dexOffset * 48) * Main.UIScale;
            
            partyPage.LinkMap[TerramonPointID.HubUI].Right = reducedMotion && !compressedState ? TerramonPointID.Party0 : TerramonPointID.PartyCollapse;
            partyPage.LinkMap[TerramonPointID.HubUI].Up = hasAutoTrash && Main.GameModeInfo.IsJourneyMode ? GamepadPointID.CreativeMenuToggle : 40;
        };
        
        UILinkPointNavigator.RegisterPage(partyPage, TerramonPageID.Party);
    }

    private static void SetupPCUIPage()
    {
        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        
        //Add new page to control PC items
        var pcPage = new UILinkPage();
        pcPage.PageOnLeft = TerramonPageID.Party;
        pcPage.PageOnRight = TerramonPageID.Party;
        
        //Add tooltips for special inventory interactions (e.g. switch page)
        pcPage.OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[56].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"]) + PlayerInput.BuildCommand(Lang.misc[64].Value, true, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"]);
        
        //add pc slots
        for (int x = 0; x <= 5; x++) {
            for (int y = 0; y <= 4; y++)
            {
                var pointID = TerramonPointID.PC0 + (x + y * 6);
                UILinkPoint newPoint =
                    new UILinkPoint(pointID, enabled: true, pointID - 1, pointID + 1, pointID - 6, pointID + 6);
                if (x == 0)
                    newPoint.Left = -1;
                if (x == 5)
                    newPoint.Right = -1;
                if (y == 4)
                    newPoint.Down = -1;
                if (y == 0)
                    newPoint.Up = TerramonPointID.Party0 + x;
                pcPage.LinkMap.Add(pointID, newPoint);
            }
        }
        
        pcPage.LinkMap.Add(TerramonPointID.PCLeft, new UILinkPoint(TerramonPointID.PCLeft, true,
            TerramonPointID.PC5, TerramonPointID.PCRight, -1, TerramonPointID.PCColor));
        pcPage.LinkMap[TerramonPointID.PCLeft].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        pcPage.LinkMap.Add(TerramonPointID.PCRight, new UILinkPoint(TerramonPointID.PCRight, true,
            TerramonPointID.PCLeft,-1, -1, TerramonPointID.PCColor));
        pcPage.LinkMap[TerramonPointID.PCRight].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        pcPage.LinkMap.Add(TerramonPointID.PCColor, new UILinkPoint(TerramonPointID.PCColor, true,
            TerramonPointID.PC11,-1, TerramonPointID.PCLeft, TerramonPointID.PCRename));
        pcPage.LinkMap[TerramonPointID.PCColor].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        pcPage.LinkMap.Add(TerramonPointID.PCRename, new UILinkPoint(TerramonPointID.PCRename, true,
            TerramonPointID.PC17,-1, TerramonPointID.PCColor, -1));
        pcPage.LinkMap[TerramonPointID.PCRename].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);

        pcPage.LinkMap[TerramonPointID.PC5].Right = TerramonPointID.PCLeft;
        pcPage.LinkMap[TerramonPointID.PC11].Right = TerramonPointID.PCColor;
        pcPage.LinkMap[TerramonPointID.PC17].Right = TerramonPointID.PCRename;

        pcPage.UpdateEvent += delegate
        {
            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    var pointID = TerramonPointID.PC0 + (x + y * 6);
                    
                    //have to set this in UpdateEvent in case gui scale changes
                    pcPage.LinkMap[pointID].Position = (FirstSlotPos + ((new Vector2(reducedMotion ? 2 : 3, 1) + new Vector2(x, y)) * 48)) * Main.UIScale;
                    
                    //set controller hints based on slot state
                    var heldPokemon = TooltipOverlay.GetHeldPokemon(out var source);
                    var slotContainsPokemon = TerramonPlayer.LocalPlayer.GetPC().Boxes[PCInterface.DisplayedBoxIndex][pointID - 9610] != null;
                    
                    if (heldPokemon != null)
                    {
                        if (slotContainsPokemon) //TODO: make this work for pretendToBeEmpty
                            pcPage.LinkMap[pointID].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[66].Value, false,
                                PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //swap
                        else
                            pcPage.LinkMap[pointID].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[65].Value, false,
                                PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //place
                    }
                    else if (slotContainsPokemon)
                        pcPage.LinkMap[pointID].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[54].Value, false,
                            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //take
                    else
                        pcPage.LinkMap[pointID].OnSpecialInteracts += () => "";
                }
            }

            pcPage.LinkMap[TerramonPointID.PCLeft].Position = (new Vector2(420, 356) + new Vector2(!reducedMotion ? 48 : 0, 0)) * Main.UIScale;
            pcPage.LinkMap[TerramonPointID.PCRight].Position = (new Vector2(452, 356) + new Vector2(!reducedMotion ? 48 : 0, 0)) * Main.UIScale;
            pcPage.LinkMap[TerramonPointID.PCColor].Position = (new Vector2(420, 390) + new Vector2(!reducedMotion ? 48 : 0, 0)) * Main.UIScale;
            pcPage.LinkMap[TerramonPointID.PCRename].Position = (new Vector2(420, 416) + new Vector2(!reducedMotion ? 48 : 0, 0)) * Main.UIScale;
        };
        
        UILinkPointNavigator.RegisterPage(pcPage, TerramonPageID.PC);

        //Add navigation to the PC from the party slots
        var partyPage = UILinkPointNavigator.Pages[TerramonPageID.Party];
        partyPage.UpdateEvent += delegate
        {
            if (!PCInterface.Active) return;
            for (int i = TerramonPointID.Party0; i <= TerramonPointID.Party5; i++)
                partyPage.LinkMap[i].Down = i + 10;
        };
    }

    private static void UpdateInventory()
    {
        var invPage = UILinkPointNavigator.Pages[GamepadPageID.Inventory];
        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        var hasAutoTrash = ModLoader.HasMod("AutoTrash");
        
        var slotOffset = hasAutoTrash ? 1 : 0;
        slotOffset += reducedMotion ? 1 : 0;
        
        //whether the party is functionally compressed
        var compressedState = InventoryParty.IsCompressed && !InventoryParty.InPCMode;

        //if party is visible, remap slot nav inputs to go to party buttons
        if (Main.playerInventory && Main.LocalPlayer.chest == -1 && Main.npcShop == 0 &&
            !Main.LocalPlayer.dead && !Main.inFancyUI && TerramonPlayer.LocalPlayer.HasChosenStarter)
        {
            //set party slot nav inputs
            if (!compressedState || InventoryParty.InPCMode)
                for (int i = 43; i <= 48; i++)
                    invPage.LinkMap[i - slotOffset].Down = 9600 + i - 43;

            //set nav input for collapse button (didn't seem necessary to use GamepadPointID for inventory slots)
            var collapseButtonPoint = (reducedMotion || compressedState) ? 48 : 42;
            if (hasAutoTrash) collapseButtonPoint -= 1;
            invPage.LinkMap[collapseButtonPoint].Down = TerramonPointID.PartyCollapse;

            //set slot 47 regardless of autotrash/reduced motion (would otherwise go to bestiary)
            if (compressedState)
                invPage.LinkMap[47].Down = TerramonPointID.PartyCollapse;

            //set navigation from bestiary button to inventoryparty items (rather than inventory slot 47)
            invPage.LinkMap[GamepadPointID.BestiaryMenu].Up = compressedState || reducedMotion
                ? TerramonPointID.PartyCollapse
                : TerramonPointID.Party5;

            //set nav input for Pokédex icon
            if (Main.GameModeInfo.IsJourneyMode)
            {
                if (hasAutoTrash)
                {
                    invPage.LinkMap[GamepadPointID.CreativeMenuToggle].Down = TerramonPointID.HubUI;
                    invPage.LinkMap[GamepadPointID.CreativeMenuToggle].Right =
                        reducedMotion ? TerramonPointID.Party0 : TerramonPointID.PartyCollapse;
                }
                else
                {
                    invPage.LinkMap[41].Down = TerramonPointID.HubUI;
                    invPage.LinkMap[GamepadPointID.CreativeMenuToggle].Right = TerramonPointID.HubUI;
                }
            }
            else
                invPage.LinkMap[40].Down = TerramonPointID.HubUI;

            //Set trash slot left input if auto trash isn't installed
            if (!hasAutoTrash)
            {
                if (collapseButtonPoint == 48)
                    invPage.LinkMap[GamepadPointID.TrashItem].Left = TerramonPointID.PartyCollapse;
                else
                    invPage.LinkMap[GamepadPointID.TrashItem].Left = TerramonPointID.Party5;
            }
        }
        
        //modify trash slot hint if a Pokémon is held within the PC
        if (PCInterface.Active && TooltipOverlay.GetHeldPokemon(out var source) != null)
            invPage.LinkMap[GamepadPointID.TrashItem].OnSpecialInteracts += () => PlayerInput.BuildCommand(
                Language.GetTextValue("Mods.Terramon.GUI.ControllerHints.Release"), false,
                PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        else
            invPage.LinkMap[GamepadPointID.TrashItem].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[74].Value, false,
                PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
    }
}

public static class TerramonPageID
{
    public const int Party = 2700;
    public const int PC = 2701;
}

public static class TerramonPointID
{
    public const int Party0 = 9600;
    public const int Party1 = 9601;
    public const int Party2 = 9602;
    public const int Party3 = 9603;
    public const int Party4 = 9604;
    public const int Party5 = 9605;
    public const int PartyCollapse = 9606;
    public const int HubUI = 9607;
    public const int PC0 = 9610;
    public const int PC1 = 9611;
    public const int PC2 = 9612;
    public const int PC3 = 9613;
    public const int PC4 = 9614;
    public const int PC5 = 9615;
    public const int PC6 = 9616;
    public const int PC7 = 9617;
    public const int PC8 = 9618;
    public const int PC9 = 9619;
    public const int PC10 = 9620;
    public const int PC11 = 9621;
    public const int PC12 = 9622;
    public const int PC13 = 9623;
    public const int PC14 = 9624;
    public const int PC15 = 9625;
    public const int PC16 = 9626;
    public const int PC17 = 9627;
    public const int PC18 = 9628;
    public const int PC19 = 9629;
    public const int PC20 = 9630;
    public const int PC21 = 9631;
    public const int PC22 = 9632;
    public const int PC23 = 9633;
    public const int PC24 = 9634;
    public const int PC25 = 9635;
    public const int PC26 = 9636;
    public const int PC27 = 9637;
    public const int PC28 = 9638;
    public const int PC29 = 9639;
    public const int PCLeft = 9640;
    public const int PCRight = 9641;
    public const int PCColor = 9642;
    public const int PCRename = 9643;
}
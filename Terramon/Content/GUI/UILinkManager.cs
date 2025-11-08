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
    public bool IsLoadingEnabled(Mod mod) => !Main.dedServ;
    public void Load(Mod mod)
    {
        SetupPartyUIPage();
        SetupPCUIPage();
        SetupHubUIPage();
        
        //add delegate to inventory updateevent
        var invPage = UILinkPointNavigator.Pages[GamepadPageID.Inventory];
        invPage.UpdateEvent += UpdateInventory;
        
        //add hacky page switches
        var leftPage = new UILinkPage();
        leftPage.LinkMap.Add(TerramonPointID.HackyPointLeft, new UILinkPoint(TerramonPointID.HackyPointLeft, false, -1, -1, -1, -1));
        UILinkPointNavigator.RegisterPage(leftPage, TerramonPageID.HackySwitchPageLeft);
        
        var rightPage = new UILinkPage();
        rightPage.LinkMap.Add(TerramonPointID.HackyPointRight, new UILinkPoint(TerramonPointID.HackyPointRight, false, -1, -1, -1, -1));
        UILinkPointNavigator.RegisterPage(rightPage, TerramonPageID.HackySwitchPageRight);
    }

    public void Unload()
    {
        RemovePage(TerramonPageID.Party);
        RemovePage(TerramonPageID.PC);
        RemovePage(TerramonPageID.HubUI);
        RemovePage(TerramonPageID.Pokedex);
        RemovePage(TerramonPageID.HackySwitchPageLeft);
        RemovePage(TerramonPageID.HackySwitchPageRight);
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
        var reducedMotion = ClientConfig.Instance.ReducedMotion;
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
            UILinkPoint newPoint = new(i, enabled: true, i - 1, i + 1, 43 + (i - 9600 - slotOffset), -1);
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
        var reducedMotion = ClientConfig.Instance.ReducedMotion;
        
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
                    new(pointID, enabled: true, pointID - 1, pointID + 1, pointID - 6, pointID + 6);
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
            TerramonPointID.PC17,-1, TerramonPointID.PCColor, TerramonPointID.PCColorH));
        pcPage.LinkMap[TerramonPointID.PCRename].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        //TODO: add clipboard/random/etc uilinkpoints
        pcPage.LinkMap.Add(TerramonPointID.PCColorH, new UILinkPoint(TerramonPointID.PCColorH, true,
            -1, -1, TerramonPointID.PCRename, TerramonPointID.PCColorS));
        pcPage.LinkMap.Add(TerramonPointID.PCColorS, new UILinkPoint(TerramonPointID.PCColorS, true,
            -1, -1, TerramonPointID.PCColorH, TerramonPointID.PCColorV));
        pcPage.LinkMap.Add(TerramonPointID.PCColorV, new UILinkPoint(TerramonPointID.PCColorV, true,
            -1, -1, TerramonPointID.PCColorS, -1));

        pcPage.LinkMap[TerramonPointID.PC5].Right = TerramonPointID.PCLeft;
        pcPage.LinkMap[TerramonPointID.PC11].Right = TerramonPointID.PCColor;
        pcPage.LinkMap[TerramonPointID.PC17].Right = TerramonPointID.PCRename;
        
        pcPage.LinkMap[TerramonPointID.PCRename].Down = TerramonPointID.PCColorH;

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

            //color slider code (stolen from vanilla UILinksInitializer lmoa)
            float interfaceDeadzoneX = PlayerInput.CurrentProfile.InterfaceDeadzoneX;
            float stickX = PlayerInput.GamepadThumbstickLeft.X;
            stickX = ((!(stickX < 0f - interfaceDeadzoneX) && !(stickX > interfaceDeadzoneX)) ? 0f : (MathHelper.Lerp(0f, 1f / 120f, (Math.Abs(stickX) - interfaceDeadzoneX) / (1f - interfaceDeadzoneX)) * (float)Math.Sign(stickX)));
            int currentPoint = UILinkPointNavigator.CurrentPoint;
            if (currentPoint == TerramonPointID.PCColorH)
                TerramonPlayer.LocalPlayer.ColorPickerHSL.X = MathHelper.Clamp(TerramonPlayer.LocalPlayer.ColorPickerHSL.X + stickX, 0f, 1f);
            else if (currentPoint == TerramonPointID.PCColorS)
                TerramonPlayer.LocalPlayer.ColorPickerHSL.Y = MathHelper.Clamp(TerramonPlayer.LocalPlayer.ColorPickerHSL.Y + stickX, 0f, 1f);
            else if (currentPoint == TerramonPointID.PCColorV)
                TerramonPlayer.LocalPlayer.ColorPickerHSL.Z = MathHelper.Clamp(TerramonPlayer.LocalPlayer.ColorPickerHSL.Z + stickX, 0f, 1f);
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

    //most of the logic + positioning for this page is handled in HubUI as it requires vars we don't have access to
    private static void SetupHubUIPage()
    {
        //Add new page to control HubUI items
        var hubPage = new UILinkPage();
        
        hubPage.OnSpecialInteracts  += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //select

        hubPage.LinkMap.Add(TerramonPointID.HubTab0, new UILinkPoint(TerramonPointID.HubTab0, true, -1, TerramonPointID.HubTab1, -1, TerramonPointID.PokedexMin));
        hubPage.LinkMap.Add(TerramonPointID.HubTab1, new UILinkPoint(TerramonPointID.HubTab1, true, TerramonPointID.HubTab0, TerramonPointID.HubTab2, -1, -1));
        hubPage.LinkMap.Add(TerramonPointID.HubTab2, new UILinkPoint(TerramonPointID.HubTab2, true, TerramonPointID.HubTab1, TerramonPointID.HubTab3, -1, -1));
        hubPage.LinkMap.Add(TerramonPointID.HubTab3, new UILinkPoint(TerramonPointID.HubTab3, true, TerramonPointID.HubTab2, -1, -1, -1));
        
        UILinkPointNavigator.RegisterPage(hubPage, TerramonPageID.HubUI);
        

        //Add new page to control Pokédex items
        var pokedexPage = new UILinkPage();
        pokedexPage.PageOnLeft = TerramonPageID.HackySwitchPageLeft;
        pokedexPage.PageOnRight = TerramonPageID.HackySwitchPageRight;
        
        //i'm setting this for the whole page since it'll apply to every button
        pokedexPage.OnSpecialInteracts  += () => PlayerInput.BuildCommand(Language.GetTextValue("Mods.Terramon.GUI.ControllerHints.SwitchPage"), false, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"])
                                                 + PlayerInput.BuildCommand(Lang.misc[53].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]); //select

        for (int i = TerramonPointID.PokedexMin; i <= TerramonPointID.PokedexMax; i++)
            pokedexPage.LinkMap.Add(i, new UILinkPoint(i, true, i - 1, i + 1, i - 6, i + 6));
        
        //TODO: set up to TerramonPointID.HubTab0 when multiple functional tabs exist
        pokedexPage.LinkMap.Add(TerramonPointID.PokedexLeft, new UILinkPoint(TerramonPointID.PokedexLeft, true,
            -1, TerramonPointID.PokedexRight, -1, TerramonPointID.PokedexMin));
        pokedexPage.LinkMap.Add(TerramonPointID.PokedexRight, new UILinkPoint(TerramonPointID.PokedexRight, true,
            TerramonPointID.PokedexLeft, TerramonPointID.WorldDexToggle, -1, TerramonPointID.PokedexMin + 1));
        pokedexPage.LinkMap.Add(TerramonPointID.WorldDexToggle, new UILinkPoint(TerramonPointID.WorldDexToggle, true,
            TerramonPointID.PokedexRight, -1, -1, TerramonPointID.PokedexMin + 5));
        
        UILinkPointNavigator.RegisterPage(pokedexPage, TerramonPageID.Pokedex);
    }

    public static void ResetPokedexSlots()
    {
        for (int i = TerramonPointID.PokedexMin; i <= TerramonPointID.PokedexMax; i++)
            UILinkPointNavigator.Pages[TerramonPageID.Pokedex].LinkMap[i] = new UILinkPoint(i, true, i - 1, i + 1, i - 6, i + 6);
    }

    private static void UpdateInventory()
    {
        var invPage = UILinkPointNavigator.Pages[GamepadPageID.Inventory];
        var reducedMotion = ClientConfig.Instance.ReducedMotion;
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
            
            //Set emote button to loop back around to Pokédex button (would otherwise go to trash)
            if (Main.GameModeInfo.IsJourneyMode)
                UILinkPointNavigator.Pages[GamepadPageID.Inventory].LinkMap[GamepadPointID.EmoteMenu].Right =
                    GamepadPointID.CreativeMenuToggle;
            else
                UILinkPointNavigator.Pages[GamepadPageID.Inventory].LinkMap[GamepadPointID.EmoteMenu].Right =
                    TerramonPointID.HubUI;
        }
        
        //modify trash slot hint if a Pokémon is held within the PC
        if (PCInterface.Active && TooltipOverlay.IsHoldingPokemon())
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
    public const int HubUI = 2702;
    public const int Pokedex = 2703;
    public const int HackySwitchPageLeft = 2798;
    public const int HackySwitchPageRight = 2799;
}

public static class TerramonPointID
{
    public const int HackyPointLeft = 9598;
    public const int HackyPointRight = 9599;
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
    public const int PCColorH = 9644;
    public const int PCColorS = 9645;
    public const int PCColorV = 9646;
    public const int HubTab0 = 9650;
    public const int HubTab1 = 9651;
    public const int HubTab2 = 9652;
    public const int HubTab3 = 9653;
    public const int PokedexMin = 9654; //Pokédex is allowed to span 9654-9754
    public const int PokedexMax = 9754;
    public const int PokedexLeft = 9755;
    public const int PokedexRight = 9756;
    public const int WorldDexToggle = 9757;
    public const int PokedexScrollbar = 9758;
}
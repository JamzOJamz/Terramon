using Terramon.Content.Configs;
using Terramon.Content.GUI;
using Terraria.GameInput;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public class UILinkManager : ILoadable
{
    //reference position for first slot in the row (default Pokédex position)
    private static readonly Vector2 FirstSlotPos = new(51, 291);
    
    public void Load(Mod mod)
    {
        SetupPartyUIPage();
    }

    public void Unload() { }


    private static void SetupPartyUIPage()
    {
        //these vars require reload so can be used in UpdateEvent without issues
        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        var hasAutoTrash = ModLoader.HasMod("AutoTrash");
        
        var slotOffset = hasAutoTrash ? 1 : 0;
        slotOffset += reducedMotion ? 1 : 0;
        
        //get inventory page
        var invPage = UILinkPointNavigator.Pages[GamepadPageID.Inventory];
        invPage.UpdateEvent += delegate
        {
            //whether or not the party is functionally compressed
            var compressedState = InventoryParty.IsCompressed && !InventoryParty.InPCMode;
            
            //if party is visible, remap slot nav inputs to go to party buttons
            if (Main.playerInventory && Main.LocalPlayer.chest == -1 && Main.npcShop == 0 &&
                !Main.LocalPlayer.dead && !Main.inFancyUI && TerramonPlayer.LocalPlayer.HasChosenStarter)
            {
                //set party slot nav inputs
                if (!compressedState || InventoryParty.InPCMode)
                    for (int i = 43; i <= 48; i++)
                        invPage.LinkMap[i - slotOffset].Down = 9600 + i - 43;
                
                //set nav input for collapse button
                var collapseButtonPoint = (reducedMotion || compressedState) ? 48 : 42;
                if (hasAutoTrash) collapseButtonPoint -= 1;
                invPage.LinkMap[collapseButtonPoint].Down = 9606;

                //set slot 47 regardless of autotrash/reduced motion (would otherwise go to bestiary)
                if (compressedState)
                    invPage.LinkMap[47].Down = 9606;

                //set nav input for Pokédex icon
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    if (hasAutoTrash)
                    {
                        invPage.LinkMap[311].Down = 9607;
                        invPage.LinkMap[311].Right = reducedMotion ? 9600 : 9606;
                    }
                    else
                    {
                        invPage.LinkMap[41].Down = 9607;
                        invPage.LinkMap[311].Right = 9607;
                    }
                }
                else
                    invPage.LinkMap[40].Down = 9607;

                //Set trash slot left input if auto trash isn't installed
                if (!hasAutoTrash)
                {
                    if (collapseButtonPoint == 48)
                        invPage.LinkMap[300].Left = 9606;
                    else
                        invPage.LinkMap[300].Left = 9605;
                }
            }
        };
        
        
        
        //Add new page to control party slot items
        var partyPage = new UILinkPage();
        
        //Add tooltips for special inventory interactions (e.g. switch page)
        partyPage.OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[56].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"]) + PlayerInput.BuildCommand(Lang.misc[64].Value, true, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"]);
        //TODO: actually add these interactions
        
        //add party slots
        for (int i = 9600; i <= 9605; i++) {
            UILinkPoint newPoint = new UILinkPoint(i, enabled: true, i - 1, i + 1, 43 + (i - 9600 - slotOffset), -1);
            partyPage.LinkMap.Add(i, newPoint);
        }
        
        //add navigation for party end slots
        if (!hasAutoTrash)
            partyPage.LinkMap[9605].Right = 300;
        
        //add other inventory buttons (collapse button and pokedex button)
        partyPage.LinkMap.Add(9606, new UILinkPoint(9606, enabled: true, 9607, 9600, -1, -1));
        partyPage.LinkMap[9606].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        partyPage.LinkMap.Add(9607, new UILinkPoint(9607, enabled: true, -1, reducedMotion ? 9600 : 9606, -1, -1));
        partyPage.LinkMap[9607].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[53].Value, false,
            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
        
        //add update events for new link points (anything that needs changing during runtime)
        partyPage.UpdateEvent += delegate
        {
            //whether or not the party is functionally compressed
            var compressedState = InventoryParty.IsCompressed && !InventoryParty.InPCMode;
            
            //have to set position in case gui scale has changed (even though slot position doesn't)
            for (int i = 0; i <= 5; i++)
            {
                partyPage.LinkMap[9600 + i].Position =
                    (FirstSlotPos + new Vector2(48 * (3 + i - slotOffset), 0)) * Main.UIScale;
                
                //add navigation to bestiary button
                if (i >= 4)
                    partyPage.LinkMap[9600 + i].Down = 310;
                
                //set controller hints based on slot state
                if (TooltipOverlay.GetHeldPokemon(out var source) != null)
                {
                    if (TerramonPlayer.LocalPlayer.Party[i] == null) //TODO: make this work for slots that are set to appear empty
                        partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[66].Value, false,
                        PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
                    else
                        partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[65].Value, false,
                            PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
                }
                else if (TerramonPlayer.LocalPlayer.Party[i] != null)
                    partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[54].Value, false,
                        PlayerInput.ProfileGamepadUI.KeyStatus["MouseLeft"]);
                else
                    partyPage.LinkMap[9600 + i].OnSpecialInteracts += () => "";
            }

            //left party end slot (might nav to journey button)
            partyPage.LinkMap[9600].Left = !reducedMotion ? 9606 : Main.GameModeInfo.IsJourneyMode ? 311 : 9607;

            //collapse button position + navigation
            var collapseButtonOffset = compressedState || reducedMotion ? 6 : 0;
            if (hasAutoTrash) collapseButtonOffset =+ 1;
            
            partyPage.LinkMap[9606].Position = (FirstSlotPos + new Vector2(48 * (2 + collapseButtonOffset), 0)) * Main.UIScale;

            if (compressedState || reducedMotion)
            {
                partyPage.LinkMap[9606].Down = 310;
                if (hasAutoTrash)
                {
                    partyPage.LinkMap[9606].Right = -1;
                    partyPage.LinkMap[9606].Up = 47;
                }
                else
                {
                    partyPage.LinkMap[9606].Right = 300;
                    partyPage.LinkMap[9606].Up = 48;
                }
            }
            else
            {
                partyPage.LinkMap[9606].Down = -1;
                partyPage.LinkMap[9606].Right = 9600;
                partyPage.LinkMap[9606].Up = 42;
            }

            //pokedex icon
            var dexOffset = new Vector2(Main.GameModeInfo.IsJourneyMode && !hasAutoTrash ? 1 : 0, Main.GameModeInfo.IsJourneyMode && hasAutoTrash ? 1 : 0);
            partyPage.LinkMap[9607].Position = (FirstSlotPos + dexOffset * 48) * Main.UIScale;
            
            partyPage.LinkMap[9607].Right = reducedMotion && !compressedState ? 9600 : 9606;
            partyPage.LinkMap[9607].Up = hasAutoTrash && Main.GameModeInfo.IsJourneyMode ? 311 : 40;
        };
        
        UILinkPointNavigator.RegisterPage(partyPage, 2702);
    }
}
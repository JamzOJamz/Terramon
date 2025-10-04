using ReLogic.Content;
using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public class InventoryParty : SmartUIState
{
    private static readonly CustomPartyItemSlot[] CustomSlots = new CustomPartyItemSlot[6];
    private static readonly Asset<Texture2D> PartySlotBallTexture;
    private static readonly Asset<Texture2D> PartySlotBallGreyedTexture;
    private static readonly Asset<Texture2D> PartySlotBallHoverTexture;
    private static readonly Asset<Texture2D> PokedexButtonTexture;
    private static readonly Asset<Texture2D> PokedexButtonHoverAltTexture;

    private static bool _reducedMotion;

    private readonly LocalizedText _hidePartyLocalizedText = Language.GetText("Mods.Terramon.GUI.Inventory.HideParty");

    private readonly LocalizedText _openPokedexLocalizedText =
        Language.GetText("Mods.Terramon.GUI.Inventory.OpenPokedex");

    private readonly LocalizedText _showPartyLocalizedText = Language.GetText("Mods.Terramon.GUI.Inventory.ShowParty");
    private readonly ITweener[] _toggleTweens = new ITweener[3];

    public static bool IsCompressed { get; private set; }
    private UIHoverImageButton _openPokedexButton;
    private UIHoverImageButton _toggleSlotsButton;

    static InventoryParty()
    {
        PartySlotBallTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBall");
        PartySlotBallHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBallHover");
        PartySlotBallGreyedTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBallGreyed");
        PokedexButtonTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PokedexButton");
        PokedexButtonHoverAltTexture =
            ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PokedexButtonHoverAlt");
    }

    public override bool Visible => Main.playerInventory && Main.LocalPlayer.chest == -1 && Main.npcShop == 0 &&
                                    !Main.LocalPlayer.dead && !Main.inFancyUI && !Main.LocalPlayer.tileEntityAnchor.InUse &&
                                    TerramonPlayer.LocalPlayer.HasChosenStarter;

    public static bool InPCMode { get; private set; }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        // AutoTrash compatibility
        var container = new UIContainer(new Vector2(449, 304));
        if (ModLoader.HasMod("AutoTrash"))
            container.Left.Set(-47, 0f);

        // Initialize according to reduced motion setting
        _reducedMotion = ClientConfig.Instance.ReducedMotion;
        if (_reducedMotion)
        {
            IsCompressed = true;
            _toggleSlotsButton = new UIHoverImageButton(PartySlotBallGreyedTexture, _showPartyLocalizedText);
        }
        else
        {
            _toggleSlotsButton = new UIHoverImageButton(PartySlotBallTexture, _hidePartyLocalizedText);
        }

        _toggleSlotsButton.SetHoverImage(PartySlotBallHoverTexture);
        _toggleSlotsButton.SetVisibility(1, 1);
        _toggleSlotsButton.OnLeftClick += ToggleSlots;
        AddElement(_toggleSlotsButton, _reducedMotion ? 404 : 118, 262, 36, 36, container);

        int[] slotPositionsX = [158, 206, 254, 301, 349, 396];
        for (var i = 0; i < slotPositionsX.Length; i++)
        {
            var slot = new CustomPartyItemSlot(i);
            if (_reducedMotion) slot.Color = Color.White * 0f;
            CustomSlots[i] = slot;
            AddElement(slot, slotPositionsX[i] - (_reducedMotion ? 47 : 0), 254, 50, 50, container);
        }

        Append(container);

        // Add Pokédex button
        _openPokedexButton = new UIHoverImageButton(PokedexButtonTexture, _openPokedexLocalizedText);
        _openPokedexButton.SetHoverImage(PokedexButtonHoverAltTexture, false);
        _openPokedexButton.SetVisibility(1, 1);
        _openPokedexButton.OnLeftClick += (_, _) => HubUI.SetActive(true);
        AddElement(_openPokedexButton, 30, 262, 30, 36);
    }

    public void SimulateToggleSlots()
    {
        _toggleSlotsButton.LeftClick(null);
    }

    public void EnterPCMode()
    {
        InPCMode = true;

        KillAllTweens();

        _toggleSlotsButton.OnLeftClick -= ToggleSlots;
        _toggleSlotsButton.OnLeftClick += ToggleSlotsWhenDisabled;
        _toggleSlotsButton.Rotation = 0;

        if (!IsCompressed) _toggleSlotsButton.SetImage(PartySlotBallGreyedTexture);

        // Show party slots when in PC mode even when compressed
        if (!_reducedMotion)
        {
            _toggleSlotsButton.Left.Pixels = 118;
            _toggleSlotsButton.SetHoverImage(PartySlotBallHoverTexture);
        }

        _toggleSlotsButton.SetHoverText(_hidePartyLocalizedText);

        foreach (var slot in CustomSlots)
            slot.Color = Color.White;
    }

    public void ExitPCMode()
    {
        InPCMode = false;

        KillAllTweens();

        _toggleSlotsButton.OnLeftClick -= ToggleSlotsWhenDisabled;
        _toggleSlotsButton.OnLeftClick += ToggleSlots;
        _toggleSlotsButton.Rotation = 0;

        if (!IsCompressed)
        {
            _toggleSlotsButton.SetImage(PartySlotBallTexture);
        }
        else
        {
            // Hide party slots when exiting PC mode
            _toggleSlotsButton.Left.Pixels = 404;
            _toggleSlotsButton.SetHoverText(_showPartyLocalizedText);
            foreach (var slot in CustomSlots)
                slot.Color = Color.Transparent;
        }
    }

    private void KillAllTweens()
    {
        if (_toggleTweens[1] is not { IsRunning: true }) return;
        foreach (var tween in _toggleTweens)
            tween.Kill();
    }

    private void ToggleSlots(UIMouseEvent evt, UIElement listeningelement)
    {
        if (_toggleTweens[1] is { IsRunning: true }) return;

        SoundEngine.PlaySound(IsCompressed ? SoundID.MenuOpen : SoundID.MenuClose);
        IsCompressed = !IsCompressed;
        _toggleSlotsButton.SetImage(IsCompressed ? PartySlotBallGreyedTexture : PartySlotBallTexture);
        var startingAlpha = (float)IsCompressed.ToInt();

        var reducedMotion = ClientConfig.Instance.ReducedMotion;
        if (reducedMotion)
        {
            _toggleSlotsButton.SetHoverText(IsCompressed ? _showPartyLocalizedText : _hidePartyLocalizedText);
            foreach (var slot in CustomSlots)
                slot.Color = Color.White * (1 - startingAlpha);
            return;
        }

        // Start animation for the compression of the party slots
        IgnoresMouseInteraction = true;
        _toggleSlotsButton.SetHoverText(string.Empty);
        _toggleSlotsButton.SetHoverImage(null);
        _toggleSlotsButton.Width.Set(36, 0);
        _toggleSlotsButton.Height.Set(36, 0);
        _toggleSlotsButton.Rotation = 0;
        var endRotation = (float)Math.PI * 2f;
        if (!IsCompressed) endRotation *= -1;
        _toggleTweens[0] = Tween.To(() => _toggleSlotsButton.Rotation, x => _toggleSlotsButton.Rotation = x,
            endRotation, 0.35f);
        var toggleTween = Tween.To(() => _toggleSlotsButton.Left.Pixels, x => _toggleSlotsButton.Left.Pixels = x,
                IsCompressed ? 404 : 118, 0.6f)
            .SetEase(Ease.OutExpo);
        toggleTween.OnComplete = () =>
        {
            _toggleSlotsButton.SetHoverText(IsCompressed ? _showPartyLocalizedText : _hidePartyLocalizedText);
            _toggleSlotsButton.SetHoverImage(PartySlotBallHoverTexture);
            IgnoresMouseInteraction = false;
        };
        _toggleTweens[1] = toggleTween;
        _toggleTweens[2] = Tween.To(() => startingAlpha, x =>
        {
            var newColor = Color.White * x;
            foreach (var slot in CustomSlots)
            {
                slot.Color = newColor;
                slot.Update(null);
            }
        }, (!IsCompressed).ToInt(), 0.35f);
        
        //UILinkPointNavigator.ChangePoint(9607);
    }

    private static void ToggleSlotsWhenDisabled(UIMouseEvent evt, UIElement listeningelement)
    {
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
        {
            Volume = 0.25f
        });
    }

    private static void UpdateSlot(PokemonData data, int index)
    {
        CustomSlots[index].SetData(data);
    }

    public static void UpdateAllSlots(PokemonData[] partyData)
    {
        for (var i = 0; i < CustomSlots.Length; i++) UpdateSlot(partyData[i], i);
    }

    public static void ClearAllSlots()
    {
        for (var i = 0; i < CustomSlots.Length; i++) UpdateSlot(null, i);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        // Move Pokédex button in Journey Mode (to avoid overlap with the Power Menu toggle button)
        if (!ModLoader.HasMod("AutoTrash"))
            switch (Main.GameModeInfo.IsJourneyMode)
            {
                case true when _openPokedexButton.Left.Pixels != 76:
                    _openPokedexButton.Left.Set(76, 0f);
                    break;
                case false when _openPokedexButton.Left.Pixels != 30:
                    _openPokedexButton.Left.Set(30, 0f);
                    break;
            }
        else
            switch (Main.GameModeInfo.IsJourneyMode)
            {
                case true when _openPokedexButton.Top.Pixels != 312:
                    _openPokedexButton.Top.Set(312, 0f);
                    break;
                case false when _openPokedexButton.Top.Pixels != 262:
                    _openPokedexButton.Top.Set(262, 0f);
                    break;
            }

        // Update party display to stop dragging state
        var partyDisplay = UILoader.GetUIState<PartyDisplay>();
        if (!partyDisplay.Visible && PartyDisplay.IsDraggingSlot)
            partyDisplay.StopDragging();

        var player = TerramonPlayer.LocalPlayer;
        foreach (var slot in CustomSlots)
        {
            var partyData = player.Party[slot.Index];

            if ((slot.Data == null && partyData != null) ||
                (slot.Data != null && partyData == null) ||
                (partyData != null && partyData.IsNetStateDirty(slot.CloneData,
                    PokemonData.BitID | PokemonData.BitLevel | PokemonData.BitEXP | PokemonData.BitNickname |
                    PokemonData.BitIsShiny,
                    out _)))
                UpdateSlot(partyData, slot.Index);
        }

        Recalculate();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Main.LocalPlayer.talkNPC != -1)
        {
            _openPokedexButton.SetVisibility(0, 0);
            _openPokedexButton.IgnoresMouseInteraction = true;
        }
        else
        {
            _openPokedexButton.SetVisibility(1, 1);
            _openPokedexButton.IgnoresMouseInteraction = false;
        }

        base.Draw(spriteBatch);
    }
}

internal sealed class CustomPartyItemSlot : UIImage
{
    private static readonly Asset<Texture2D> PartySlotBgEmptyTexture;
    private static readonly Asset<Texture2D> PartySlotBgTexture;
    private static readonly Asset<Texture2D> PartySlotBgClickedTexture;
    private static readonly Asset<Texture2D> PartySlotBgEmptyHoverTexture;
    private static readonly Asset<Texture2D> PartySlotBgHoverTexture;

    private static CustomPartyItemSlot _initialSlot;

    public readonly int Index;
    private UIImage _minispriteImage;
    private bool _pretendToBeEmptyState;
    private string _tooltipName;
    private string _tooltipText;

    static CustomPartyItemSlot()
    {
        PartySlotBgEmptyTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgEmpty");
        PartySlotBgTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBg");
        PartySlotBgClickedTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgClicked");
        PartySlotBgEmptyHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgEmptyHover");
        PartySlotBgHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgHover");
    }

    public CustomPartyItemSlot(int index) : base(PartySlotBgEmptyTexture)
    {
        Index = index;
        ImageScale = 0.85f;
        AllowResizingDimensions = false;
    }

    public PokemonData CloneData { get; private set; }
    public PokemonData Data { get; private set; }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Data == null) return;
        if (Main.mouseItem.ModItem is IPokemonDirectUse directUseItem)
        {
            UseItem(directUseItem);
        }
        else if (Main.mouseItem.IsAir)
        {
        }
    }

    public override void RightClick(UIMouseEvent evt)
    {
        base.RightClick(evt);
        if (Data != null && Main.mouseItem.ModItem is IPokemonDirectUse directUseItem)
            UseItem(directUseItem, true);
    }

    private void UseItem(IPokemonDirectUse item, bool rightClick = false)
    {
        if (!item.AffectedByPokemonDirectUse(Data))
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.ItemNoEffect", Data.DisplayName),
                TerramonCommand.ChatColorYellow);
            return;
        }

        var consume = item.PokemonDirectUse(Main.LocalPlayer, Data, rightClick ? Main.mouseItem.stack : 1);
        Main.mouseItem.stack -= consume;
        if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
    }

    private void LeftClickPCMode()
    {
        var heldPokemon = TooltipOverlay.GetHeldPokemon(out var heldSource);
        //if (heldPokemon != null && heldPokemonSource != TooltipOverlay.HeldPokemonSource.Party) return; // Only allow Party related operations for now

        //Main.NewText($"_initialSlot: {_initialSlot?.Index}");

        if (Data != null)
        {
            // Fix active slots!
            var modPlayer = TerramonPlayer.LocalPlayer;
            var activePokemon = modPlayer.GetActivePokemon();
            if (heldPokemon != null && heldPokemon == activePokemon)
                modPlayer.ActiveSlot = Index;
            else if (heldPokemon != null && Data == activePokemon)
                modPlayer.ActiveSlot = _initialSlot?.Index ?? -1;

            if (heldPokemon == null && TerramonPlayer.LocalPlayer.Party.Count(d => d != null) == 1)
            {
                if (InventoryParty.InPCMode)
                    Main.NewText(Language.GetTextValue("Mods.Terramon.GUI.Inventory.CannotRemoveLastPokemon"),
                        TerramonCommand.ChatColorYellow);
                else
                    SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                    {
                        Volume = 0.25f
                    });
                return;
            }

            SoundEngine.PlaySound(SoundID.Grab);
            if (heldPokemon == Data)
            {
                TooltipOverlay.ClearHeldPokemon(place: false);
                SetData(heldPokemon);
                _initialSlot = null;
                _pretendToBeEmptyState = false;
                return;
            }

            if (heldPokemon != null && heldSource == TooltipOverlay.HeldPokemonSource.PC)
                // Special case for setting held Pokemon - treat like PC mon
                TooltipOverlay.SetHeldPokemon(Data, TooltipOverlay.HeldPokemonSource.PC, d =>
                {
                    var box = modPlayer.GetPC().Boxes[PCInterface.DisplayedBoxIndex];

                    // Check for free space in the box starting from the end
                    var freeSpaceIndex = -1;
                    for (var i = PCBox.Capacity - 1; i >= 0; i--)
                        if (box[i] == null)
                        {
                            freeSpaceIndex = i;
                            break;
                        }

                    if (freeSpaceIndex != -1)
                    {
                        box[freeSpaceIndex] = d;
                        if (PCInterface.Active) PCInterface.PopulateCustomSlots(box);
                    }
                    else
                    {
                        box.Service.StorePokemon(d);
                    }
                });
            else
                TooltipOverlay.SetHeldPokemon(Data, TooltipOverlay.HeldPokemonSource.Party, d =>
                {
                    //Main.NewText($"onReturn called for index {Index} with data {d?.DisplayName}");
                    TerramonPlayer.LocalPlayer.Party[Index] = d;
                    SetData(d);
                    _initialSlot = null;
                    _pretendToBeEmptyState = false;
                }, (d, newSource) =>
                {
                    /*Main.NewText(
                        $"onPlace called for index {Index} with data {d?.DisplayName} from source {newSource}");*/
                    if (d != null && newSource == TooltipOverlay.HeldPokemonSource.PC)
                        d = null;
                    if (d == null) // Disposed (not swapped)
                    {
                        var useIndex = _initialSlot?.Index ?? Index;
                        var activeMon = modPlayer.GetActivePokemon();
                        modPlayer.Party[useIndex] = null;
                        // Fix any gaps in the party array by cascading the Pokémon down
                        for (var i = 0; i < modPlayer.Party.Length - 1; i++)
                            if (modPlayer.Party[i] == null)
                                for (var j = i; j < modPlayer.Party.Length - 1; j++)
                                    modPlayer.Party[j] = modPlayer.Party[j + 1];
                        if (modPlayer.Party[4] == modPlayer.Party[5])
                            modPlayer.Party[5] = null;
                        if (activeMon != null)
                            modPlayer.ActiveSlot = Array.IndexOf(modPlayer.Party, activeMon);
                        if (_initialSlot != null)
                        {
                            _initialSlot.SetData(null);
                            _initialSlot._pretendToBeEmptyState = false;
                            _initialSlot = null;
                        }
                        else
                        {
                            SetData(null);
                            _pretendToBeEmptyState = false;
                        }
                    }
                    else // Overwritten (swapped)
                    {
                        if (_initialSlot == null) return;
                        modPlayer.Party[_initialSlot.Index] = d;
                        /*if (d == modPlayer.GetActivePokemon())
                            modPlayer.ActiveSlot = _initialSlot.Index;*/
                        _initialSlot.SetData(d);
                        _initialSlot?._minispriteImage?.Remove();
                    }
                });

            if (heldPokemon == null) // Initial pickup
            {
                _initialSlot = this;
                _pretendToBeEmptyState = true;
                _minispriteImage?.Remove();
                SetImage(PartySlotBgEmptyTexture);
            }
            else // Swap
            {
                modPlayer.Party[Index] = heldPokemon;
                SetData(heldPokemon);
            }
        }
        else
        {
            if (heldPokemon == null) return;
            SoundEngine.PlaySound(SoundID.Grab);
            var modPlayer = TerramonPlayer.LocalPlayer;
            var activePokemon = modPlayer.GetActivePokemon();
            TooltipOverlay.ClearHeldPokemon();
            // Find lowest empty slot searching from the current index - 1
            var emptySlot = Index;
            for (var i = Index - 1; i >= 0; i--)
                if (TerramonPlayer.LocalPlayer.Party[i] == null)
                    emptySlot = i;
                else
                    break;
            modPlayer.Party[emptySlot] = heldPokemon;
            // Fix any gaps in the party array by cascading the Pokémon down
            for (var i = 0; i < modPlayer.Party.Length - 1; i++)
                if (modPlayer.Party[i] == null)
                    for (var j = i; j < modPlayer.Party.Length - 1; j++)
                        modPlayer.Party[j] = modPlayer.Party[j + 1];
            if (modPlayer.Party[4] == modPlayer.Party[5])
                modPlayer.Party[5] = null;
            if (activePokemon != null)
                modPlayer.ActiveSlot = Array.IndexOf(modPlayer.Party, activePokemon);
        }
    }

    public void SetData(PokemonData data)
    {
        if (CloneData == null && data != null)
            SetImage(PartySlotBgTexture);
        else if (CloneData != null && data == null)
            SetImage(PartySlotBgEmptyTexture);
        Data = data;
        CloneData = data?.ShallowCopy();
        _minispriteImage?.Remove();
        if (data != null)
        {
            var schema = data.Schema;
            var hp = data.HP;
            var maxHp = data.MaxHP;
            if (data.Level < Terramon.MaxPokemonLevel)
            {
                var minTotalExp = ExperienceLookupTable.GetLevelTotalExp(data.Level, schema.GrowthRate);
                var nextLevelExp = ExperienceLookupTable.GetLevelTotalExp((byte)(data.Level + 1), schema.GrowthRate);
                var pointsToNextLevel = nextLevelExp - minTotalExp;
                var pointsProgress = data.TotalEXP - minTotalExp;
                _tooltipText =
                    Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotTooltip", hp, maxHp,
                        pointsProgress, pointsToNextLevel); // [c/80B9F1:Frozen]
            }
            else
            {
                _tooltipText = Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotTooltipMaxLevel", hp, maxHp);
            }

            _tooltipName = Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotName", data.DisplayName, data.Level);
            _minispriteImage = new UIImage(data.GetMiniSprite())
            {
                ImageScale = 0.7f
            };
            _minispriteImage.Top.Set(-6, 0f);
            _minispriteImage.Left.Set(-14, 0f);
            _minispriteImage.Color = Color;
            Append(_minispriteImage);
        }
        else
        {
            _tooltipName = null;
            _tooltipText = null;
        }

        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        IgnoresMouseInteraction = Color.A < 255;
        
        if (Data != null)
            _minispriteImage.Color = Color;
        
        if (IsMouseHovering && UILinkPointNavigator.InUse)
            SetImage(_pretendToBeEmptyState || Data == null ? PartySlotBgEmptyHoverTexture : PartySlotBgHoverTexture);
        else if (!_pretendToBeEmptyState && Data != null)
        {
            if (Main.mouseItem.ModItem is IPokemonDirectUse directUseItem &&
                     directUseItem.AffectedByPokemonDirectUse(Data))
                SetImage(PartySlotBgClickedTexture);
            else if (!_pretendToBeEmptyState)
                SetImage(PartySlotBgTexture);
        }
        else
            SetImage(PartySlotBgEmptyTexture);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering)
        {
            if (Main.mouseItem.IsAir)
            {
                if (Data != null)
                {
                    if (KeybindSystem.OpenPokedexEntryKeybind.JustPressed)
                    {
                        HubUI.OpenToPokemon(Data.ID, Data.IsShiny);
                        return;
                    }

                    TooltipOverlay.SetName(_tooltipName);
                    var tooltip = _tooltipText;
                    if (InventoryParty.InPCMode)
                    {
                        var key = Data == TerramonPlayer.LocalPlayer.GetActivePokemon()
                            ? "Mods.Terramon.GUI.Inventory.SlotTooltipPCModeActive"
                            : "Mods.Terramon.GUI.Inventory.SlotTooltipPCMode";
                        tooltip += "\n" + Language.GetTextValue(key);
                    }

                    TooltipOverlay.SetTooltip(tooltip);
                    TooltipOverlay.SetIcon(BallAssets.GetBallIcon(Data.Ball));
                    if (Data.IsShiny) TooltipOverlay.SetColor(ModContent.GetInstance<KeyItemRarity>().RarityColor);
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                    LeftClickPCMode();
            }
            else if (Data != null && Main.mouseItem.ModItem is IPokemonDirectUse directUseItem &&
                     directUseItem.AffectedByPokemonDirectUse(Data))
            {
                Main.instance.MouseText("Use " + Main.mouseItem.Name);
            }
        }

        base.Draw(spriteBatch);
    }
}
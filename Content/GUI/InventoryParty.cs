using System;
using System.Collections.Generic;
using ReLogic.Content;
using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class InventoryParty : SmartUIState
{
    private static readonly CustomPartyItemSlot[] CustomSlots = new CustomPartyItemSlot[6];
    private static readonly Asset<Texture2D> PartySlotBallTexture;
    private static readonly Asset<Texture2D> PartySlotBallGreyedTexture;
    private static readonly Asset<Texture2D> PartySlotBallHoverTexture;
    private static readonly Asset<Texture2D> PokedexButtonTexture;
    private static readonly Asset<Texture2D> PokedexButtonHoverAltTexture;

    private readonly LocalizedText _hidePartyLocalizedText = Language.GetText("Mods.Terramon.GUI.Inventory.HideParty");

    private readonly LocalizedText _openPokedexLocalizedText =
        Language.GetText("Mods.Terramon.GUI.Inventory.OpenPokedex");

    private readonly LocalizedText _showPartyLocalizedText = Language.GetText("Mods.Terramon.GUI.Inventory.ShowParty");

    private bool _isCompressed;
    private UIHoverImageButton _openPokedexButton;
    private UIHoverImageButton _toggleSlotsButton;
    private ITweener _toggleTween;

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
                                    !Main.LocalPlayer.dead && !Main.inFancyUI &&
                                    TerramonPlayer.LocalPlayer.HasChosenStarter;

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
        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        if (reducedMotion)
        {
            _isCompressed = true;
            _toggleSlotsButton = new UIHoverImageButton(PartySlotBallGreyedTexture, _showPartyLocalizedText);
        }
        else
        {
            _toggleSlotsButton = new UIHoverImageButton(PartySlotBallTexture, _hidePartyLocalizedText);
        }

        _toggleSlotsButton.SetHoverImage(PartySlotBallHoverTexture);
        _toggleSlotsButton.SetVisibility(1, 1);
        _toggleSlotsButton.OnLeftClick += ToggleSlots;
        AddElement(_toggleSlotsButton, reducedMotion ? 404 : 118, 262, 36, 36, container);

        int[] slotPositionsX = [158, 206, 254, 301, 349, 396];
        for (var i = 0; i < slotPositionsX.Length; i++)
        {
            var slot = new CustomPartyItemSlot(i);
            if (reducedMotion) slot.Color = Color.White * 0f;
            CustomSlots[i] = slot;
            AddElement(slot, slotPositionsX[i] - (reducedMotion ? 47 : 0), 254, 50, 50, container);
        }

        Append(container);

        // Add Pokédex button
        _openPokedexButton = new UIHoverImageButton(PokedexButtonTexture, _openPokedexLocalizedText);
        _openPokedexButton.SetHoverImage(PokedexButtonHoverAltTexture, false);
        _openPokedexButton.SetVisibility(1, 1);
        _openPokedexButton.OnLeftClick += (_, _) => HubUI.SetActive(true);
        AddElement(_openPokedexButton, 30, 262, 30, 36);
    }

    private void ToggleSlots(UIMouseEvent evt, UIElement listeningelement)
    {
        if (_toggleTween is { IsRunning: true }) return;

        SoundEngine.PlaySound(_isCompressed ? SoundID.MenuOpen : SoundID.MenuClose);
        _isCompressed = !_isCompressed;
        _toggleSlotsButton.SetImage(_isCompressed ? PartySlotBallGreyedTexture : PartySlotBallTexture);
        var startingAlpha = (float)_isCompressed.ToInt();

        var reducedMotion = ModContent.GetInstance<ClientConfig>().ReducedMotion;
        if (reducedMotion)
        {
            _toggleSlotsButton.SetHoverText(_isCompressed ? _showPartyLocalizedText : _hidePartyLocalizedText);
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
        if (!_isCompressed) endRotation *= -1;
        Tween.To(() => _toggleSlotsButton.Rotation, x => _toggleSlotsButton.Rotation = x, endRotation, 0.35f);
        _toggleTween = Tween.To(() => _toggleSlotsButton.Left.Pixels, x => _toggleSlotsButton.Left.Pixels = x,
                _isCompressed ? 404 : 118, 0.6f)
            .SetEase(Ease.OutExpo);
        _toggleTween.OnComplete = () =>
        {
            _toggleSlotsButton.SetHoverText(_isCompressed ? _showPartyLocalizedText : _hidePartyLocalizedText);
            _toggleSlotsButton.SetHoverImage(PartySlotBallHoverTexture);
            IgnoresMouseInteraction = false;
        };
        Tween.To(() => startingAlpha, x =>
        {
            var newColor = Color.White * x;
            foreach (var slot in CustomSlots)
            {
                slot.Color = newColor;
                slot.Update(null);
            }
        }, (!_isCompressed).ToInt(), 0.35f);
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
                    PokemonData.BitID | PokemonData.BitLevel | PokemonData.BitNickname | PokemonData.BitIsShiny,
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

    public readonly int Index;
    private UIImage _minispriteImage;
    private string _tooltipName;
    private string _tooltipText;

    static CustomPartyItemSlot()
    {
        PartySlotBgEmptyTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgEmpty");
        PartySlotBgTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBg");
        PartySlotBgClickedTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Inventory/PartySlotBgClicked");
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
        UseItem();
    }
    
    public override void RightClick(UIMouseEvent evt)
    {
        base.RightClick(evt);
        UseItem(true);
    }

    private void UseItem(bool rightClick = false)
    {
        if (Data == null || Main.mouseItem.ModItem is not TerramonItem { HasPokemonDirectUse: true } item) return;
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
            var schema = Terramon.DatabaseV2.GetPokemon(data.ID);
            var hp = data.HP;
            if (data.Level < Terramon.MaxPokemonLevel)
            {
                var currentLevelExp = ExperienceLookupTable.GetLevelTotalExp(data.Level, schema.GrowthRate);
                var nextLevelExp = ExperienceLookupTable.GetLevelTotalExp((byte)(data.Level + 1), schema.GrowthRate);
                var toNextLevel = nextLevelExp - currentLevelExp;
                _tooltipText =
                    Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotTooltip", hp, hp,
                        toNextLevel); // [c/80B9F1:Frozen]
            }
            else
            {
                _tooltipText = Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotTooltipMaxLevel", hp, hp);
            }

            _tooltipName = Language.GetTextValue("Mods.Terramon.GUI.Inventory.SlotName", data.DisplayName, data.Level);
            _minispriteImage = new UIImage(ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{schema.Identifier}{(!string.IsNullOrEmpty(data.Variant) ? "_" + data.Variant : string.Empty)}_Mini{(data.IsShiny ? "_S" : string.Empty)}",
                AssetRequestMode.ImmediateLoad))
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
        if (Data == null) return;
        _minispriteImage.Color = Color;
        if (Main.mouseItem.ModItem is TerramonItem { HasPokemonDirectUse: true } item &&
            item.AffectedByPokemonDirectUse(Data))
            SetImage(PartySlotBgClickedTexture);
        else
            SetImage(PartySlotBgTexture);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering && Main.mouseItem.type == ItemID.None && Data != null)
        {
            TooltipOverlay.SetName(_tooltipName);
            TooltipOverlay.SetTooltip(_tooltipText);
            TooltipOverlay.SetIcon(BallAssets.GetBallIcon(Data.Ball));
            if (Data.IsShiny) TooltipOverlay.SetColor(ModContent.GetInstance<KeyItemRarity>().RarityColor);
        }

        base.Draw(spriteBatch);
    }
}
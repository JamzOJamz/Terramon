using System.Text;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class PartyDisplay : SmartUIState
{
    private static readonly PartySidebarSlot[] PartySlots = new PartySidebarSlot[6];
    public static bool IsDraggingSlot { get; set; }
    public static PartySidebar Sidebar { get; private set; }

    public override bool Visible =>
        !Main.playerInventory && !Main.LocalPlayer.dead && TerramonPlayer.LocalPlayer.HasChosenStarter && !HubUI.Active;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        Sidebar = new PartySidebar(new Vector2(120, 486))
        {
            VAlign = 0.5f
        };
        Sidebar.Left.Set(-1, 0f);
        for (var i = 0; i < PartySlots.Length; i++)
        {
            var slot = new PartySidebarSlot(this, i);
            slot.Top.Set(71 * i + 12 * i - 2, 0f);
            Sidebar.Append(slot);
            PartySlots[i] = slot;
        }

        Append(Sidebar);
    }

    private static void UpdateSlot(PokemonData data, int index)
    {
        PartySlots[index].SetData(data);
    }

    public static void RecalculateSlot(int index)
    {
        PartySlots[index].SetData(PartySlots[index].Data);
    }

    public static void UpdateAllSlots(PokemonData[] partyData)
    {
        for (var i = 0; i < PartySlots.Length; i++) UpdateSlot(partyData[i], i);
    }

    public static void ClearAllSlots()
    {
        for (var i = 0; i < PartySlots.Length; i++) UpdateSlot(null, i);
    }

    public static void SwapSlotIndexes(int index1, int index2)
    {
        TerramonPlayer.LocalPlayer.SwapParty(index1, index2);
        PartySlots[index1].Index = index2;
        PartySlots[index2].Index = index1;
        var slot1 = PartySlots[index1];
        var slot2 = PartySlots[index2];
        PartySlots[index1] = slot2;
        PartySlots[index2] = slot1;

        PartySlots[index2].PlayIndexSound();
    }

    public void StopDragging()
    {
        foreach (var slot in PartySlots)
            slot.RightMouseUp(null);
        Recalculate();
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        var player = TerramonPlayer.LocalPlayer;

        // Update inventory slots even if it is not visible
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (!inventoryParty.Visible) inventoryParty.SafeUpdate(gameTime);

        foreach (var slot in PartySlots)
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
}

public sealed class PartySidebar(Vector2 size) : UIContainer(size)
{
    private bool _isToggled = true;
    private bool _keyUp = true;
    private ITweener _toggleTween;

    public override void Update(GameTime gameTime)
    {
        //base.Update(gameTime);

        //below code is a modification of code in UIElement.Update()
        //create a static version of Elements so modification in BringSlotToTop doesn't cause errors
        var elementsStatic = new UIElement[Elements.Count];
        Elements.CopyTo(elementsStatic);
        foreach (var element in elementsStatic) element.Update(gameTime);

        var openKey = KeybindSystem.TogglePartyKeybind.Current;
        switch (openKey)
        {
            case true when _keyUp:
            {
                _keyUp = false;
                if (Main.blockInput) break;
                _toggleTween?.Kill();
                if (_isToggled)
                {
                    _toggleTween = Tween.To(() => Left.Pixels, x => Left.Pixels = x, -125, 0.5f).SetEase(Ease.OutExpo);
                    _isToggled = false;
                }
                else
                {
                    _toggleTween = Tween.To(() => Left.Pixels, x => Left.Pixels = x, 0, 0.5f).SetEase(Ease.OutExpo);
                    _isToggled = true;
                }

                break;
            }
            case false:
                _keyUp = true;
                break;
        }
    }

    public void ForceKillAnimation()
    {
        _toggleTween?.Kill();
        Left.Pixels = _isToggled ? 0 : -125;
        Recalculate();
    }

    public void BringSlotToTop(PartySidebarSlot slot)
    {
        var index = Elements.FindIndex(s => (PartySidebarSlot)s == slot);
        Elements.RemoveAt(index);
        Elements.Add(slot);
    }
}

public class PartySidebarSlot : UIImage
{
    private readonly UIText _levelText;
    private readonly UIText _nameText;
    private readonly PartyDisplay _partyDisplay;
    private bool _dragging;
    private UIBlendedImage _genderIcon;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private UIBlendedImage _heldItemBox;
#pragma warning restore CS0649
    private int _index;
    private bool _isActiveSlot;
    private bool _isHovered;
    private bool _justEndedDragging;
    private bool _monitorCursor;
    private UIMouseEvent _monitorEvent;
    private Vector2 _offset;
    private ITweener _snapTween;
    private UIBlendedImage _spriteBox;
    public PokemonData CloneData;
    public PokemonData Data;

    public PartySidebarSlot(PartyDisplay partyDisplay, int index) : base(ModContent.Request<Texture2D>(
        "Terramon/Assets/GUI/Party/SidebarClosed"))
    {
        _partyDisplay = partyDisplay;
        Index = index;
        _nameText = new UIText(string.Empty, 0.67f);
        _nameText.Left.Set(8, 0);
        _nameText.Top.Set(57, 0);
        Append(_nameText);
        _levelText = new UIText(string.Empty, 0.67f);
        _levelText.Left.Set(8, 0);
        _levelText.Top.Set(10, 0);
        Append(_levelText);
    }

    public static CancellationTokenSource CrySoundSource { get; private set; }

    public int Index
    {
        get => _index;
        set
        {
            SnapPosition(value);
            _index = value;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (!IsMouseHovering || Data == null || PartyDisplay.IsDraggingSlot) return;
        if (KeybindSystem.OpenPokedexEntryKeybind.JustPressed)
        {
            HubUI.OpenToPokemon(Data.ID, Data.IsShiny);
            return;
        }

        var hoverText =
            Language.GetTextValue(_isActiveSlot
                ? "Mods.Terramon.GUI.Party.SlotHoverActive"
                : "Mods.Terramon.GUI.Party.SlotHover");
        if (TerramonPlayer.LocalPlayer.NextFreePartyIndex() > 1)
            hoverText += Language.GetTextValue("Mods.Terramon.GUI.Party.SlotHoverExtra");
        Main.hoverItemName = hoverText;
    }

    public void PlayIndexSound()
    {
        if (ModContent.GetInstance<ClientConfig>().ReducedAudio)
            return;

        var s = new SoundStyle
        {
            SoundPath = "Terramon/Sounds/button_smm",
            Pitch = (float)_index / -15 + 0.6f,
            Volume = 0.2925f
        };
        SoundEngine.PlaySound(s);
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        _monitorEvent = evt;
        _monitorCursor = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        _monitorCursor = false;
        if (_dragging)
        {
            DragEnd();
        }
        else if (IsMouseHovering && Data != null)
        {
            var s = _isActiveSlot
                ? new SoundStyle("Terramon/Sounds/pkball_consume") { Volume = 0.35f }
                : new SoundStyle("Terramon/Sounds/pkmn_recall") { Volume = 0.375f };
            SoundEngine.PlaySound(s);

            CancellationTokenSource token = null;
            if (!_isActiveSlot)
            {
                token = new CancellationTokenSource();
                Task.Run(() =>
                {
                    // Wait for ~500ms before playing the sound
                    Thread.Sleep(490);
                    if (token.Token.IsCancellationRequested) return;

                    Main.QueueMainThreadAction(() =>
                    {
                        var cry = new SoundStyle("Terramon/Sounds/Cries/" + Data.InternalName)
                            { Volume = 0.15f };
                        SoundEngine.PlaySound(cry);
                    });
                }, token.Token);
            }

            if (_isActiveSlot)
            {
                TerramonPlayer.LocalPlayer.ActiveSlot = -1;
                PartyDisplay.RecalculateSlot(Index);
            }
            else
            {
                var oldSlot = TerramonPlayer.LocalPlayer.ActiveSlot;
                TerramonPlayer.LocalPlayer.ActiveSlot = Index;
                if (oldSlot != -1) PartyDisplay.RecalculateSlot(oldSlot);
                PartyDisplay.RecalculateSlot(Index);
            }

            CrySoundSource = token;
        }
    }

    public override void RightMouseDown(UIMouseEvent evt)
    {
        base.RightMouseDown(evt);
        DragStart(evt);
    }

    public override void RightMouseUp(UIMouseEvent evt)
    {
        base.RightMouseUp(evt);
        DragEnd();
    }

    private void SnapPosition(int index)
    {
        if (Data == null || _dragging) return;
        _snapTween = Tween.To(() => Top.Pixels, x => Top.Pixels = x, -2 + 83 * index, 0.15f).SetEase(Ease.OutExpo);
    }

    private void DragStart(UIMouseEvent evt)
    {
        if (Data == null || TerramonPlayer.LocalPlayer.NextFreePartyIndex() < 2) return;
        PlayIndexSound();
        PartyDisplay.Sidebar.BringSlotToTop(this);
        _offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
        _dragging = true;
        PartyDisplay.IsDraggingSlot = true;
        _snapTween?.Kill();
    }

    private void DragEnd()
    {
        if (Data == null || TerramonPlayer.LocalPlayer.NextFreePartyIndex() < 2) return;
        if (ModContent.GetInstance<ClientConfig>().ReducedAudio && _partyDisplay.Visible)
            SoundEngine.PlaySound(SoundID.Tink);
        _dragging = false;
        _justEndedDragging = true;
        PartyDisplay.IsDraggingSlot = false;
        SnapPosition(Index);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Handle player removing companion buff manually (right-clicking the buff icon)
        if (_isActiveSlot && TerramonPlayer.LocalPlayer.ActiveSlot != Index)
            PartyDisplay.RecalculateSlot(Index);

        if (_monitorCursor)
            //check if mouse has travelled minimum distance in order to enter drag
            if (MathF.Abs(_monitorEvent.MousePosition.Y - Main.MouseScreen.Y) > 8)
            {
                _monitorCursor = false;
                DragStart(_monitorEvent);
            }

        var transparentColor = Color.White * 0.45f;
        if (PartyDisplay.IsDraggingSlot)
        {
            if (!_dragging)
            {
                if (Data == null) return;
                Color = transparentColor;
                _nameText.TextColor = transparentColor;
                _levelText.TextColor = transparentColor;
                //_heldItemBox.Color = transparentColor;
                _spriteBox.Color = transparentColor;
                ((UIImage)_spriteBox.Children.ElementAt(0)).Color = transparentColor;
                _genderIcon.Color = transparentColor;
                return;
            }

            var i = TerramonPlayer.LocalPlayer.NextFreePartyIndex() - 1;
            if (i == -2) i = 6;
            var bottomScrollMax = 71 * i + 12 * i - 2;
            var yOff = Math.Max(Math.Min(Main.mouseY - _offset.Y, bottomScrollMax), -2);
            switch (yOff)
            {
                case >= 45.5f when Index == 0:
                    PartyDisplay.SwapSlotIndexes(0, 1);
                    break;
                case < 45.5f when Index == 1:
                    PartyDisplay.SwapSlotIndexes(1, 0);
                    break;
                case >= 128.5f when Index == 1:
                    PartyDisplay.SwapSlotIndexes(1, 2);
                    break;
                case < 128.5f when Index == 2:
                    PartyDisplay.SwapSlotIndexes(2, 1);
                    break;
                case >= 211.5f when Index == 2:
                    PartyDisplay.SwapSlotIndexes(2, 3);
                    break;
                case < 211.5f when Index == 3:
                    PartyDisplay.SwapSlotIndexes(3, 2);
                    break;
                case >= 294.5f when Index == 3:
                    PartyDisplay.SwapSlotIndexes(3, 4);
                    break;
                case < 294.5f when Index == 4:
                    PartyDisplay.SwapSlotIndexes(4, 3);
                    break;
                case >= 377.5f when Index == 4:
                    PartyDisplay.SwapSlotIndexes(4, 5);
                    break;
                case < 377.5f when Index == 5:
                    PartyDisplay.SwapSlotIndexes(5, 4);
                    break;
            }

            Top.Set(yOff, 0f);
        }
        else if (Data != null)
        {
            Color = Color.White;
            _nameText.TextColor = Color.White;
            _levelText.TextColor = Color.White;
            //_heldItemBox.Color = Color.White;
            _spriteBox.Color = Color.White;
            ((UIImage)_spriteBox.Children.ElementAt(0)).Color = Color.White;
            _genderIcon.Color = Color.White;
        }

        if (IsMouseHovering && !PartyDisplay.IsDraggingSlot)
        {
            if (Data == null || _isHovered) return;
            _isHovered = true;
            if (!_justEndedDragging) SoundEngine.PlaySound(SoundID.MenuTick);
            UpdateSprite(true);
        }
        else
        {
            if (Data == null || !_isHovered) return;
            _isHovered = false;
            _justEndedDragging = false;
            UpdateSprite();
        }
    }

    private void UpdateSprite(bool selected = false)
    {
        var spritePath = new StringBuilder("Terramon/Assets/GUI/Party/");

        if (Data != null)
        {
            spritePath.Append("SidebarOpen");

            if (selected) spritePath.Append("_Selected");

            if (_isActiveSlot) spritePath.Append("Active");
        }
        else
        {
            spritePath.Append("SidebarClosed");
        }

        SetImage(ModContent.Request<Texture2D>(spritePath.ToString(),
            AssetRequestMode.ImmediateLoad));
    }

    public void SetData(PokemonData data)
    {
        Data = data;
        CloneData = data?.ShallowCopy();
        _isActiveSlot = TerramonPlayer.LocalPlayer.ActiveSlot == Index;
        UpdateSprite(IsMouseHovering && !PartyDisplay.IsDraggingSlot);
        _heldItemBox?.Remove();
        _spriteBox?.Remove();
        _genderIcon?.Remove();
        if (data == null)
        {
            _nameText.SetText(string.Empty);
            _levelText.SetText(string.Empty);
        }
        else
        {
            _nameText.SetText(data.DisplayName);
            _levelText.SetText(Language.GetText("Mods.Terramon.GUI.Party.LevelDisplay").WithFormatArgs(data.Level));
            /*_heldItemBox = new UIBlendedImage(ModContent.Request<Texture2D>(
                "Terramon/Assets/GUI/Party/HeldItemBox" + (_isActiveSlot ? "Active" : string.Empty),
                AssetRequestMode.ImmediateLoad));
            _heldItemBox.Top.Set(25, 0f);
            _heldItemBox.Left.Set(8, 0f);*/
            _spriteBox = new UIBlendedImage(ModContent.Request<Texture2D>(
                "Terramon/Assets/GUI/Party/SpriteBox" + (_isActiveSlot ? "Active" : string.Empty),
                AssetRequestMode.ImmediateLoad));
            _spriteBox.Top.Set(10, 0f);
            _spriteBox.Left.Set(59, 0f);
            var sprite = new UIImage(data.GetMiniSprite())
            {
                ImageScale = 0.7f
            };
            sprite.Top.Set(-12, 0f);
            sprite.Left.Set(-20, 0f);
            _spriteBox.Append(sprite);
            var genderIconPath = data.Gender != Gender.Unspecified
                ? $"Terramon/Assets/GUI/Party/Icon{(data.Gender == Gender.Male ? "Male" : "Female")}"
                : "Terraria/Images/NPC_0";
            _genderIcon = new UIBlendedImage(ModContent.Request<Texture2D>(genderIconPath,
                AssetRequestMode.ImmediateLoad));
            _genderIcon.Top.Set(57, 0f);
            _genderIcon.Left.Set(87, 0f);
            //Append(_heldItemBox);
            Append(_spriteBox);
            Append(_genderIcon);
        }

        Recalculate();
    }
}
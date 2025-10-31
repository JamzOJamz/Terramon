using System.Runtime.CompilerServices;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public sealed class PartyDisplay : SmartUIState
{
    private static readonly PartySidebarSlot[] PartySlots = new PartySidebarSlot[6];
    public static bool IsDraggingSlot { get; set; }
    public static PartySidebar Sidebar { get; private set; }

    public override bool Visible
    {
        get
        {
            var terramonPlayer = TerramonPlayer.LocalPlayer;
            return !Main.playerInventory
                   && !Main.LocalPlayer.dead
                   && terramonPlayer.HasChosenStarter
                   && !HubUI.Active;
        }
    }

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
        // Sidebar.Left.Set(-1, 0f);
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
    private const float ClosedOffset = -128f;

    private bool _keyUp = true;
    private ITweener _toggleTween;

    public bool IsToggled { get; private set; } = true;

    public void Toggle()
    {
        _toggleTween?.Kill();
        if (IsToggled)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (IsToggled) return;

        _toggleTween?.Kill();
        _toggleTween = Tween.To(() => Left.Pixels, x => Left.Pixels = x, 0, 0.5f).SetEase(Ease.OutExpo);
        IsToggled = true;
    }

    public void Close()
    {
        if (!IsToggled) return;

        _toggleTween?.Kill();
        _toggleTween = Tween.To(() => Left.Pixels, x => Left.Pixels = x, ClosedOffset, 0.5f).SetEase(Ease.OutExpo);
        IsToggled = false;
    }

    public void SetToggleState(bool open)
    {
        _toggleTween?.Kill();
        IsToggled = open;
        Left.Pixels = open ? 0 : ClosedOffset;
        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        //base.Update(gameTime);

        //below code is a modification of code in UIElement.Update()
        //create a static version of Elements so modification in BringSlotToTop doesn't cause errors
        var elementsStatic = new UIElement[Elements.Count];
        Elements.CopyTo(elementsStatic);
        foreach (var element in elementsStatic) element.Update(gameTime);

        var openKey = KeybindSystem.TogglePartyKeybind.Current && TerramonPlayer.LocalPlayer.Battle == null;
        switch (openKey)
        {
            case true when _keyUp:
            {
                _keyUp = false;
                if (Main.blockInput) break;
                Toggle(); // Use the new Toggle method
                break;
            }
            case false:
                _keyUp = true;
                break;
        }
    }

/*
    public bool IsAnimationActive()
    {
        return _toggleTween is { IsRunning: true };
    }
*/

    public void ForceKillAnimation()
    {
        _toggleTween?.Kill();
        Left.Pixels = IsToggled ? 0 : ClosedOffset;
        Recalculate();
    }

    public void BringSlotToTop(PartySidebarSlot slot)
    {
        var index = Elements.FindIndex(s => (PartySidebarSlot)s == slot);
        Elements.RemoveAt(index);
        Elements.Add(slot);
    }
}

public class PartySidebarSlot : UICompositeImage
{
    private readonly UIText _levelText;
    private readonly UIText _nameText;
    private readonly PartyDisplay _partyDisplay;
    private bool _dragging;
    private UIImage _genderIcon;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private UIBlendedImage _heldItemBox;
#pragma warning restore CS0649
    private UIImage _hpMeter;
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
        "Terramon/Assets/GUI/Party/SidebarClosed"), new Point(126, 76))
    {
        _partyDisplay = partyDisplay;
        Index = index;
        _nameText = new UIText(string.Empty, 0.67f);
        _nameText.Left.Set(7, 0);
        _nameText.Top.Set(57, 0);
        Append(_nameText);
        _levelText = new UIText(string.Empty, 0.67f);
        _levelText.Left.Set(7, 0);
        _levelText.Top.Set(10, 0);
        Append(_levelText);
        RemoveFloatingPointsFromDrawPosition = true;
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

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_texture")]
    private static extern ref Asset<Texture2D> GetImage(UIImage self);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        // RemoveFloatingPointsFromDrawPosition = !PartyDisplay.IsDraggingSlot;

        var outlined = IsMouseHovering && Data != null;
        if (outlined)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null,
                Main.UIScaleMatrix);
            var outlineShader = GameShaders.Misc[$"{nameof(Terramon)}Outline"];
            var highlightColor = ClientConfig.DefaultHighlightColor;
            outlineShader.Shader.Parameters["uThickOutline"].SetValue(true);
            outlineShader.Shader.Parameters["uImageSize0"].SetValue(GetImage(this).Size());
            outlineShader
                .UseColor(highlightColor)
                .UseSecondaryColor(highlightColor.HueShift(0.035f, -0.08f))
                .Apply();
        }

        base.DrawSelf(spriteBatch);
        if (outlined)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null,
                Main.UIScaleMatrix);
        }

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
        if (ClientConfig.Instance.ReducedAudio)
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
                    Thread.Sleep(511);
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
            }
            else
            {
                var oldSlot = TerramonPlayer.LocalPlayer.ActiveSlot;
                TerramonPlayer.LocalPlayer.ActiveSlot = Index;
                if (oldSlot != -1) PartyDisplay.RecalculateSlot(oldSlot);
            }

            PartyDisplay.RecalculateSlot(Index);

            CrySoundSource = token;
        }
    }

    public override void RightMouseDown(UIMouseEvent evt)
    {
        base.RightMouseDown(evt);
        if (UILinkPointNavigator.InUse) return;
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
        if (ClientConfig.Instance.ReducedAudio && _partyDisplay.Visible)
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

        if (PartyDisplay.IsDraggingSlot)
        {
            if (!_dragging)
            {
                if (Data == null) return;
                CompositeColor = Color.White * 0.45f;
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
            var targetColor = _isActiveSlot ? Color.Pink : Color.White;
            Color = targetColor;
            CompositeColor = Color.White;
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
        var spritePath = "Assets/GUI/Party/Sidebar";

        if (Data != null)
            spritePath += "Open";
        else
            spritePath += "Closed";

        SetImage(Terramon.Instance.Assets.Request<Texture2D>(spritePath,
            AssetRequestMode.ImmediateLoad));
    }

    public void SetData(PokemonData data)
    {
        Data = data;
        CloneData = data?.ShallowCopy();
        _isActiveSlot = TerramonPlayer.LocalPlayer.ActiveSlot == Index;
        UpdateSprite(IsMouseHovering && !PartyDisplay.IsDraggingSlot);

        RemoveUIElements();

        if (data == null)
        {
            _nameText.SetText(string.Empty);
            _levelText.SetText(string.Empty);
        }
        else
        {
            _nameText.SetText(data.DisplayName);
            _levelText.SetText(Language.GetText("Mods.Terramon.GUI.Party.LevelDisplay").WithFormatArgs(data.Level));
            CreateUIElements(data);
        }

        Recalculate();
    }

    private void RemoveUIElements()
    {
        _heldItemBox?.Remove();
        _spriteBox?.Remove();
        _genderIcon?.Remove();
        _hpMeter?.Remove();
    }

    private void CreateUIElements(PokemonData data)
    {
        var assetRepository = Terramon.Instance.Assets;

        // Sprite box
        _spriteBox = new UIBlendedImage(assetRepository.Request<Texture2D>("Assets/GUI/Party/SpriteBox",
            AssetRequestMode.ImmediateLoad))
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _spriteBox.Top.Set(8, 0f);
        _spriteBox.Left.Set(59, 0f);

        var sprite = new UIImage(data.GetMiniSprite())
        {
            ImageScale = 0.7f
        };
        sprite.Top.Set(-12, 0f);
        sprite.Left.Set(-20, 0f);
        _spriteBox.Append(sprite);
        Append(_spriteBox);

        // Gender icon
        if (data.Gender != Gender.Unspecified)
        {
            _genderIcon = new UIImage(assetRepository.Request<Texture2D>($"Assets/GUI/Party/Icon{data.Gender}",
                AssetRequestMode.ImmediateLoad))
            {
                RemoveFloatingPointsFromDrawPosition = true
            };
            _genderIcon.Top.Set(54, 0f);
            _genderIcon.Left.Set(87, 0f);
            Append(_genderIcon);
        }

        // HP meter
        _hpMeter = new UIImage(assetRepository.Request<Texture2D>("Assets/GUI/Party/HPMeter",
            AssetRequestMode.ImmediateLoad))
        {
            Left = { Pixels = 112 },
            Top = { Pixels = 12 },
            RemoveFloatingPointsFromDrawPosition = true
        };
        var ball = new UIImage(BallAssets.GetBallIcon(data.Ball))
        {
            Left = { Pixels = -2 },
            Top = { Pixels = 38 },
            RemoveFloatingPointsFromDrawPosition = true
        };
        _hpMeter.Append(ball);
        Append(_hpMeter);
    }
}
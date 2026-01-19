using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Core.Battling;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
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
                    PokemonData.BitID | PokemonData.BitLevel | PokemonData.BitNickname | PokemonData.BitHP, out _)))
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

        var openKey = KeybindSystem.TogglePartyKeybind.Current && !BattleClient.LocalBattleOngoing;
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

public sealed class PartyHeldItemSlot(PartySidebarSlot parent) : UIElement
{
    private static readonly Asset<Texture2D> BackTexture;

    public Color Color = Color.White;

    static PartyHeldItemSlot()
    {
        BackTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/HeldItemBox");
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dims = GetDimensions();
        spriteBatch.Draw(BackTexture.Value, dims.Position(), Color);

        var item = parent.Data?.HeldItem;
        if (item is null || item.IsAir)
            return;

        ItemSlot.DrawItemIcon(item, ItemSlot.Context.MouseItem, spriteBatch, dims.Center(), 1f, 16f, Color.White);
    }

    public override void Recalculate()
    {
        var parentDimensions = parent.GetInnerDimensions();
        _dimensions = new CalculatedStyle
        {
            X = Left.GetValue(parentDimensions.Width) + parentDimensions.X,
            Y = Top.GetValue(parentDimensions.Height) + parentDimensions.Y,
            Width = Width.Pixels,
            Height = Height.Pixels,
        };
    }
}

public sealed class PartySidebarSlot : UICompositeImage
{
    // Textures
    private static readonly Asset<Texture2D> ClosedTexture;
    private static readonly Asset<Texture2D> OpenTexture;
    private static readonly Asset<Texture2D> SpriteBoxTexture;
    private static readonly Asset<Texture2D> MaleIconTexture;
    private static readonly Asset<Texture2D> FemaleIconTexture;
    
    // Constants
    private static readonly Color ActiveColor = new(253, 182, 218);

    // UI elements
    private readonly PartyDisplay _partyDisplay;
    private readonly PartyHeldItemSlot _heldItemBox;
    private readonly UIText _levelText;
    private readonly UIText _nameText;
    private readonly UIImage _spriteBox;
    private readonly UIImage _pokemonSprite;
    private readonly UIImage _genderIcon;
    private readonly PartySidebarHPMeter _hpMeter;

    // Animation/interaction state
    private ITweener _snapTween;
    private bool _dragging;
    private bool _justEndedDragging;
    private Vector2 _offset;
    private bool _monitorCursor;
    private UIMouseEvent _monitorEvent;

    // Display state
    private int _index;
    private bool _isActiveSlot;
    private bool _isHovered;

    // Data
    public PokemonData Data;
    public PokemonData CloneData;

    static PartySidebarSlot()
    {
        ClosedTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/SidebarClosed");
        OpenTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/SidebarOpen");
        SpriteBoxTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/SpriteBox");
        MaleIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/IconMale");
        FemaleIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/IconFemale");
    }

    public PartySidebarSlot(PartyDisplay partyDisplay, int index) : base(ClosedTexture, 126, 76)
    {
        _partyDisplay = partyDisplay;
        Index = index;
        
        // Empty texture placeholder
        var emptyTex = TextureAssets.Npc[0];
        
        _nameText = new UIText(string.Empty, 0.67f);
        _nameText.Left.Pixels = 7f;
        _nameText.Top.Pixels = 57f;
        Append(_nameText);
        
        _levelText = new UIText(string.Empty, 0.67f);
        _levelText.Left.Pixels = 7f;
        _levelText.Top.Pixels = 10f;
        Append(_levelText);
        
        _heldItemBox = new PartyHeldItemSlot(this);
        _heldItemBox.Left.Pixels = 10f;
        _heldItemBox.Top.Pixels = 24f;
        _heldItemBox.Width.Pixels = _heldItemBox.Height.Pixels = 24f;
        
        _spriteBox = new UIImage(SpriteBoxTexture)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _spriteBox.Top.Set(8, 0f);
        _spriteBox.Left.Set(59, 0f);
        _pokemonSprite = new UIImage(emptyTex)
        {
            ImageScale = 0.7f
        };
        _pokemonSprite.Top.Set(-12, 0f);
        _pokemonSprite.Left.Set(-20, 0f);
        _spriteBox.Append(_pokemonSprite);
        
        _genderIcon = new UIImage(emptyTex)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _genderIcon.Top.Set(54, 0f);
        _genderIcon.Left.Set(87, 0f);

        _hpMeter = new PartySidebarHPMeter();
        _hpMeter.Left.Pixels = 112f;
        _hpMeter.Top.Pixels = 12f;
        
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

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var outlined = IsMouseHovering && Data != null;
        if (outlined)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null,
                Main.UIScaleMatrix);
            var outlineShader = ShaderAssets.Outline;
            var highlightColor = ClientConfig.DefaultHighlightColor;
            outlineShader.Shader.Parameters["uThickOutline"].SetValue(true);
            outlineShader.Shader.Parameters["uImageSize0"].SetValue(_texture.Size());
            outlineShader
                .UseColor(highlightColor)
                .UseSecondaryColor(highlightColor.HueShift(0.035f, -0.08f))
                .Apply();
        }

        base.DrawSelf(spriteBatch);
        if (_isActiveSlot)
        {
            // Draw again with reduced opacity to make the slot appear more opaque
            var oldColor = Color;
            Color *= 0.5f;
            base.DrawSelf(spriteBatch);
            Color = oldColor;
        }
        
        if (outlined)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null,
                Main.UIScaleMatrix);
        }

        if (Data != null && ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
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

        var s = TerramonSoundID.ButtonSmm;
        s.Pitch += (float)_index / -15;
        SoundEngine.PlaySound(in s);
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
                ? TerramonSoundID.PkballConsume
                : TerramonSoundID.PkmnRecall;
            SoundEngine.PlaySound(in s);

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
                        var cry = Data.GetCry();
                        SoundEngine.PlaySound(in cry);
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
            SoundEngine.PlaySound(in SoundID.Tink);
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
            //check if mouse has traveled minimum distance in order to enter drag
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
            var targetColor = _isActiveSlot ? ActiveColor : Color.White;
            Color = targetColor;
            _spriteBox.Color = targetColor;
            _heldItemBox.Color = targetColor;
            CompositeColor = Color.White;
        }

        if (IsMouseHovering && !PartyDisplay.IsDraggingSlot)
        {
            if (Data == null || _isHovered) return;
            _isHovered = true;
            if (!_justEndedDragging) SoundEngine.PlaySound(in SoundID.MenuTick);
        }
        else
        {
            if (Data == null || !_isHovered) return;
            _isHovered = false;
            _justEndedDragging = false;
        }
    }

    private void UpdateSprite()
    {
        SetImage(Data != null ? OpenTexture : ClosedTexture);
    }

    public void SetData(PokemonData data)
    {
        Data = data;
        CloneData = data?.ShallowCopy();
        _isActiveSlot = TerramonPlayer.LocalPlayer.ActiveSlot == Index;
        UpdateSprite();

        if (data == null)
        {
            _nameText.SetText(string.Empty);
            _levelText.SetText(string.Empty);
            _spriteBox.Remove();
            _genderIcon.Remove();
            _hpMeter.Remove();
            _heldItemBox.Remove();
        }
        else
        {
            _nameText.SetText(data.DisplayName);
            if (_isActiveSlot)
                _nameText.TextColor = ClientConfig.DefaultHighlightColor;
            else
                _nameText.TextColor = Color.White;
            _levelText.SetText(Language.GetText("Mods.Terramon.GUI.Party.LevelDisplay").WithFormatArgs(data.Level));
            Append(_heldItemBox);
            _pokemonSprite.SetImage(data.GetMiniSprite());
            Append(_spriteBox);
            if (data.Gender != Gender.Unspecified)
            {
                _genderIcon.SetImage(data.Gender == Gender.Male ? MaleIconTexture : FemaleIconTexture);
                Append(_genderIcon);
            }
            _hpMeter.SetData(data.HP, data.MaxHP, data.Ball);
            Append(_hpMeter);
        }

        Recalculate();
    }
}

public class PartySidebarHPMeter : UIElement
{
    private readonly UIImage _ball;
    private const int FrameCount = 4;

    private static readonly Asset<Texture2D> Texture;

    static PartySidebarHPMeter()
    {
        Texture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/HPMeter");
    }

    /// <summary>
    ///     HP fill percentage (0f = empty, 1f = full)
    /// </summary>
    public float Percent { get; set; } = 1f;

    public PartySidebarHPMeter()
    {
        _ball = new UIImage(TextureAssets.Npc[0]) // Initialize with an empty texture
        {
            Left = { Pixels = -2 },
            Top = { Pixels = 38 },
            RemoveFloatingPointsFromDrawPosition = true
        };
        Append(_ball);
    }

    public void SetData(ushort currentHP, ushort maxHP, BallID ballID)
    {
        Percent = maxHP == 0 ? 0f : (float)currentHP / maxHP;
        var ballTexture = BallAssets.GetBallIcon(ballID);
        _ball.SetImage(ballTexture);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        var drawPos = dimensions.Position().Floor();

        var backgroundFrame = Texture.Frame(FrameCount, frameX: 3);
        spriteBatch.Draw(Texture.Value, drawPos, backgroundFrame, Color.White);

        var colorFrameX = Percent switch
        {
            > 0.5f => 0,
            > 0.2f => 1,
            _ => 2
        };

        var clampedPercent = Utils.Clamp(Percent, 0f, 1f);

        var fillFrame = Texture.Frame(FrameCount, frameX: colorFrameX);
        var fullHeight = fillFrame.Height;

        const int insetPxTop = 2;
        const int insetPxBottom = 2;

        var innerFullHeight = Math.Max(0, fullHeight - insetPxTop - insetPxBottom);
        var visibleInnerHeight = (int)(innerFullHeight * clampedPercent);
        visibleInnerHeight = Math.Max(0, visibleInnerHeight);

        if (visibleInnerHeight > 0)
        {
            var src = fillFrame;
            src.Y += insetPxTop + (innerFullHeight - visibleInnerHeight);
            src.Height = visibleInnerHeight;

            var dest = drawPos;
            dest.Y += insetPxTop + (innerFullHeight - visibleInnerHeight);

            spriteBatch.Draw(Texture.Value, dest, src, Color.White);
        }
    }
}
using System.Globalization;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.OS;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Initializers;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class PCInterface : SmartUIState
{
    private static readonly CustomPCItemSlot[,] CustomSlots = new CustomPCItemSlot[5, 6];

    //private static readonly Asset<Texture2D> ListViewButtonTexture;
    //private static readonly Asset<Texture2D> SingleViewButtonTexture;
    //private static readonly Asset<Texture2D> SmallerButtonHoverTexture;
    private static readonly Asset<Texture2D> SmallestButtonHoverTexture;
    private static readonly Asset<Texture2D> SmallPageButtonLeftTexture;
    private static readonly Asset<Texture2D> SmallPageButtonRightTexture;

    private static readonly Color[] DefaultBoxColors =
    [
        new Color(152, 200, 96),
        new Color(94, 218, 229),
        new Color(98, 168, 239),
        new Color(156, 123, 247),
        new Color(241, 134, 238),
        new Color(239, 98, 98),
        new Color(255, 149, 104),
        new Color(255, 223, 66)
    ];

    private static BetterUIText _boxNameText;
    private static UIHoverImageButton _leftArrowButton;
    private static UIHoverImageButton _rightArrowButton;
    private static PCService _pcService;
    private static PCColorPicker _colorPicker;
    private static PCDragBar _boxDragBar;
    private static PCActionButton _changeColorButton;
    private static PCActionButton _renameBoxButton;
    private static PCActionButton _cancelRenameButton;
    private static UIContainer _container;
    private static bool _inColorPickerMode;
    private static bool _pendingColorChange;
    private static bool _inRenameMode;
    private static UITextField _textInput;

    static PCInterface()
    {
        On_Main.DoUpdate_Enter_ToggleChat += orig =>
        {
            if (_inRenameMode && Main.keyState.IsKeyDown(Keys.Enter) && !Main.keyState.IsKeyDown(Keys.LeftAlt) &&
                !Main.keyState.IsKeyDown(Keys.RightAlt) && Main.hasFocus) // Submit the rename
            {
                Main.chatRelease = false;
                SoundEngine.PlaySound(SoundID.MenuClose);
                SetNameForCurrentBox(_textInput?.CurrentValue);
                _container?.RemoveChild(_cancelRenameButton);
                _renameBoxButton?.SetText("Rename");
                if (_boxNameText != null)
                {
                    _boxNameText.SetText(GetNameForCurrentBox());
                    _boxNameText.ShowTypingCaret = false;
                }

                _textInput?.SetNotTyping();
                _inRenameMode = false;
                return;
            }

            orig();
        };

        On_IngameOptions.Open += orig =>
        {
            ExitRenameMode();
            orig();
        };

        On_IngameFancyUI.OpenUIState += (orig, state) =>
        {
            ExitRenameMode();
            orig(state);
        };

        /*ListViewButtonTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/ListViewButton");
        SingleViewButtonTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SingleViewButton");
        SmallerButtonHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallerButtonHover");*/
        SmallestButtonHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallestButtonHover");
        SmallPageButtonLeftTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallPageButtonLeft");
        SmallPageButtonRightTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallPageButtonRight");
    }

    public static bool Active => _pcService != null;

    public override bool Visible => TerramonPlayer.LocalPlayer.ActivePCTileEntityID != -1 &&
                                    Main.LocalPlayer.chest == -1 && !Main.recBigList;

    public static int DisplayedBoxIndex { get; private set; }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        _container = new UIContainer(new Vector2(442, 262));
        _container.Left.Set(ModContent.GetInstance<ClientConfig>().ReducedMotion ? 73 : 120, 0);
        _container.Top.Set(306, 0);

        /*var listViewButton = new UIHoverImageButton(ListViewButtonTexture, "Switch to List View");
        listViewButton.Left.Set(50, 0);
        listViewButton.Top.Set(56, 0);
        listViewButton.SetHoverImage(SmallerButtonHoverTexture);
        listViewButton.SetVisibility(1, 1);
        AddElement(listViewButton, 0, 6, 32, 32, container);*/ // TODO: Implement list view

        int[] slotPositionsX = [158, 206, 254, 301, 349, 396];
        const int rows = 5;
        for (var i = 0; i < rows; i++)
        for (var j = 0; j < slotPositionsX.Length; j++)
        {
            var slot = new CustomPCItemSlot();
            CustomSlots[i, j] = slot;
            AddElement(slot, slotPositionsX[j] - 120, i * 48 - 4, 50, 50, _container);
        }

        _boxNameText = new BetterUIText("Box 1")
        {
            TextColor = DefaultBoxColors[0]
        };
        _boxNameText.Left.Set(334, 0);
        _boxNameText.Top.Set(9, 0);
        _container.Append(_boxNameText);

        _changeColorButton = new PCActionButton("Change Color");
        _changeColorButton.Left.Set(335, 0);
        _changeColorButton.Top.Set(68, 0);
        _changeColorButton.OnLeftClick += (_, _) =>
        {
            if (_inRenameMode)
            {
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                {
                    Volume = 0.25f
                });
                return;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);
            if (!HasChild(_colorPicker))
            {
                _inColorPickerMode = true;
                _changeColorButton.SetText("Save Color");
                _renameBoxButton.SetText("Cancel");
                _colorPicker.SetColor(GetColorForCurrentBox(), GetDefaultColorForCurrentBox());
                Append(_colorPicker);
            }
            else
            {
                if (_pendingColorChange)
                {
                    var currentColor = _colorPicker.GetColor();
                    _pcService.Boxes[DisplayedBoxIndex].Color = currentColor == GetDefaultColorForCurrentBox()
                        ? Color.Transparent
                        : currentColor;
                }

                _boxDragBar.Color = Color.Transparent;
                _inColorPickerMode = false;
                _pendingColorChange = false;
                _changeColorButton.SetText("Change Color");
                _renameBoxButton.SetText("Rename");
                RemoveChild(_colorPicker);
            }
        };
        _container.Append(_changeColorButton);

        _renameBoxButton = new PCActionButton("Rename");
        _renameBoxButton.Left.Set(335, 0);
        _renameBoxButton.Top.Set(94, 0);
        _renameBoxButton.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);

            if (!_inColorPickerMode)
            {
                // Rename the box
                if (!_container.HasChild(_cancelRenameButton))
                {
                    Main.drawingPlayerChat = false;
                    _container.Append(_cancelRenameButton);
                    _renameBoxButton.SetText("Save");
                    _textInput.SetCurrentText(_boxNameText.Text);
                    _textInput.SetTyping();
                    _boxNameText.ShowTypingCaret = true;
                    _inRenameMode = true;
                }
                else // Save the new name
                {
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    SetNameForCurrentBox(_textInput.CurrentValue);
                    _container.RemoveChild(_cancelRenameButton);
                    _renameBoxButton.SetText("Rename");
                    _boxNameText.SetText(GetNameForCurrentBox());
                    _boxNameText.ShowTypingCaret = false;
                    _textInput.SetNotTyping();
                    _inRenameMode = false;
                }

                return;
            }

            _colorPicker.Remove();
            _boxNameText.TextColor = GetColorForCurrentBox();
            _boxDragBar.Color = Color.Transparent;
            _changeColorButton.SetText("Change Color");
            _renameBoxButton.SetText("Rename");
            _pendingColorChange = false;
            _inColorPickerMode = false;
        };
        _container.Append(_renameBoxButton);

        _cancelRenameButton = new PCActionButton("Cancel");
        _cancelRenameButton.Left.Set(335, 0);
        _cancelRenameButton.Top.Set(120, 0);
        _cancelRenameButton.OnLeftClick += (_, _) =>
        {
            if (_inColorPickerMode || !_inRenameMode) return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            SoundEngine.PlaySound(SoundID.MenuClose);
            ExitRenameMode();
        };

        _leftArrowButton = new UIHoverImageButton(SmallPageButtonLeftTexture, string.Empty);
        //_leftArrowButton.SetImageScale(14f / 15f);
        _leftArrowButton.SetHoverImage(SmallestButtonHoverTexture);
        _leftArrowButton.SetVisibility(1f, 1f);
        _leftArrowButton.OnLeftClick += (_, _) =>
        {
            if (_pendingColorChange || _inRenameMode)
            {
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                {
                    Volume = 0.25f
                });
                return;
            }

            DisplayedBoxIndex--;
            if (DisplayedBoxIndex < 0) DisplayedBoxIndex = _pcService.Boxes.Count - 1;
            PopulateCustomSlots(_pcService.Boxes[DisplayedBoxIndex]);
            UpdateArrowButtonsHoverText();

            if (_inColorPickerMode) _colorPicker.SetColor(GetColorForCurrentBox(), GetDefaultColorForCurrentBox());
        };
        AddElement(_leftArrowButton, 334, 37, 28, 28, _container);

        _rightArrowButton = new UIHoverImageButton(SmallPageButtonRightTexture, string.Empty);
        //_rightArrowButton.SetImageScale(14f / 15f);
        _rightArrowButton.SetHoverImage(SmallestButtonHoverTexture);
        _rightArrowButton.SetVisibility(1f, 1f);
        _rightArrowButton.OnLeftClick += (_, _) =>
        {
            if (_pendingColorChange || _inRenameMode)
            {
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                {
                    Volume = 0.25f
                });
                return;
            }

            DisplayedBoxIndex++;
            if (DisplayedBoxIndex >= _pcService.Boxes.Count) DisplayedBoxIndex = 0;
            PopulateCustomSlots(_pcService.Boxes[DisplayedBoxIndex]);
            UpdateArrowButtonsHoverText();

            if (_inColorPickerMode) _colorPicker.SetColor(GetColorForCurrentBox(), GetDefaultColorForCurrentBox());
        };
        AddElement(_rightArrowButton, 367, 37, 28, 28, _container);

        _boxDragBar = new PCDragBar();
        AddElement(_boxDragBar, 42, 241, 282, 10, _container);

        Append(_container);

        _colorPicker = new PCColorPicker
        {
            HAlign = 0.5f
        };
        _colorPicker.OnColorChange += color =>
        {
            _boxNameText.TextColor = color;
            _pendingColorChange = color != GetColorForCurrentBox();
            if (_pendingColorChange)
            {
                _boxDragBar.Color = color;
                _changeColorButton.SetText("Save Color (*)");
            }
            else
            {
                _boxDragBar.Color = Color.Transparent;
                _changeColorButton.SetText("Save Color");
            }
        };

        _textInput = new UITextField(PCBox.MaxNameLength);
        Append(_textInput);

        /*var panel = new UIPanel();
        panel.Width.Set(200, 0);
        panel.Height.Set(200, 0);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        Append(panel);*/
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (_inRenameMode)
        {
            _boxNameText.SetText(_textInput.CurrentValue);

            if (!_textInput.IsTyping)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                ExitRenameMode();
            }
        }

        Recalculate();
    }

    public override void Recalculate()
    {
        base.Recalculate();

        // Reposition the color picker panel relative to the screen height
        _colorPicker?.Top.Set(Main.screenHeight / 2f + 60, 0f);
    }

    /// <summary>
    ///     Called when the PC interface is opened.
    /// </summary>
    public static void OnOpen()
    {
        if (InventoryParty.InPCMode) return; // Check to prevent multiple runs

        // Clean up the color picker and related UI elements
        if (_inColorPickerMode && !_pendingColorChange)
        {
            _colorPicker.Remove();
            _boxDragBar.Color = Color.Transparent;
            _changeColorButton.SetText("Change Color");
            _container.Append(_renameBoxButton);
            _renameBoxButton.SetText("Rename");
            _pendingColorChange = false;
            _inColorPickerMode = false;
        }

        // Enter PC mode in the Inventory Party UI (forces it open and enables PC interactions)
        UILoader.GetUIState<InventoryParty>().EnterPCMode();

        // Get the local player's PC storage...
        _pcService = TerramonPlayer.LocalPlayer.GetPC();

        // ...and populate the UI with the actively displayed box data
        var selectedBox = _pcService.Boxes[DisplayedBoxIndex];
        PopulateCustomSlots(selectedBox);

        // Update hover text for the arrow buttons
        UpdateArrowButtonsHoverText();
    }
    
    public static bool SilenceCloseSound { get; set; }

    /// <summary>
    ///     Called when the PC interface is closed.
    /// </summary>
    public static void OnClose()
    {
        // Exit PC mode in the Inventory Party UI (reverts to original state)
        if (InventoryParty.InPCMode)
            UILoader.GetUIState<InventoryParty>().ExitPCMode();

        // Clean up the rename mode UI elements
        ExitRenameMode();

        // Clear the PC service reference
        _pcService = null;

        // Play the PC off sound
        if (!SilenceCloseSound)
        {
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/ls_pc_off")
            {
                Volume = 0.54f
            });
        } else
        {
            SilenceCloseSound = false;
        }
    }

    public static void ResetToDefault()
    {
        DisplayedBoxIndex = 0;
        if (_inColorPickerMode)
        {
            _colorPicker.Remove();
            _boxDragBar.Color = Color.Transparent;
            _changeColorButton.SetText("Change Color");
            _container.Append(_renameBoxButton);
            _renameBoxButton.SetText("Rename");
            _pendingColorChange = false;
            _inColorPickerMode = false;
        }

        if (InventoryParty.InPCMode)
            UILoader.GetUIState<InventoryParty>().ExitPCMode();
    }

    /// <summary>
    ///     Populates the custom slots UI with Pokémon data from the given box.
    /// </summary>
    /// <param name="box">The Pokémon box to populate from.</param>
    public static void PopulateCustomSlots(PCBox box)
    {
        var boxWidth = CustomSlots.GetLength(0);
        var boxHeight = CustomSlots.GetLength(1);

        var boxIndex = 0;
        for (var column = 0; column < boxWidth; column++)
        for (var row = 0; row < boxHeight; row++)
        {
            if (boxIndex >= PCBox.Capacity) continue;
            CustomSlots[column, row].SetBoxRef(box, boxIndex);
            boxIndex++;
        }

        // Update the box name text
        _boxNameText.SetText(GetNameForCurrentBox());

        if (_boxDragBar.Color == Color.Transparent) _boxNameText.TextColor = GetColorForCurrentBox();
    }

    public static void UpdateArrowButtonsHoverText()
    {
        if (_pcService == null) return;
        var hoverText = $"{DisplayedBoxIndex + 1}/{_pcService.Boxes.Count}";
        _leftArrowButton.SetHoverText(hoverText);
        _rightArrowButton.SetHoverText(hoverText);
    }

    private static void ExitRenameMode()
    {
        if (!_inRenameMode) return;
        _container.RemoveChild(_cancelRenameButton);
        _renameBoxButton.SetText("Rename");
        _boxNameText.SetText(GetNameForCurrentBox());
        _boxNameText.ShowTypingCaret = false;
        _boxNameText.Recalculate();
        _textInput.SetNotTyping();
        _inRenameMode = false;
    }

    private static string GetNameForCurrentBox()
    {
        var name = _pcService.Boxes[DisplayedBoxIndex].GivenName;
        return string.IsNullOrEmpty(name) ? $"Box {DisplayedBoxIndex + 1}" : name;
    }

    private static void SetNameForCurrentBox(string name)
    {
        _pcService.Boxes[DisplayedBoxIndex].GivenName = string.IsNullOrEmpty(name) ? null : name;
    }

    public static Color GetColorForCurrentBox()
    {
        var color = _pcService.Boxes[DisplayedBoxIndex].Color;
        return color == Color.Transparent ? GetDefaultColorForCurrentBox() : color;
    }

    private static Color GetDefaultColorForCurrentBox()
    {
        return DefaultBoxColors[DisplayedBoxIndex % DefaultBoxColors.Length];
    }
}

internal sealed class CustomPCItemSlot : UIImage
{
    private static readonly Asset<Texture2D> PCSlotBgEmptyTexture;
    private static readonly Asset<Texture2D> PCSlotBgTexture;
    private PCBox _box;
    private int _index;

    private UIImage _minispriteImage;
    private string _tooltipName;
    private string _tooltipText;

    static CustomPCItemSlot()
    {
        PCSlotBgEmptyTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/PCSlotBgEmpty");
        PCSlotBgTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/PCSlotBg");
    }

    public CustomPCItemSlot() : base(PCSlotBgEmptyTexture)
    {
        ImageScale = 0.85f;
        AllowResizingDimensions = false;
    }

    private PokemonData Data => _box?[_index];

    public void SetBoxRef(PCBox box, int index)
    {
        _box = box;
        _index = index;
        SetData(Data);
    }

    private void SetData(PokemonData data)
    {
        _box[_index] = data;
        SetImage(data != null ? PCSlotBgTexture : PCSlotBgEmptyTexture);
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
            Append(_minispriteImage);
        }
        else
        {
            _tooltipText = null;
            _tooltipName = null;
        }
    }

    private void LeftClick()
    {
        var heldPokemon = TooltipOverlay.GetHeldPokemon(out _);
        var data = Data;
        if (data == null)
        {
            // Place the held Pokémon into the slot if present and slot is empty
            if (heldPokemon == null) return;
            SoundEngine.PlaySound(SoundID.Grab);
            SetData(heldPokemon);
            TooltipOverlay.ClearHeldPokemon();
        }
        else if (Main.mouseItem.IsAir)
        {
            // Take or swap the Pokémon from the slot if slot is not empty and player is not holding an item
            SoundEngine.PlaySound(SoundID.Grab);
            TooltipOverlay.SetHeldPokemon(data, TooltipOverlay.HeldPokemonSource.PC, d =>
            {
                // Check for free space in the box starting from the end
                var freeSpaceIndex = -1;
                for (var i = PCBox.Capacity - 1; i >= 0; i--)
                    if (_box[i] == null)
                    {
                        freeSpaceIndex = i;
                        break;
                    }

                if (freeSpaceIndex != -1)
                {
                    _box[freeSpaceIndex] = d;
                    if (PCInterface.Active) PCInterface.PopulateCustomSlots(_box);
                }
                else
                {
                    _box.Service.StorePokemon(d);
                }
            });
            data = heldPokemon;
            if (data == null)
            {
                SetData(null);
                SetImage(PCSlotBgEmptyTexture);
                _minispriteImage?.Remove();
            }
            else
            {
                SetData(data);
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var data = Data;
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering)
        {
            if (Main.mouseItem.IsAir && data != null)
            {
                if (Main.keyState.IsKeyDown(Keys.O))
                {
                    HubUI.OpenToPokemon(Data.ID, Data.IsShiny);
                    return;
                }

                TooltipOverlay.SetName(_tooltipName);
                TooltipOverlay.SetTooltip(_tooltipText);
                TooltipOverlay.SetIcon(BallAssets.GetBallIcon(data.Ball));
                if (data.IsShiny) TooltipOverlay.SetColor(ModContent.GetInstance<KeyItemRarity>().RarityColor);
            }

            if (Main.mouseLeft && Main.mouseLeftRelease)
                LeftClick();
        }

        base.Draw(spriteBatch);
    }
}

internal sealed class PCActionButton : BetterUIText
{
    public PCActionButton(string text) : base(text, 0.75f)
    {
        Height.Set(34, 0);
        TextColor = new Color(232, 232, 249);
        TextOriginX = 0f;
        TextOriginY = 0.5f;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        TextColor = Color.White;
        Tween.To(() => TextScale, SetTextScale, 1f, 1f / 12f);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        TextColor = new Color(232, 232, 249);
        Tween.To(() => TextScale, SetTextScale, 0.75f, 1f / 12f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

        base.DrawSelf(spriteBatch);
    }
}

internal sealed class PCDragBar : UIImage
{
    private static readonly Asset<Texture2D> BoxDragBarTexture;

    static PCDragBar()
    {
        BoxDragBarTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/BoxDragBar");
    }

    public PCDragBar() : base(BoxDragBarTexture)
    {
        Color = Color.Transparent;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

        var color = Color;
        Color = Color.White;
        base.DrawSelf(spriteBatch); // Draw the drag bar at Color.White
        Color = color;

        var drawRect = GetDimensions().ToRectangle();
        drawRect.X += 2;
        drawRect.Y += 2;
        drawRect.Width -= 4;
        drawRect.Height -= 4;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRect,
            color != Color.Transparent
                ? color
                : PCInterface.GetColorForCurrentBox()); // Draw the drag bar at the box color
    }
}

internal sealed class PCColorPicker : UIContainer
{
    private readonly UIColoredImageButton _copyHexButton;
    private readonly UIText _hexCodeText;
    private readonly UIColoredImageButton _pasteHexButton;
    private readonly UIColoredImageButton _randomColorButton;
    private readonly BetterUIText _resetToDefaultButton;
    private Vector3 _currentColorPickerHSL = RgbToScaledHsl(new Color());
    private Vector3 _defaultColorPickerHSL = RgbToScaledHsl(new Color());

    public PCColorPicker() : base(new Vector2(220, 180))
    {
        var invBgColor = new Color(63, 65, 151, 255) * 0.785f;

        var backPanel = new UIPanel
        {
            BackgroundColor = invBgColor
        };
        backPanel.Width.Set(220f, 0f);
        backPanel.Height.Set(137f, 0f);
        backPanel.SetPadding(0f);
        backPanel.PaddingTop = 3f;

        // Append sliders
        backPanel.Append(CreateHSLSlider(HSLSliderId.Hue));
        backPanel.Append(CreateHSLSlider(HSLSliderId.Saturation));
        backPanel.Append(CreateHSLSlider(HSLSliderId.Luminance));

        _resetToDefaultButton = new BetterUIText("Reset to Default Color", 0.8125f)
        {
            TextColor = new Color(100, 100, 100),
            TextOriginX = 0.5f,
            TextOriginY = 0.5f,
            HAlign = 0.5f
        };
        _resetToDefaultButton.Top.Set(97f, 0f);
        _resetToDefaultButton.Width.Set(160f, 0f);
        _resetToDefaultButton.Height.Set(28f, 0f);
        _resetToDefaultButton.OnMouseOver += (_, _) =>
        {
            if (IsDefaultColor) return;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Tween.To(() => _resetToDefaultButton.TextScale, _resetToDefaultButton.SetTextScale, 0.89f, 1f / 12f);
            _resetToDefaultButton.TextColor = new Color(255, 214, 102);
            _resetToDefaultButton.ShadowColor = new Color(173, 48, 46);
        };
        _resetToDefaultButton.OnMouseOut += (_, _) =>
        {
            if (IsDefaultColor) return;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Tween.To(() => _resetToDefaultButton.TextScale, _resetToDefaultButton.SetTextScale, 0.8125f, 1f / 12f);
            _resetToDefaultButton.TextColor = new Color(247, 218, 101);
            _resetToDefaultButton.ShadowColor = Color.Black;
        };
        _resetToDefaultButton.OnLeftClick += (_, _) =>
        {
            if (IsDefaultColor) return;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Tween.To(() => _resetToDefaultButton.TextScale, _resetToDefaultButton.SetTextScale, 0.8125f, 1f / 12f);
            _currentColorPickerHSL = _defaultColorPickerHSL;
            var color = ScaledHslToRgb(_defaultColorPickerHSL.X, _defaultColorPickerHSL.Y, _defaultColorPickerHSL.Z);
            OnColorChange?.Invoke(color);
            _resetToDefaultButton.TextColor = new Color(100, 100, 100);
            _resetToDefaultButton.ShadowColor = Color.Black;
            UpdateHexText(color);
        };
        backPanel.Append(_resetToDefaultButton);

        Append(backPanel);

        var hexCodePanel = new UIPanel
        {
            BackgroundColor = invBgColor
        };
        hexCodePanel.Left.Set(120f, 0f);
        hexCodePanel.Top.Set(148f, 0f);
        hexCodePanel.Width.Set(100f, 0f);
        hexCodePanel.Height.Set(32f, 0f);

        _copyHexButton = new UIColoredImageButton(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Copy"), true);
        _copyHexButton.Top.Set(148f, 0f);
        _copyHexButton.OnLeftMouseDown += Click_CopyHex;
        Append(_copyHexButton);

        _pasteHexButton =
            new UIColoredImageButton(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Paste"), true);
        _pasteHexButton.Top.Set(148f, 0f);
        _pasteHexButton.Left.Set(40f, 0f);
        _pasteHexButton.OnLeftMouseDown += Click_PasteHex;
        Append(_pasteHexButton);

        _randomColorButton =
            new UIColoredImageButton(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Randomize"), true);
        _randomColorButton.Top.Set(148f, 0f);
        _randomColorButton.Left.Set(80f, 0f);
        _randomColorButton.OnLeftMouseDown += Click_RandomizeSingleColor;
        Append(_randomColorButton);

        _hexCodeText = new UIText("#FFFFFF")
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _hexCodeText.Left.Set(-1f, 0f);
        hexCodePanel.Append(_hexCodeText);

        Append(hexCodePanel);
    }

    private bool IsDefaultColor => _currentColorPickerHSL == _defaultColorPickerHSL;

    public event Action<Color> OnColorChange;

    public Color GetColor()
    {
        return ScaledHslToRgb(_currentColorPickerHSL.X, _currentColorPickerHSL.Y, _currentColorPickerHSL.Z);
    }

    public void SetColor(Color color, Color defaultColor)
    {
        _currentColorPickerHSL = RgbToScaledHsl(color);
        _defaultColorPickerHSL = RgbToScaledHsl(defaultColor);
        _resetToDefaultButton.TextColor = !IsDefaultColor ? new Color(247, 218, 101) : new Color(100, 100, 100);
        UpdateHexText(color);
    }

    private void Click_CopyHex(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        Platform.Get<IClipboard>().Value = _hexCodeText.Text;
    }

    private void Click_PasteHex(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        var value = Platform.Get<IClipboard>().Value;
        if (!GetHexColor(value, out var hsl)) return;
        //ApplyPendingColor(ScaledHslToRgb(hsl.X, hsl.Y, hsl.Z));
        _currentColorPickerHSL = hsl;
        var color = ScaledHslToRgb(hsl.X, hsl.Y, hsl.Z);
        OnColorChange?.Invoke(color);
        _resetToDefaultButton.TextColor = !IsDefaultColor ? new Color(247, 218, 101) : new Color(100, 100, 100);
        UpdateHexText(color);
        //UpdateColorPickers();
    }

    private void Click_RandomizeSingleColor(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        var randomColorVector = GetRandomColorVector();
        //ApplyPendingColor(ScaledHslToRgb(randomColorVector.X, randomColorVector.Y, randomColorVector.Z));
        _currentColorPickerHSL = randomColorVector;
        var color = ScaledHslToRgb(randomColorVector.X, randomColorVector.Y, randomColorVector.Z);
        OnColorChange?.Invoke(color);
        _resetToDefaultButton.TextColor = !IsDefaultColor ? new Color(247, 218, 101) : new Color(100, 100, 100);
        UpdateHexText(color);
        //UpdateColorPickers();
    }

    private static Vector3 GetRandomColorVector()
    {
        return new Vector3(Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());
    }

    private UIColoredSlider CreateHSLSlider(HSLSliderId id)
    {
        var uIColoredSlider = CreateHSLSliderButtonBase(id);
        uIColoredSlider.VAlign = 0f;
        uIColoredSlider.HAlign = 0f;
        uIColoredSlider.Width = StyleDimension.FromPixelsAndPercent(-10f, 1f);
        uIColoredSlider.Top.Set(30 * (int)id, 0f);
        //uIColoredSlider.OnLeftMouseDown += Click_ColorPicker;
        uIColoredSlider.SetSnapPoint("Middle", (int)id, null, new Vector2(0f, 20f));
        return uIColoredSlider;
    }

    private UIColoredSlider CreateHSLSliderButtonBase(HSLSliderId id)
    {
        return id switch
        {
            HSLSliderId.Saturation => new UIColoredSlider(LocalizedText.Empty,
                () => GetHSLSliderPosition(HSLSliderId.Saturation),
                delegate(float x) { UpdateHSLValue(HSLSliderId.Saturation, x); }, UpdateHSL_S,
                x => GetHSLSliderColorAt(HSLSliderId.Saturation, x), Color.Transparent),
            HSLSliderId.Luminance => new UIColoredSlider(LocalizedText.Empty,
                () => GetHSLSliderPosition(HSLSliderId.Luminance),
                delegate(float x) { UpdateHSLValue(HSLSliderId.Luminance, x); }, UpdateHSL_L,
                x => GetHSLSliderColorAt(HSLSliderId.Luminance, x), Color.Transparent),
            _ => new UIColoredSlider(LocalizedText.Empty, () => GetHSLSliderPosition(HSLSliderId.Hue),
                delegate(float x) { UpdateHSLValue(HSLSliderId.Hue, x); }, UpdateHSL_H,
                x => GetHSLSliderColorAt(HSLSliderId.Hue, x), Color.Transparent)
        };
    }

    private void UpdateHSL_H()
    {
        var value = UILinksInitializer.HandleSliderHorizontalInput(_currentColorPickerHSL.X, 0f, 1f,
            PlayerInput.CurrentProfile.InterfaceDeadzoneX, 0.35f);
        UpdateHSLValue(HSLSliderId.Hue, value);
    }

    private void UpdateHSL_S()
    {
        var value = UILinksInitializer.HandleSliderHorizontalInput(_currentColorPickerHSL.Y, 0f, 1f,
            PlayerInput.CurrentProfile.InterfaceDeadzoneX, 0.35f);
        UpdateHSLValue(HSLSliderId.Saturation, value);
    }

    private void UpdateHSL_L()
    {
        var value = UILinksInitializer.HandleSliderHorizontalInput(_currentColorPickerHSL.Z, 0f, 1f,
            PlayerInput.CurrentProfile.InterfaceDeadzoneX, 0.35f);
        UpdateHSLValue(HSLSliderId.Luminance, value);
    }

    private float GetHSLSliderPosition(HSLSliderId id)
    {
        switch (id)
        {
            case HSLSliderId.Hue:
                return _currentColorPickerHSL.X;
            case HSLSliderId.Saturation:
                return _currentColorPickerHSL.Y;
            case HSLSliderId.Luminance:
                return _currentColorPickerHSL.Z;
            default:
                return 1f;
        }
    }

    private void UpdateHSLValue(HSLSliderId id, float value)
    {
        switch (id)
        {
            case HSLSliderId.Hue:
                _currentColorPickerHSL.X = value;
                break;
            case HSLSliderId.Saturation:
                _currentColorPickerHSL.Y = value;
                break;
            case HSLSliderId.Luminance:
                _currentColorPickerHSL.Z = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), id, null);
        }

        var color = ScaledHslToRgb(_currentColorPickerHSL.X, _currentColorPickerHSL.Y, _currentColorPickerHSL.Z);
        OnColorChange?.Invoke(color);
        _resetToDefaultButton.TextColor = !IsDefaultColor ? new Color(247, 218, 101) : new Color(100, 100, 100);

        UpdateHexText(color);
    }

    private static Color ScaledHslToRgb(Vector3 hsl)
    {
        return ScaledHslToRgb(hsl.X, hsl.Y, hsl.Z);
    }

    private static Color ScaledHslToRgb(float hue, float saturation, float luminosity)
    {
        return Main.hslToRgb(hue, saturation, luminosity * 0.85f + 0.15f);
    }

    private Color GetHSLSliderColorAt(HSLSliderId id, float pointAt)
    {
        return id switch
        {
            HSLSliderId.Hue => ScaledHslToRgb(pointAt, 1f, 0.5f),
            HSLSliderId.Saturation => ScaledHslToRgb(_currentColorPickerHSL.X, pointAt, _currentColorPickerHSL.Z),
            HSLSliderId.Luminance => ScaledHslToRgb(_currentColorPickerHSL.X, _currentColorPickerHSL.Y, pointAt),
            _ => Color.White
        };
    }

    private void UpdateHexText(Color pendingColor)
    {
        _hexCodeText.SetText(GetHexText(pendingColor));
    }

    private static string GetHexText(Color pendingColor)
    {
        return "#" + pendingColor.Hex3().ToUpper();
    }

    private static Vector3 RgbToScaledHsl(Color color)
    {
        var value = Main.rgbToHsl(color);
        value.Z = (value.Z - 0.15f) / 0.85f;
        return Vector3.Clamp(value, Vector3.Zero, Vector3.One);
    }

    private static bool GetHexColor(string hexString, out Vector3 hsl)
    {
        if (hexString.StartsWith('#'))
            hexString = hexString[1..];

        if (hexString.Length <= 6 &&
            uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result))
        {
            var b = result & 0xFFu;
            var g = (result >> 8) & 0xFFu;
            var r = (result >> 16) & 0xFFu;
            hsl = RgbToScaledHsl(new Color((int)r, (int)g, (int)b));
            return true;
        }

        hsl = Vector3.Zero;
        return false;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

        var text = string.Empty;
        if (_copyHexButton.IsMouseHovering)
            text = Language.GetTextValue("UI.CopyColorToClipboard");
        if (_pasteHexButton.IsMouseHovering)
            text = Language.GetTextValue("UI.PasteColorFromClipboard");
        if (_randomColorButton.IsMouseHovering)
            text = Language.GetTextValue("UI.RandomizeColor");
        if (!string.IsNullOrEmpty(text)) Main.instance.MouseText(text);

        base.Draw(spriteBatch);
    }

    private enum HSLSliderId
    {
        Hue,
        Saturation,
        Luminance
    }
}
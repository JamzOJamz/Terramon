using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
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

    public static readonly Color[] DefaultBoxColors =
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

    private static UIText _boxNameText;
    private static UIHoverImageButton _leftArrowButton;
    private static UIHoverImageButton _rightArrowButton;

    static PCInterface()
    {
        /*ListViewButtonTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/ListViewButton");
        SingleViewButtonTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SingleViewButton");
        SmallerButtonHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallerButtonHover");*/
        SmallestButtonHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallestButtonHover");
        SmallPageButtonLeftTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallPageButtonLeft");
        SmallPageButtonRightTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/SmallPageButtonRight");
    }

    public override bool Visible => TerramonPlayer.LocalPlayer.ActivePCTileEntityID != -1 &&
                                    Main.LocalPlayer.chest == -1 && !Main.recBigList;

    public static int DisplayedBoxIndex { get; private set; }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        var container = new UIContainer(new Vector2(442, 262));
        container.Left.Set(120, 0);
        container.Top.Set(306, 0);

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
            AddElement(slot, slotPositionsX[j] - 120, i * 48 - 4, 50, 50, container);
        }

        _boxNameText = new UIText("Box 1")
        {
            TextColor = DefaultBoxColors[0]
        };
        _boxNameText.Left.Set(334, 0);
        _boxNameText.Top.Set(9, 0);
        container.Append(_boxNameText);

        var changeColorButton = new PCActionButton("Change Color");
        changeColorButton.Left.Set(335, 0);
        changeColorButton.Top.Set(27, 0);
        container.Append(changeColorButton);

        var renameBoxButton = new PCActionButton("Rename");
        renameBoxButton.Left.Set(335, 0);
        renameBoxButton.Top.Set(53, 0);
        container.Append(renameBoxButton);

        _leftArrowButton = new UIHoverImageButton(SmallPageButtonLeftTexture, string.Empty);
        _leftArrowButton.SetHoverImage(SmallestButtonHoverTexture);
        _leftArrowButton.SetVisibility(1f, 1f);
        _leftArrowButton.OnLeftClick += (_, _) =>
        {
            var pcStorage = TerramonPlayer.LocalPlayer.GetPC();
            DisplayedBoxIndex--;
            if (DisplayedBoxIndex < 0) DisplayedBoxIndex = pcStorage.Boxes.Count - 1;
            PopulateCustomSlots(pcStorage.Boxes[DisplayedBoxIndex]);
            UpdateArrowButtonsHoverText(pcStorage);
        };
        AddElement(_leftArrowButton, 334, 90, 30, 30, container);

        _rightArrowButton = new UIHoverImageButton(SmallPageButtonRightTexture, string.Empty);
        _rightArrowButton.SetHoverImage(SmallestButtonHoverTexture);
        _rightArrowButton.SetVisibility(1f, 1f);
        _rightArrowButton.OnLeftClick += (_, _) =>
        {
            var pcStorage = TerramonPlayer.LocalPlayer.GetPC();
            DisplayedBoxIndex++;
            if (DisplayedBoxIndex >= pcStorage.Boxes.Count) DisplayedBoxIndex = 0;
            PopulateCustomSlots(pcStorage.Boxes[DisplayedBoxIndex]);
            UpdateArrowButtonsHoverText(pcStorage);
        };
        AddElement(_rightArrowButton, 369, 90, 30, 30, container);

        var boxDragBar = new PCDragBar();
        AddElement(boxDragBar, 42, 241, 282, 10, container);

        Append(container);

        /*var panel = new UIPanel();
        panel.Width.Set(200, 0);
        panel.Height.Set(200, 0);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        Append(panel);*/
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        Recalculate();
    }

    /// <summary>
    ///     Called when the PC interface is opened.
    /// </summary>
    public static void OnOpen()
    {
        // Enter PC mode in the Inventory Party UI (forces it open and enables PC interactions)
        UILoader.GetUIState<InventoryParty>().EnterPCMode();

        // Get the local player's PC storage...
        var pcStorage = TerramonPlayer.LocalPlayer.GetPC();

        // ...and populate the UI with the actively displayed box data
        var selectedBox = pcStorage.Boxes[DisplayedBoxIndex];
        PopulateCustomSlots(selectedBox);

        // Update hover text for the arrow buttons
        UpdateArrowButtonsHoverText(pcStorage);
    }

    /// <summary>
    ///     Called when the PC interface is closed.
    /// </summary>
    public static void OnClose()
    {
        // Exit PC mode in the Inventory Party UI (reverts to original state)
        UILoader.GetUIState<InventoryParty>().ExitPCMode();
    }

    public static void ResetToDefault()
    {
        DisplayedBoxIndex = 0;

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
        _boxNameText.SetText($"Box {DisplayedBoxIndex + 1}");
        _boxNameText.TextColor = DefaultBoxColors[DisplayedBoxIndex % DefaultBoxColors.Length];
    }

    private static void UpdateArrowButtonsHoverText(PCService pcStorage)
    {
        var hoverText = $"{DisplayedBoxIndex + 1}/{pcStorage.Boxes.Count}";
        _leftArrowButton.SetHoverText(hoverText);
        _rightArrowButton.SetHoverText(hoverText);
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
        var heldPokemon = TooltipOverlay.GetHeldPokemon();
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
            var boxCopy = _box;
            var indexCopy = _index;
            TooltipOverlay.SetHeldPokemon(data, onReturn: d =>
            {
                if (boxCopy != _box)
                {
                    if (boxCopy[indexCopy] == null)
                        boxCopy[indexCopy] = d;
                    else boxCopy.Service.StorePokemon(d);
                }
                else
                {
                    if (Data == null)
                        SetData(d);
                    else
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

internal sealed class PCDragBar() : UIImage(BoxDragBarTexture)
{
    private static readonly Asset<Texture2D> BoxDragBarTexture;

    static PCDragBar()
    {
        BoxDragBarTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/PC/BoxDragBar");
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

        base.DrawSelf(spriteBatch);

        var drawRect = GetDimensions().ToRectangle();
        drawRect.X += 2;
        drawRect.Y += 2;
        drawRect.Width -= 4;
        drawRect.Height -= 4;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRect,
            PCInterface.DefaultBoxColors[PCInterface.DisplayedBoxIndex % PCInterface.DefaultBoxColors.Length]);
    }
}
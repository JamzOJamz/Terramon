using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Core;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class PartyDisplay : SmartUIState
{
    private readonly PartySidebarSlot[] PartySlots = new PartySidebarSlot[6];
    public bool IsDraggingSlot;
    public PartySidebar Sidebar;

    public override bool Visible =>
        !Main.playerInventory && !Main.LocalPlayer.dead && TerramonPlayer.LocalPlayer.HasChosenStarter;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        Sidebar = new PartySidebar(new Vector2(200, 486))
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

    public void UpdateSlot(PokemonData data, int index)
    {
        PartySlots[index].SetData(data);
    }

    public void UpdateAllSlots(PokemonData[] partyData)
    {
        for (var i = 0; i < PartySlots.Length; i++) UpdateSlot(partyData[i], i);
    }

    public void ClearAllSlots()
    {
        for (var i = 0; i < PartySlots.Length; i++) UpdateSlot(null, i);
    }

    public void SwapSlotIndexes(int index1, int index2)
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

    public override void SafeUpdate(GameTime gameTime)
    {
        Recalculate();
    }
}

public class PartySidebar : UIContainer
{
    public PartySidebar(Vector2 size) : base(size)
    {
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
    private readonly UIText LevelText;
    private readonly UIText NameText;
    private readonly PartyDisplay PartyDisplay;
    private int _index;
    private PokemonData Data;
    private bool Dragging;
    private UIImage GenderIcon;
    private UIImage HeldItemBox;
    private bool IsHovered;
    private bool JustEndedDragging;
    private Vector2 Offset;
    private ITweener SnapTween;
    private UIImage SpriteBox;

    public PartySidebarSlot(PartyDisplay partyDisplay, int index) : base(ModContent.Request<Texture2D>(
        "Terramon/Assets/GUI/Party/SidebarClosed",
        AssetRequestMode.ImmediateLoad))
    {
        PartyDisplay = partyDisplay;
        Index = index;
        NameText = new UIText(string.Empty, 0.67f);
        NameText.Left.Set(8, 0);
        NameText.Top.Set(57, 0);
        Append(NameText);
        LevelText = new UIText(string.Empty, 0.67f);
        LevelText.Left.Set(8, 0);
        LevelText.Top.Set(10, 0);
        Append(LevelText);
    }

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
        if (IsMouseHovering && Data != null && !PartyDisplay.IsDraggingSlot)
            Main.hoverItemName = "Left click to send out\nRight click and drag to reorder";
    }

    public void PlayIndexSound()
    {
        if (ModContent.GetInstance<ClientConfig>().ReducedAudio)
            return;

        //float[] pitchTable = new float[] { -0.16f, 0, 0.16f, 0.416f, 0.583f, 0.75f };
        var s = new SoundStyle
        {
            SoundPath = "Terramon/Assets/Audio/Sounds/button_smm",
            Pitch = ((float)_index / -15) + 0.6f,
            Volume = 0.25f
        };
        SoundEngine.PlaySound(s);
    }

    public override void RightMouseDown(UIMouseEvent evt)
    {
        base.RightMouseDown(evt);
        PlayIndexSound();
        DragStart(evt);
    }

    public override void RightMouseUp(UIMouseEvent evt)
    {
        base.RightMouseUp(evt);
        DragEnd();
    }

    private void SnapPosition(int index)
    {
        if (Data == null || Dragging) return;
        SnapTween = Tween.To(() => Top.Pixels, x => Top.Pixels = x, -2 + 83 * index, 0.15f).SetEase(Ease.OutExpo);
    }

    private void DragStart(UIMouseEvent evt)
    {
        if (Data == null) return;
        PartyDisplay.Sidebar.BringSlotToTop(this);
        Offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
        Dragging = true;
        PartyDisplay.IsDraggingSlot = true;
        SnapTween?.Kill();
    }

    private void DragEnd()
    {
        if (Data == null) return;
        if (ModContent.GetInstance<ClientConfig>().ReducedAudio)
            SoundEngine.PlaySound(SoundID.Tink);
        Dragging = false;
        JustEndedDragging = true;
        PartyDisplay.IsDraggingSlot = false;
        SnapPosition(Index);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        var transparentColor = Color.White * 0.45f;
        if (PartyDisplay.IsDraggingSlot)
        {
            if (!Dragging)
            {
                if (Data == null) return;
                Color = transparentColor;
                NameText.TextColor = transparentColor;
                LevelText.TextColor = transparentColor;
                HeldItemBox.Color = transparentColor;
                SpriteBox.Color = transparentColor;
                ((UIImage)SpriteBox.Children.ElementAt(0)).Color = transparentColor;
                GenderIcon.Color = transparentColor;
                return;
            }

            var i = TerramonPlayer.LocalPlayer.NextFreePartyIndex() - 1;
            if (i == -2) i = 6;
            var bottomScrollMax = 71 * i + 12 * i - 2;
            var yOff = Math.Max(Math.Min(Main.mouseY - Offset.Y, bottomScrollMax), -2);
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
            NameText.TextColor = Color.White;
            LevelText.TextColor = Color.White;
            HeldItemBox.Color = Color.White;
            SpriteBox.Color = Color.White;
            ((UIImage)SpriteBox.Children.ElementAt(0)).Color = Color.White;
            GenderIcon.Color = Color.White;
        }

        if (IsMouseHovering && !PartyDisplay.IsDraggingSlot)
        {
            if (Data == null) return;
            Main.LocalPlayer.mouseInterface = true;
            if (IsHovered) return;
            IsHovered = true;
            if (!JustEndedDragging) SoundEngine.PlaySound(SoundID.MenuTick);
            UpdateSprite(true);
        }
        else
        {
            if (Data == null || !IsHovered) return;
            IsHovered = false;
            JustEndedDragging = false;
            UpdateSprite();
        }
    }

    public void UpdateSprite(bool selected = false)
    {
        string spritePath = "Terramon/Assets/GUI/Party/SidebarOpen";
        if (Data != null)
        {
            if (selected)
                spritePath += "_Selected";
        }

        SetImage(ModContent.Request<Texture2D>(spritePath,
        AssetRequestMode.ImmediateLoad));
    }

    public void SetData(PokemonData data)
    {
        Data = data;
        if (data == null)
        {
            
            HeldItemBox?.Remove();
            SpriteBox?.Remove();
            GenderIcon?.Remove();
            NameText.SetText(string.Empty);
            LevelText.SetText(string.Empty);
        }
        else
        {
            UpdateSprite();
            NameText.SetText(Terramon.Database.GetLocalizedPokemonName(data.ID).Value);
            LevelText.SetText("Lv. " + data.Level);
            HeldItemBox = new UIImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/HeldItemBox",
                AssetRequestMode.ImmediateLoad));
            HeldItemBox.Top.Set(25, 0f);
            HeldItemBox.Left.Set(8, 0f);
            SpriteBox = new UIImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/SpriteBox",
                AssetRequestMode.ImmediateLoad));
            SpriteBox.Top.Set(10, 0f);
            SpriteBox.Left.Set(59, 0f);
            var sprite = new UIImage(ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.Database.GetPokemonName(data.ID)}_Mini",
                AssetRequestMode.ImmediateLoad))
            {
                ImageScale = 0.7f
            };
            sprite.Top.Set(-12, 0f);
            sprite.Left.Set(-20, 0f);
            SpriteBox.Append(sprite);
            var genderIconPath = data.Gender != GenderID.Unknown
                ? $"Terramon/Assets/GUI/Party/Icon{(data.Gender == GenderID.Male ? "Male" : "Female")}"
                : "Terramon/Assets/Empty";
            GenderIcon = new UIBlendedImage(ModContent.Request<Texture2D>(genderIconPath,
                AssetRequestMode.ImmediateLoad));
            GenderIcon.Top.Set(57, 0f);
            GenderIcon.Left.Set(87, 0f);
            Append(HeldItemBox);
            Append(SpriteBox);
            Append(GenderIcon);
        }
    }
}
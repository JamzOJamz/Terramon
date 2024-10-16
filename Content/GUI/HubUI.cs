using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items.KeyItems;
using Terramon.Content.NPCs;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

public class HubUI : SmartUIState
{
    private static readonly Asset<Texture2D> EmptyTabHeaderTexture;
    private static readonly Asset<Texture2D> EmptyTabHeaderAltTexture;
    private static readonly Asset<Texture2D> PokedexTabHeaderTexture;
    private static readonly Asset<Texture2D> PokedexTabIconTexture;
    private static readonly Asset<Texture2D> BallIconTexture;
    private static readonly Asset<Texture2D> BallIconEmptyTexture;
    private static readonly Asset<Texture2D> PlayerDexFilterTexture;
    private static readonly Asset<Texture2D> PlayerDexFilterHoverTexture;
    private static readonly Asset<Texture2D> WorldDexFilterTexture;
    private static readonly Asset<Texture2D> WorldDexFilterHoverTexture;

    private static bool _playerInventoryOpen;

    private static bool _worldDexMode;
    private UIText _caughtAmountText;
    private UIPanel _caughtSeenPanel;
    private UIHoverImageButton _filterButton;
    private UIPanel _mainPanel;
    private PokedexOverviewPanel _overviewPanel;

    private float _pokedexCompletionPercentage;
    private PokedexPageDisplay _pokedexPage;
    private UIPanel _progressPanel;
    private UIText _progressText;
    private UIPanel _rangePanel;
    private UIText _rangeText;
    private UIText _seenAmountText;
    private UIHoverImage _seenBallIcon;

    static HubUI()
    {
        EmptyTabHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyTabHeader");
        EmptyTabHeaderAltTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyTabHeaderAlt");
        PokedexTabHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexTabHeader");
        PokedexTabIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexTabIcon");
        BallIconTexture = ModContent.Request<Texture2D>("Terramon/icon_small");
        BallIconEmptyTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyBallIcon");
        PlayerDexFilterTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PlayerDexFilter");
        PlayerDexFilterHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PlayerDexFilterHover");
        WorldDexFilterTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/WorldDexFilter");
        WorldDexFilterHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/WorldDexFilterHover");

        // Prevents the player from closing the inventory while the hub UI is active, instead closing the hub UI itself
        On_Player.ToggleInv += static (orig, self) =>
        {
            if (Active)
            {
                SetActive(false);
                return;
            }

            orig(self);
        };
    }

    public static bool Active { get; private set; }
    public override bool Visible => false; // Vanilla will update/draw this state through IngameFancyUI

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    }

    public static void SetActive(bool active)
    {
        if (Active == active) return;
        Active = active;
        var hubState = UILoader.GetUIState<HubUI>();
        hubState.RefreshPokedex(closeOverview: !active);
        if (active)
        {
            _playerInventoryOpen = Main.playerInventory;
            IngameFancyUI.OpenUIState(hubState);
        }
        else
        {
            IngameFancyUIClose();
            Main.playerInventory = _playerInventoryOpen;
        }

        SoundEngine.PlaySound(new SoundStyle(active ? "Terramon/Sounds/dex_open" : "Terramon/Sounds/dex_close")
        {
            Volume = 0.48f
        });
    }

    public static void ToggleActive()
    {
        SetActive(!Active);
    }

    public override void OnInitialize()
    {
        _mainPanel = new UIPanel();
        _mainPanel.Width.Set(900f, 0);
        _mainPanel.Height.Set(834f, 0);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.Top.Set(157f, 0f); // -834f + -24f + -50f + -15f
        _mainPanel.BackgroundColor = new Color(34, 42, 79) * 0.85f;

        #region Tabs

        // Pokédex tab
        var pokedexTabHeader = new HubTabHeader("Pokédex", 93, new Vector2(51, 15), PokedexTabHeaderTexture,
            PokedexTabIconTexture);
        AddElement(pokedexTabHeader, -12, -12, 227, 66, _mainPanel);

        // First ??? tab
        var emptyTabHeader = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderTexture,
            null);
        AddElement(emptyTabHeader, 213, -12, 226, 66, _mainPanel);

        // Second ??? tab
        var emptyTabHeader2 = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderTexture,
            null);
        AddElement(emptyTabHeader2, 437, -12, 226, 66, _mainPanel);

        // Third ??? tab
        var emptyTabHeader3 = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderAltTexture,
            null);
        AddElement(emptyTabHeader3, 661, -12, 227, 66, _mainPanel);

        #endregion

        #region Pokédex Content

        // Populate the Pokémon list
        _pokedexPage = new PokedexPageDisplay(470, 694, 9, 6)
        {
            Left = { Pixels = 6 },
            Top = { Pixels = 114 }
        };

        // Page buttons for the Pokédex
        var pageButtonLeft = new PokedexPageButton(_pokedexPage, false);
        AddElement(pageButtonLeft, 6, 65, 38, 38, _mainPanel);

        var pageButtonRight = new PokedexPageButton(_pokedexPage, true);
        AddElement(pageButtonRight, 53, 65, 38, 38, _mainPanel);

        // Range panel (amount of Pokémon shown on the current page)
        _rangePanel = new UIPanel
        {
            BackgroundColor = new Color(37, 49, 90) * 0.95f
        };
        _rangeText = new UIText($"Showing No. 1-54 ({Terramon.LoadedPokemonCount})", 0.92f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            PaddingLeft = 14,
            PaddingRight = 14
        };
        _rangePanel.Append(_rangeText);
        AddElement(_rangePanel, 104, 65, (int)_rangeText.MinWidth.Pixels, 38, _mainPanel);

        // Filter button
        _filterButton = new UIHoverImageButton(PlayerDexFilterTexture, "Show player Pokédex")
        {
            Left = { Pixels = -38 - 6, Percent = 1f },
            Top = { Pixels = 65 },
            Width = { Pixels = 38 },
            Height = { Pixels = 38 }
        };
        _filterButton.SetHoverImage(PlayerDexFilterHoverTexture, false);
        _filterButton.SetVisibility(1f, 1f);
        _filterButton.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            _worldDexMode = !_worldDexMode;
            if (_worldDexMode) _filterButton.SetHoverText("Show World Dex");
            _filterButton.SetImage(_worldDexMode ? WorldDexFilterTexture : PlayerDexFilterTexture);
            _filterButton.SetHoverImage(_worldDexMode ? WorldDexFilterHoverTexture : PlayerDexFilterHoverTexture);
            RefreshPokedex(closeOverview: true);
        };
        _mainPanel.Append(_filterButton);

        // Pokémon caught/seen counter
        _caughtSeenPanel = new UIPanel
        {
            BackgroundColor = new Color(37, 49, 90) * 0.95f
        };
        _caughtSeenPanel.SetPadding(0);
        _caughtSeenPanel.PaddingLeft = 13;
        _caughtSeenPanel.PaddingRight = 13;
        var caughtBallIcon = new UIHoverImage(BallIconTexture, "Obtained")
        {
            Top = { Pixels = 4 },
            Left = { Pixels = -3 },
            Width = { Pixels = 30 },
            Height = { Pixels = 30 }
        };
        caughtBallIcon.OnMouseOver += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            PokedexEntryIcon.HighlightedStatus = PokedexEntryStatus.Registered;
        };
        caughtBallIcon.OnMouseOut += (_, _) => { PokedexEntryIcon.HighlightedStatus = null; };
        _caughtSeenPanel.Append(caughtBallIcon);
        _caughtAmountText = new UIText("0", 0.92f)
        {
            Left = { Pixels = 32 },
            VAlign = 0.5f,
            IgnoresMouseInteraction = true
        };
        _caughtSeenPanel.Append(_caughtAmountText);
        _seenBallIcon = new UIHoverImage(BallIconEmptyTexture, "Seen")
        {
            Top = { Pixels = 4 },
            Left = { Pixels = (int)_caughtAmountText.MinWidth.Pixels + 41 },
            Width = { Pixels = 30 },
            Height = { Pixels = 30 }
        };
        _seenBallIcon.OnMouseOver += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            PokedexEntryIcon.HighlightedStatus = PokedexEntryStatus.Seen;
        };
        _seenBallIcon.OnMouseOut += (_, _) => { PokedexEntryIcon.HighlightedStatus = null; };
        _caughtSeenPanel.Append(_seenBallIcon);
        _seenAmountText = new UIText("0", 0.92f)
        {
            Left = { Pixels = (int)_caughtAmountText.MinWidth.Pixels + 76 },
            VAlign = 0.5f,
            IgnoresMouseInteraction = true
        };
        _caughtSeenPanel.Append(_seenAmountText);
        var caughtSeenPanelWidth = 13 + 32 + (int)_caughtAmountText.MinWidth.Pixels + 41 + 5 +
                                   (int)_seenAmountText.MinWidth.Pixels + 13;
        AddElement(_caughtSeenPanel, 490, 65, caughtSeenPanelWidth, 38, _mainPanel);

        // Progress panel (completion percentage)
        _progressPanel = new UIPanel
        {
            BackgroundColor = new Color(37, 49, 90) * 0.95f
        };
        _progressText = new UIText("0% Completion", 0.92f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            PaddingLeft = 14,
            PaddingRight = 14
        };
        _progressPanel.Append(_progressText);
        var minWidth = (int)_progressText.MinWidth.Pixels;
        var fillWidth = 380 - caughtSeenPanelWidth - 38 - 14 - 14;
        var useWidth = minWidth > fillWidth ? minWidth : fillWidth;
        AddElement(_progressPanel, 490 + caughtSeenPanelWidth + 14, 65, useWidth, 38,
            _mainPanel);

        _mainPanel.Append(_pokedexPage);

        // Pokémon overview panel
        _overviewPanel = new PokedexOverviewPanel
        {
            Left = { Pixels = 490 },
            Top = { Pixels = 114 }
        };
        _mainPanel.Append(_overviewPanel);

        #endregion

        Append(_mainPanel);

        var backPanel = new UITextPanel<LocalizedText>(Language.GetText("UI.Back"), 0.7f, true)
        {
            HAlign = 0.5f
        };
        backPanel.Width.Set(440f, 0);
        backPanel.Height.Set(50f, 0);
        backPanel.Top.Set(-74f, 1f);
        backPanel.OnMouseOver += FadedMouseOver;
        backPanel.OnMouseOut += FadedMouseOut;
        backPanel.OnLeftClick += (_, _) => { SetActive(false); };
        backPanel.SetSnapPoint("Back", 0);

        Append(backPanel);
    }

    private static void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ((UIPanel)evt.Target).BackgroundColor = new Color(73, 94, 171);
        ((UIPanel)evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    private static void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        ((UIPanel)evt.Target).BackgroundColor = new Color(63, 82, 151) * 0.8f;
        ((UIPanel)evt.Target).BorderColor = Color.Black;
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        // Recalculate inventory slots even if it is not visible
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (!inventoryParty.Visible) inventoryParty.Recalculate();

        // Update the progress text color based on the completion percentage (must be set every frame to function properly)
        _progressText.TextColor = _pokedexCompletionPercentage == 100
            ? ModContent.GetInstance<KeyItemRarity>().RarityColor
            : Color.White;

        // Update the Pokédex filter button's hover text
        if (!_worldDexMode) _filterButton.SetHoverText($"Show {Main.LocalPlayer.name}'s Pokédex");

        Recalculate();
    }

    public void RefreshPokedex(ushort pokemon = 0, bool closeOverview = false)
    {
        // Close the Pokémon overview panel if requested
        if (closeOverview)
            _overviewPanel.SetCurrentEntry(0);

        // Get the player's Pokédex
        var pokedex =
            _worldDexMode
                ? TerramonWorld.GetWorldDex()
                : TerramonPlayer.LocalPlayer.GetPokedex(); // Neither should not be null here

        // Update the Pokémon page entries
        if (pokemon == 0)
            _pokedexPage.UpdateAllEntries(pokedex);
        else
            _pokedexPage.UpdateEntry(pokemon, pokedex.Entries.GetValueOrDefault(pokemon));

        if (pokemon == 0)
        {
            // Update the range text
            var range = _pokedexPage.GetPageRange();
            _rangeText.SetText($"Showing No. {range.Item1}-{range.Item2} ({Terramon.LoadedPokemonCount})");
            _rangePanel.Width.Set((int)_rangeText.MinWidth.Pixels, 0f);
        }

        // Update the caught/seen amount and completion percentage
        var seenCount = pokedex.SeenCount;
        var registeredCount = pokedex.RegisteredCount;
        _seenAmountText.SetText((registeredCount + seenCount).ToString());
        _caughtAmountText.SetText(registeredCount.ToString());
        var completion = pokedex.RegisteredCount * 100f / Terramon.LoadedPokemonCount;
        if (completion > 100) completion = 100; // Just in case
        _progressText.SetText($"{completion:0.##}% Completion", 0.92f, false);
        _progressText.TextColor = completion == 100 ? ModContent.GetInstance<KeyItemRarity>().RarityColor : Color.White;
        _pokedexCompletionPercentage = completion;

        // Update positions and sizes of dynamic elements
        _seenBallIcon.Left.Set((int)_caughtAmountText.MinWidth.Pixels + 41, 0f);
        _seenAmountText.Left.Set((int)_caughtAmountText.MinWidth.Pixels + 76, 0f);
        var caughtSeenPanelWidth = 13 + 32 + (int)_caughtAmountText.MinWidth.Pixels + 41 + 5 +
                                   (int)_seenAmountText.MinWidth.Pixels + 13;
        _caughtSeenPanel.Width.Set(caughtSeenPanelWidth, 0f);
        _progressPanel.Left.Set(490 + caughtSeenPanelWidth + 14, 0f);
        var minWidth = (int)_progressText.MinWidth.Pixels;
        var fillWidth = 380 - caughtSeenPanelWidth - 38 - 14 - 14;
        if (minWidth > fillWidth)
        {
            var diff = minWidth - fillWidth;
            // Scale the progress text to fit the new width
            _progressText.SetText($"{completion:0.##}% Completion", 0.92f - diff / 300f, false);
        }

        _progressPanel.Width.Set(fillWidth, 0f);

        Recalculate();
    }

    public void ResetPokedex()
    {
        _pokedexPage.PageIndex = 0; // Resets the Pokédex page to the first page for the next time it is opened
        _worldDexMode = false; // Reset to player's Pokédex mode
        _filterButton.SetImage(PlayerDexFilterTexture);
        _filterButton.SetHoverImage(PlayerDexFilterHoverTexture);
        RefreshPokedex();
    }

    public void SetCurrentPokedexEntry(ushort pokemon)
    {
        _overviewPanel.SetCurrentEntry(pokemon);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        // Resize the main panel based on screen's height
        var mainPanelHeight = Main.screenHeight - 89f - 157f;
        _mainPanel?.Height.Set(mainPanelHeight, 0f);

        // Resize the overview panel based on the main panel's height
        _overviewPanel?.Height.Set(mainPanelHeight - 140, 0f);

        // Resize the Pokédex page display based on the main panel's height
        if (_pokedexPage == null || _pokedexPage.Height.Pixels == mainPanelHeight - 140) return;
        _pokedexPage.Height.Set(mainPanelHeight - 140, 0f);
        _pokedexPage.Reset(true);
        if (Active) RefreshPokedex();
    }

    /// <summary>
    ///     The same as <see cref="IngameFancyUI.Close" /> but does not play the <see cref="SoundID.MenuClose" /> sound.
    /// </summary>
    private static void IngameFancyUIClose()
    {
        Main.inFancyUI = false;
        //SoundEngine.PlaySound(SoundID.MenuClose); // Commented out to prevent the sound from playing
        var flag = !Main.gameMenu;
        var flag2 = Main.InGameUI.CurrentState is not UIVirtualKeyboard;
        var flag3 = false;
        var keyboardContext = UIVirtualKeyboard.KeyboardContext;
        if ((uint)(keyboardContext - 2) <= 1u)
            flag3 = true;

        if (flag && !(flag2 || flag3))
            flag = false;

        if (flag)
            Main.playerInventory = true;

        if (!Main.gameMenu && Main.InGameUI.CurrentState is UIEmotesMenu)
            Main.playerInventory = false;

        Main.LocalPlayer.releaseInventory = false;
        Main.InGameUI.SetState(null);
        UILinkPointNavigator.Shortcuts.FANCYUI_SPECIAL_INSTRUCTIONS = 0;
    }
}

internal sealed class HubTabHeader : UIImage
{
    private readonly Asset<Texture2D> _headerAsset;
    private readonly UIImage _icon;
    private readonly Asset<Texture2D> _iconAsset;
    private readonly bool _locked;
    private bool _headerAssetLoaded;
    private bool _iconAssetLoaded;

    public HubTabHeader(string title, float headerX, Vector2 iconPos, Asset<Texture2D> headerTexture,
        Asset<Texture2D> iconTexture) : base(headerTexture)
    {
        _locked = title == "???";
        _headerAsset = headerTexture;
        _iconAsset = iconTexture;

        var titleText = new BetterUIText(title, 0.485f, true)
        {
            VAlign = 0.5f,
            ShadowSpread = 2f
        };
        titleText.Left.Set(headerX, 0f);
        Append(titleText);

        if (iconTexture == null) return;

        _icon = new UIImage(iconTexture);
        _icon.Left.Set(iconPos.X, 0f);
        _icon.Top.Set(iconPos.Y, 0f);
        Append(_icon);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (!_headerAssetLoaded && _headerAsset.IsLoaded)
        {
            // Force the dimensions to be recalculated
            SetImage(_headerAsset);
            Recalculate();
            _headerAssetLoaded = true;
        }

        if (_iconAsset != null && !_iconAssetLoaded && _iconAsset.IsLoaded)
        {
            // Force the dimensions to be recalculated
            _icon.SetImage(_iconAsset);
            _icon.Recalculate();
            _iconAssetLoaded = true;
        }

        base.DrawSelf(spriteBatch);

        if (!_locked || !ContainsPoint(Main.MouseScreen)) return;
        var cursorText = Language.GetTextValue("Mods.Terramon.GUI.Starter.ComingSoon");
        Main.instance.MouseText(cursorText);
    }
}

internal sealed class PokedexPageDisplay : UIElement
{
    private const int ButtonSize = 70;

    private readonly List<PokedexEntryIcon> _entries = [];

    private int _cols;

    private int _pageIndex;
    private int _rows;
    private int _startItemIndex;

    public PokedexPageDisplay(int width, int height, int rows, int cols)
    {
        _rows = rows;
        _cols = cols;

        Width.Set(width, 0);
        Height.Set(height, 0);

        Reset();
    }

    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            if (value == 0)
            {
                _startItemIndex = 0;
                _pageIndex = 0;
                Reset();
                return;
            }

            var pageDiff = value - _pageIndex;
            if (pageDiff == 0) return;
            _startItemIndex += pageDiff * _rows * _cols;
            if (_startItemIndex < 0) _startItemIndex = 0;
            if (_startItemIndex > Terramon.LoadedPokemonCount - 1)
                _startItemIndex = Terramon.LoadedPokemonCount - _rows * _cols;
            _pageIndex = value;
            Reset();
        }
    }

    public Tuple<int, int> GetPageRange()
    {
        return Tuple.Create(_startItemIndex + 1,
            Math.Min(_startItemIndex + _rows * _cols, Terramon.LoadedPokemonCount));
    }

    public void Reset(bool changedHeight = false)
    {
        // Delete all the current entries
        foreach (var entry in _entries)
            entry.Remove();
        _entries.Clear();

        // Calculate row count based on the button size and the height of the page
        _rows = (int)Height.Pixels / (ButtonSize + 4);
        _cols = (int)Width.Pixels / (ButtonSize + 4);

        // Calculate the spacing between the buttons based on the size of the buttons and the size of the page
        var buttonSpacingX = (Width.Pixels - _cols * ButtonSize) / (_cols - 1);
        var buttonSpacingY = (Height.Pixels - _rows * ButtonSize) / (_rows - 1);

        // Add the new entries
        for (var i = 0; i < _rows * _cols; i++)
        {
            var row = i / _cols;
            var col = i % _cols;

            var pokemon = Terramon.DatabaseV2.Pokemon.Keys.ElementAtOrDefault(i + _startItemIndex);
            if (pokemon is 0 || pokemon > Terramon.LoadedPokemonCount) break;

            var monButton = new PokedexEntryIcon(pokemon)
            {
                Left = { Pixels = col * (ButtonSize + buttonSpacingX) },
                Top = { Pixels = row * (ButtonSize + buttonSpacingY) }
            };

            _entries.Add(monButton);

            Append(monButton);
        }
    }

    public void UpdateAllEntries(PokedexService pokedex)
    {
        foreach (var entryIcon in _entries)
        {
            var pokemon = entryIcon.ID;
            if (pokemon == 0) continue;
            var entryData = pokedex.Entries.GetValueOrDefault(pokemon);
            if (entryData == null) continue;
            entryIcon.SetData(entryData);
        }
    }

    public void UpdateEntry(ushort pokemon, PokedexEntry entry)
    {
        var entryIcon = _entries.FirstOrDefault(icon => icon.ID == pokemon);
        entryIcon?.SetData(entry);
    }
}

internal sealed class PokedexEntryIcon : UIPanel
{
    private const int IconSize = 70;
    private static readonly Asset<Texture2D> QuestionMarkTexture;
    private static readonly Asset<Texture2D> HoverTexture;
    private readonly UIText _debugText;

    private readonly UIImage _icon;

    private PokedexEntry _entry;

    private object _hoverText = "???";

    static PokedexEntryIcon()
    {
        QuestionMarkTexture = ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Icon_Locked");
        HoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexEntryIconHover");
    }

    public PokedexEntryIcon(ushort pokemon = 0)
    {
        ID = pokemon;
        Width.Set(IconSize, 0f);
        Height.Set(IconSize, 0f);
        //BackgroundColor = new Color(50, 62, 114);
        BackgroundColor = new Color(34, 42, 79);
        OnMouseOver += FadedMouseOver;
        OnMouseOut += FadedMouseOut;
        OnLeftClick += (_, _) =>
        {
            if (_entry?.Status == PokedexEntryStatus.Undiscovered) return;
            UILoader.GetUIState<HubUI>().SetCurrentPokedexEntry(ID);
        };
        /*var miniTexture =
            ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(pokemon)}_Mini");
        var icon = new UIImage(miniTexture)
        {
            Left = { Pixels = -18 },
            Top = { Pixels = -6 },
            Width = { Pixels = 80 },
            Height = { Pixels = 60 }
        };
        Append(icon);*/
        _icon = new UIImage(QuestionMarkTexture)
        {
            Left = { Pixels = 11 },
            Top = { Pixels = 6 },
            Width = { Pixels = 24 },
            Height = { Pixels = 34 },
            Color = Color.White * 0.5f,
            IgnoresMouseInteraction = true
        };
        Append(_icon);
        _debugText = new UIText(pokemon.ToString(), 0.67f)
        {
            Left = { Pixels = -5 },
            Top = { Pixels = -5 },
            IgnoresMouseInteraction = true
        };
        Append(_debugText);
    }

    public static PokedexEntryStatus? HighlightedStatus { get; set; }

    public ushort ID { get; }

    private static void FadedMouseOver(UIEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ((UIPanel)evt.Target).BorderColor = new Color(233, 176, 0);
    }

    private static void FadedMouseOut(UIEvent evt, UIElement listeningElement)
    {
        ((UIPanel)evt.Target).BorderColor = Color.Black;
    }

    public void SetData(PokedexEntry entry)
    {
        _entry = entry;
        if (entry.Status == PokedexEntryStatus.Undiscovered)
        {
            _icon.SetImage(QuestionMarkTexture);
            _icon.Left.Pixels = 11;
            _icon.Top.Pixels = 6;
            _icon.Width.Pixels = 24;
            _icon.Height.Pixels = 34;
            _icon.Color = Color.White * 0.5f;
            BackgroundColor = new Color(34, 42, 79);
            _hoverText = "???";
            return;
        }

        var miniTexture =
            ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(ID)}_Mini");
        _icon.SetImage(miniTexture);
        _icon.Left.Pixels = -18;
        _icon.Top.Pixels = -6;
        _icon.Width.Pixels = 80;
        _icon.Height.Pixels = 60;
        _icon.Color = entry.Status == PokedexEntryStatus.Registered ? Color.White : new Color(127, 127, 127, 245);
        BackgroundColor = entry.Status == PokedexEntryStatus.Registered
            ? new Color(62, 76, 151)
            : new Color(50, 62, 114);
        _hoverText = Terramon.DatabaseV2.GetLocalizedPokemonName(ID);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Main.keyState.IsKeyDown(Keys.LeftShift))
        {
            _debugText.TextColor = Color.Transparent;
            _debugText.ShadowColor = Color.Transparent;
        }
        else
        {
            _debugText.TextColor = Color.White;
            _debugText.ShadowColor = Color.Black;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (BorderColor != Color.Black || HighlightedStatus == _entry?.Status)
        {
            // Draw the hover texture
            var dimensions = GetDimensions();
            spriteBatch.Draw(HoverTexture.Value, dimensions.Position(), Color.White);
        }

        if (!IsMouseHovering) return;
        var useHoverText = _hoverText.ToString();
        if (!string.IsNullOrEmpty(_entry?.LastUpdatedBy))
            switch (_entry.Status)
            {
                case PokedexEntryStatus.Registered:
                    useHoverText += $"\n[c/FFE745:Obtained by {_entry.LastUpdatedBy}]";
                    break;
                case PokedexEntryStatus.Seen:
                    useHoverText += $"\n[c/FFE745:Seen by {_entry.LastUpdatedBy}]";
                    break;
                case PokedexEntryStatus.Undiscovered:
                default:
                    break;
            }

        Main.instance.MouseText(useHoverText);
    }
}

internal sealed class PokedexPageButton : UIHoverImageButton
{
    private static readonly Asset<Texture2D> PageButtonLeftTexture;
    private static readonly Asset<Texture2D> PageButtonLeftHoverTexture;
    private static readonly Asset<Texture2D> PageButtonRightTexture;
    private static readonly Asset<Texture2D> PageButtonRightHoverTexture;

    private readonly PokedexPageDisplay _pageDisplay;
    private readonly bool _right;

    static PokedexPageButton()
    {
        PageButtonLeftTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonLeft");
        PageButtonLeftHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonLeftHover");
        PageButtonRightTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonRight");
        PageButtonRightHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonRightHover");
    }

    /// <summary>
    ///     Creates a new page button that controls a <see cref="PokedexPageDisplay" />. Allows the player to navigate between
    ///     pages in the GUI.
    /// </summary>
    /// <param name="pageDisplay">The <see cref="PokedexPageDisplay" /> that this button will control.</param>
    /// <param name="right">
    ///     Whether this button is the right button or not. This controls whether the button will navigate to
    ///     the next or previous page.
    /// </param>
    public PokedexPageButton(PokedexPageDisplay pageDisplay, bool right) : base(
        right ? PageButtonRightTexture : PageButtonLeftTexture, string.Empty)
    {
        _pageDisplay = pageDisplay;
        _right = right;
        SetHoverImage(right ? PageButtonRightHoverTexture : PageButtonLeftHoverTexture, false);
        SetVisibility(1f, 1f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        var currentRange = _pageDisplay.GetPageRange();
        if ((!_right && currentRange.Item1 == 1) ||
            (_right && currentRange.Item2 == Terramon.LoadedPokemonCount)) return;

        _pageDisplay.PageIndex += _right.ToDirectionInt(); // -1 for left, 1 for right
        UILoader.GetUIState<HubUI>().RefreshPokedex();

        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/dex_pageup")
        {
            Volume = 0.325f
        });
    }
}

internal sealed class PokedexOverviewPanel : UIPanel
{
    private static readonly Asset<Texture2D> OverviewHeaderTexture;
    private readonly UIText _dexNoText;
    private readonly UIImage _header;
    private readonly BetterUIText _monNameText;
    private readonly PokedexPreviewCanvas _preview;

    static PokedexOverviewPanel()
    {
        OverviewHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexOverviewHeader");
    }

    public PokedexOverviewPanel()
    {
        Width.Set(380, 0);
        Height.Set(694, 0);
        BackgroundColor = new Color(37, 49, 90);
        SetPadding(0);
        _header = new UIImage(OverviewHeaderTexture)
        {
            Width = { Percent = 1f },
            Height = { Pixels = 62 },
            Color = Color.Transparent
        };
        Append(_header);
        _dexNoText = new UIText(string.Empty, 0.84f)
        {
            Left = { Pixels = 10 },
            Top = { Pixels = 8 }
        };
        Append(_dexNoText);
        _monNameText = new BetterUIText(string.Empty, 0.46f, true)
        {
            HAlign = 0.48f,
            Top = { Pixels = 22 }
        };
        Append(_monNameText);
        _preview = new PokedexPreviewCanvas
        {
            Left = { Pixels = 49 },
            Top = { Pixels = 76 }
        };
        Append(_preview);
        var overviewList = new UIList
        {
            Width = { Percent = 1f },
            Height = { Percent = 1f },
            ListPadding = 10f
        };
        Append(overviewList);
        var overviewScroll = new UIScrollbar();
        overviewScroll.SetView(100f, 1000f);
        overviewList.SetScrollbar(overviewScroll);
    }

    public void SetCurrentEntry(ushort pokemon)
    {
        if (pokemon == 0)
        {
            _dexNoText.SetText(string.Empty);
            _monNameText.SetText(string.Empty);
            _header.Color = Color.Transparent;
            _preview.IDToDraw = 0;
            return;
        }

        _dexNoText.SetText($"No. {pokemon}");
        _monNameText.SetText(Terramon.DatabaseV2.GetLocalizedPokemonName(pokemon));
        _header.Color = Color.White;
        _preview.IDToDraw = pokemon;
    }
}

internal sealed class PokedexPreviewCanvas : UIImage
{
    private static readonly Asset<Texture2D> OverviewPreviewTexture;

    private NPC _dummyNPCForDrawing;

    private ushort _idToDraw;

    static PokedexPreviewCanvas()
    {
        OverviewPreviewTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexOverviewPreview");
    }

    public PokedexPreviewCanvas() : base(OverviewPreviewTexture)
    {
        Width.Set(274, 0);
        Height.Set(138, 0);
        Color = Color.Transparent;
    }

    public ushort IDToDraw
    {
        set
        {
            _idToDraw = value;
            Color = value == 0 ? Color.Transparent : Color.White;
            if (value == 0 || !PokemonEntityLoader.IDToNPCType.TryGetValue(value, out var type))
                return;
            _dummyNPCForDrawing = new NPC
            {
                IsABestiaryIconDummy = true
            };
            _dummyNPCForDrawing.SetDefaults_ForNetId(type, 1);
            _dummyNPCForDrawing.netID = type;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (_idToDraw == 0) return;
        _dummyNPCForDrawing.FindFrame();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (_idToDraw == 0) return;

        var position = GetOuterDimensions().Position();
        position.X += (int)(Width.Pixels / 2 - _dummyNPCForDrawing.width / 2f);
        position.Y += (int)(Height.Pixels - _dummyNPCForDrawing.height - 16);
        if (_dummyNPCForDrawing.GetGlobalNPC<NPCWanderingHoverBehaviour>()
            .Enabled) // Draw the NPC a bit higher if it is a flying Pokémon
            position.Y -= 6;
        Main.instance.DrawNPCDirect(spriteBatch, _dummyNPCForDrawing, false, -position);
    }
}
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Content.NPCs;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;
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
    private static readonly Asset<Texture2D> WorldDexFilterTexture;
    private static readonly Asset<Texture2D> PlayerShinyDexFilterTexture;
    public static readonly Asset<Texture2D> SmallButtonHoverTexture;

    private static bool _playerInventoryOpen;
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
        // Don't run this on the server
        if (Main.dedServ) return;

        EmptyTabHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyTabHeader");
        EmptyTabHeaderAltTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyTabHeaderAlt");
        PokedexTabHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexTabHeader");
        PokedexTabIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexTabIcon");
        BallIconTexture = ModContent.Request<Texture2D>("Terramon/icon_small");
        BallIconEmptyTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/EmptyBallIcon");
        PlayerDexFilterTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PlayerDexFilter");
        WorldDexFilterTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/WorldDexFilter");
        PlayerShinyDexFilterTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PlayerShinyDexFilter");
        SmallButtonHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/SmallButtonHover");

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

        // Mimic bestiary behaviour (this should not run if the hub UI is active)
        On_Player.LookForTileInteractions += static (orig, self) =>
        {
            if (Active) return;
            orig(self);
        };
    }

    public static bool WorldDexMode { get; private set; }

    public static bool ShinyActive { get; private set; }
    public static bool ShiftKeyIgnore { get; set; }

    public static bool Active { get; private set; }
    public override bool Visible => false; // Vanilla will update/draw this state through IngameFancyUI

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    }

    public static void SetActive(bool active, bool customSound = true)
    {
        if (Active == active) return;
        Active = active;
        var hubState = UILoader.GetUIState<HubUI>();
        hubState.RefreshPokedex(closeOverview: !active);
        if (active)
        {
            UILinkPointNavigator.ChangePoint(TerramonPointID.PokedexMin);
            _playerInventoryOpen = Main.playerInventory;
            hubState.Update(null);
            IngameFancyUI.OpenUIState(hubState);
        }
        else
        {
            IngameFancyUIClose();
            Main.playerInventory = _playerInventoryOpen;
            UILinkPointNavigator.ChangePoint(InventoryParty.InPCMode ? TerramonPointID.PC0 : GamepadPointID.Inventory0);
        }

        if (customSound)
            SoundEngine.PlaySound(new SoundStyle(active ? "Terramon/Sounds/dex_open" : "Terramon/Sounds/dex_close")
            {
                Volume = 0.48f
            });
        else
            SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public static void ToggleActive()
    {
        SetActive(!Active);
    }

    public static void OpenToPokemon(ushort pokemon, bool isShiny = false)
    {
        if (Active) return;

        var hubState = UILoader.GetUIState<HubUI>();
        var filterButtonImage = isShiny ? PlayerShinyDexFilterTexture : PlayerDexFilterTexture;
        var filterButtonRarity = isShiny ? ModContent.RarityType<KeyItemRarity>() : ItemRarityID.White;
        WorldDexMode = false;
        ShiftKeyIgnore = true;
        ShinyActive = isShiny;
        hubState._filterButton.SetImage(filterButtonImage);
        hubState._filterButton.SetHoverRarity(filterButtonRarity);
        SetActive(true, false);
        hubState.SetCurrentPokedexEntry(TerramonPlayer.LocalPlayer.GetPokedex(isShiny).Entries[pokemon], pokemon);
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
        var pokedexTabHeader = new HubTabHeader(Language.GetText("Mods.Terramon.Keybinds.OpenPokedex.DisplayName"), 93,
            new Vector2(51, 15), PokedexTabHeaderTexture,
            PokedexTabIconTexture, TerramonPointID.HubTab0);
        AddElement(pokedexTabHeader, -12, -12, 227, 66, _mainPanel);

        // First ??? tab
        var emptyTabHeader = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderTexture,
            null, TerramonPointID.HubTab1);
        AddElement(emptyTabHeader, 213, -12, 226, 66, _mainPanel);

        // Second ??? tab
        var emptyTabHeader2 = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderTexture,
            null, TerramonPointID.HubTab2);
        AddElement(emptyTabHeader2, 437, -12, 226, 66, _mainPanel);

        // Third ??? tab
        var emptyTabHeader3 = new HubTabHeader("???", 96, Vector2.Zero, EmptyTabHeaderAltTexture,
            null, TerramonPointID.HubTab3);
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
        _rangeText = new UIText(string.Empty, 0.92f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            PaddingLeft = 14,
            PaddingRight = 14
        };
        _rangePanel.Append(_rangeText);
        AddElement(_rangePanel, 104, 65, (int)_rangeText.MinWidth.Pixels, 38, _mainPanel);

        // Filter button
        _filterButton = new UIHoverImageButton(PlayerDexFilterTexture, string.Empty)
        {
            Left = { Pixels = -38 - 6, Percent = 1f },
            Top = { Pixels = 65 },
            Width = { Pixels = 38 },
            Height = { Pixels = 38 }
        };
        _filterButton.SetHoverImage(SmallButtonHoverTexture);
        _filterButton.SetVisibility(1f, 1f);
        _filterButton.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            WorldDexMode = !WorldDexMode;
            ShinyActive = false;
            if (WorldDexMode)
                _filterButton.SetHoverText(Language.GetTextValue("Mods.Terramon.GUI.Pokedex.WorldDexFilter"));
            _filterButton.SetImage(WorldDexMode ? WorldDexFilterTexture : PlayerDexFilterTexture);
            _filterButton.SetHoverRarity(ItemRarityID.White);
            RefreshPokedex(closeOverview: true);
        };
        _filterButton.OnRightClick += (_, _) =>
        {
            if (WorldDexMode) return; // TODO: Later maybe make a Shiny World Dex
            var shinyDex = TerramonPlayer.LocalPlayer.GetPokedex(true);
            if (shinyDex.SeenCount == 0 && shinyDex.RegisteredCount == 0) return;
            SoundEngine.PlaySound(SoundID.MaxMana);
            ShinyActive = !ShinyActive;
            _filterButton.SetImage(ShinyActive ? PlayerShinyDexFilterTexture : PlayerDexFilterTexture);
            _filterButton.SetHoverRarity(ShinyActive ? ModContent.RarityType<KeyItemRarity>() : ItemRarityID.White);
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
        var caughtBallIcon = new UIHoverImage(BallIconTexture, Language.GetText("Mods.Terramon.GUI.Pokedex.Obtained"))
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
        _seenBallIcon = new UIHoverImage(BallIconEmptyTexture, Language.GetText("Mods.Terramon.GUI.Pokedex.Seen"))
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
        _progressText = new UIText(string.Empty, 0.92f)
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
        if (!WorldDexMode)
            _filterButton.SetHoverText(Language.GetTextValue(
                ShinyActive
                    ? "Mods.Terramon.GUI.Pokedex.PlayerShinyDexFilter"
                    : "Mods.Terramon.GUI.Pokedex.PlayerDexFilter",
                Main.LocalPlayer.name));

        //update world dex uilinkpoint
        UILinkPointNavigator.Pages[TerramonPageID.Pokedex].LinkMap[TerramonPointID.WorldDexToggle].Position =
            _filterButton.GetDimensions().ToRectangle().Center.ToVector2() * Main.UIScale;

        //change page using gamepad shoulder buttons
        if (UILinkPointNavigator.CurrentPage == TerramonPageID.HackySwitchPageLeft ||
            UILinkPointNavigator.CurrentPage == TerramonPageID.HackySwitchPageRight)
        {
            var dirIsRight = UILinkPointNavigator.CurrentPage == TerramonPageID.HackySwitchPageRight;

            var currentRange = _pokedexPage.GetPageRange();
            if ((!dirIsRight && currentRange.Item1 == 1) ||
                (dirIsRight && currentRange.Item2 == Terramon.LoadedPokemonCount))
            {
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                {
                    Volume = 0.25f
                });
            }
            else
            {
                _pokedexPage.ChangePage(dirIsRight.ToDirectionInt()); // -1 for left, 1 for right
                UILoader.GetUIState<HubUI>().RefreshPokedex();

                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/dex_pageup")
                {
                    Volume = 0.325f
                });
            }

            UILinkPointNavigator.ChangePoint(TerramonPointID.PokedexMin);
        }

        Recalculate();
    }

    public void RefreshPokedex(ushort pokemon = 0, bool closeOverview = false)
    {
        // Close the Pokémon overview panel if requested
        if (closeOverview)
            _overviewPanel.SetCurrentEntry(null, 0);

        // Get the player's Pokédex
        var pokedex =
            WorldDexMode
                ? TerramonWorld.GetWorldDex()
                : TerramonPlayer.LocalPlayer.GetPokedex(ShinyActive); // Neither should be null here

        // Update the Pokémon page entries
        if (pokemon == 0)
        {
            _pokedexPage.UpdateAllEntries(pokedex);
        }
        else
        {
            var entry = pokedex.Entries.GetValueOrDefault(pokemon);
            _pokedexPage.UpdateEntry(pokemon, entry);
            if (_overviewPanel.Pokemon == pokemon)
                _overviewPanel.SetCurrentEntry(entry, pokemon);
        }

        if (pokemon == 0)
        {
            // Update the range text
            var range = _pokedexPage.GetPageRange();
            _rangeText.SetText(Language.GetTextValue("Mods.Terramon.GUI.Pokedex.ShowingRange", range.Item1, range.Item2,
                Terramon.LoadedPokemonCount));
            _rangePanel.Width.Set((int)_rangeText.MinWidth.Pixels, 0f);
        }

        // Update the caught/seen amount and completion percentage
        var seenCount = pokedex.SeenCount;
        var registeredCount = pokedex.RegisteredCount;
        _seenAmountText.SetText((registeredCount + seenCount).ToString());
        _caughtAmountText.SetText(registeredCount.ToString());
        var completion = pokedex.RegisteredCount * 100f / Terramon.HighestPokemonID;
        if (completion > 100) completion = 100; // Just in case
        var completionFmtString = Language.GetTextValue("Mods.Terramon.GUI.Pokedex.Completion", $"{completion:0.##}");
        _progressText.SetText(completionFmtString, 0.92f, false);
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
            _progressText.SetText(completionFmtString, 0.92f - diff / 300f, false);
        }

        _progressPanel.Width.Set(fillWidth, 0f);

        Recalculate();
    }

    public void ResetPokedex()
    {
        _pokedexPage.ReturnToFirstPage(); // Resets the Pokédex page to the first page for the next time it is opened
        WorldDexMode = false; // Reset to player's Pokédex mode
        ShinyActive = false; // Reset to non-shiny mode
        _filterButton.SetImage(PlayerDexFilterTexture);
        _filterButton.SetHoverRarity(ItemRarityID.White);
        RefreshPokedex();
    }

    public void SetCurrentPokedexEntry(PokedexEntry entry, ushort pokemon)
    {
        _overviewPanel.SetCurrentEntry(entry, pokemon, true);
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
        _pokedexPage.Reset();
        if (Active) RefreshPokedex(closeOverview: true);
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
    private readonly int _uiLinkPointID;
    private bool _headerAssetLoaded;
    private bool _iconAssetLoaded;

    public HubTabHeader(object title, float headerX, Vector2 iconPos, Asset<Texture2D> headerTexture,
        Asset<Texture2D> iconTexture, int uiLinkPointID) : base(headerTexture)
    {
        _locked = title.ToString() == "???";
        _headerAsset = headerTexture;
        _iconAsset = iconTexture;
        _uiLinkPointID = uiLinkPointID;

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

    public override void Update(GameTime gameTime)
    {
        UILinkPointNavigator.SetPosition(_uiLinkPointID, GetDimensions().ToRectangle().Center.ToVector2());
        base.Update(gameTime);
    }
}

internal sealed class PokedexPageDisplay : UIElement
{
    private const int ButtonSize = 70;

    private readonly List<PokedexEntryIcon> _entries = [];

    // Cache the sorted Pokemon IDs for efficient paging
    private readonly List<ushort> _sortedPokemonIds;

    private int _cols;
    private int _rows;
    private ushort _startPokemonId; // Now stores actual Pokemon ID, not array index

    public PokedexPageDisplay(int width, int height, int rows, int cols)
    {
        _rows = rows;
        _cols = cols;

        Width.Set(width, 0);
        Height.Set(height, 0);

        // Cache and sort all available Pokemon IDs
        _sortedPokemonIds = Terramon.DatabaseV2.Pokemon.Keys
            .Where(id => id <= Terramon.HighestPokemonID)
            .OrderBy(id => id)
            .ToList();

        _startPokemonId = _sortedPokemonIds.FirstOrDefault();
        Reset();
    }

    public void ChangePage(int direction)
    {
        var itemsPerPage = _rows * _cols;
        var currentIndex = _sortedPokemonIds.IndexOf(_startPokemonId);

        if (currentIndex == -1) // Current start ID not found, reset to first
        {
            _startPokemonId = _sortedPokemonIds.FirstOrDefault();
            Reset();
            return;
        }

        var newIndex = currentIndex + direction * itemsPerPage;

        // Clamp to valid bounds
        if (newIndex < 0)
        {
            newIndex = 0;
        }
        else if (newIndex >= _sortedPokemonIds.Count)
        {
            // Go to the last possible page that shows items
            var lastPageStartIndex = Math.Max(0, _sortedPokemonIds.Count - itemsPerPage);
            newIndex = lastPageStartIndex;
        }

        _startPokemonId = _sortedPokemonIds[newIndex];
        Reset();
    }

    public void ReturnToFirstPage()
    {
        _startPokemonId = _sortedPokemonIds.FirstOrDefault();
        Reset();
    }

    public Tuple<int, int> GetPageRange()
    {
        var startIndex = _sortedPokemonIds.IndexOf(_startPokemonId);
        if (startIndex == -1) return Tuple.Create(0, 0);

        var itemsPerPage = _rows * _cols;
        var endIndex = Math.Min(startIndex + itemsPerPage - 1, _sortedPokemonIds.Count - 1);

        return Tuple.Create((int)_sortedPokemonIds[startIndex], (int)_sortedPokemonIds[endIndex]);
    }

    public void Reset()
    {
        // Delete all the current entries
        foreach (var entry in _entries)
            entry.Remove();
        _entries.Clear();
        UILinkManager.ResetPokedexSlots();

        // Calculate row count based on the button size and the height of the page
        _rows = (int)Height.Pixels / (ButtonSize + 4);
        _cols = (int)Width.Pixels / (ButtonSize + 4);

        // Calculate the spacing between the buttons based on the size of the buttons and the size of the page
        var buttonSpacingX = (Width.Pixels - _cols * ButtonSize) / (_cols - 1);
        var buttonSpacingY = (Height.Pixels - _rows * ButtonSize) / (_rows - 1);

        // Find the starting index in our sorted Pokemon list
        var startIndex = _sortedPokemonIds.IndexOf(_startPokemonId);
        if (startIndex == -1) startIndex = 0; // Fallback if ID not found

        // Add the new entries
        var itemsPerPage = _rows * _cols;
        for (var i = 0; i < itemsPerPage; i++)
        {
            var row = i / _cols;
            var col = i % _cols;
            var pokemonIndex = startIndex + i;

            if (pokemonIndex >= _sortedPokemonIds.Count)
            {
                // Handle early page ending for controller navigation
                var id = TerramonPointID.PokedexMin + i;

                //if page ends early, set edge values
                if (col != 0 && id <= TerramonPointID.PokedexMax + 1)
                {
                    var page = UILinkPointNavigator.Pages[TerramonPageID.Pokedex];
                    id -= 1;

                    page.LinkMap[id].Right = -1;

                    //current row
                    for (var j = 0; j <= col; j++)
                        page.LinkMap[id - j].Down = -1;

                    //row above
                    for (var j = col; j < _cols; j++)
                        page.LinkMap[id + j - 6].Down = -1;
                }

                break;
            }

            var pokemon = _sortedPokemonIds[pokemonIndex];

            var monButton = new PokedexEntryIcon(pokemon)
            {
                Left = { Pixels = col * (ButtonSize + buttonSpacingX) },
                Top = { Pixels = (int)(row * (ButtonSize + buttonSpacingY)) }
            };

            _entries.Add(monButton);
            Append(monButton);

            //set up ui links
            var pointID = TerramonPointID.PokedexMin + i;
            if (pointID <= TerramonPointID.PokedexMax)
            {
                //set uilinkpoint position
                UILinkPointNavigator.SetPosition(pointID, monButton.GetDimensions().ToRectangle().Center.ToVector2());
                var page = UILinkPointNavigator.Pages[TerramonPageID.Pokedex];

                //set uilinkpoint navigation for edge elements
                if (col == 0)
                    page.LinkMap[pointID].Left = -1;
                if (col == _cols - 1)
                {
                    if (row <= 1)
                        page.LinkMap[pointID].Right = TerramonPointID.WorldDexToggle;
                    else
                        page.LinkMap[pointID].Right = -1;
                }

                if (row == 0)
                    page.LinkMap[pointID].Up = col == 0 ? TerramonPointID.PokedexLeft : TerramonPointID.PokedexRight;

                if (row == _rows - 1)
                    page.LinkMap[pointID].Down = -1;
            }
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
            UILoader.GetUIState<HubUI>().SetCurrentPokedexEntry(_entry, ID);
        };
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
                $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(ID)}_Mini{(HubUI.ShinyActive ? "_S" : string.Empty)}");
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
        if (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift))
        {
            if (HubUI.ShiftKeyIgnore) return;
            _debugText.TextColor = Color.Transparent;
            _debugText.ShadowColor = Color.Transparent;
        }
        else
        {
            _debugText.TextColor = Color.White;
            _debugText.ShadowColor = Color.Black;
            HubUI.ShiftKeyIgnore = false;
        }

        base.Update(gameTime);
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
                    useHoverText += Language.GetTextValue("Mods.Terramon.GUI.Pokedex.ObtainedBy", _entry.LastUpdatedBy);
                    break;
                case PokedexEntryStatus.Seen:
                    useHoverText += Language.GetTextValue("Mods.Terramon.GUI.Pokedex.SeenBy", _entry.LastUpdatedBy);
                    break;
                case PokedexEntryStatus.Undiscovered:
                default:
                    break;
            }

        Main.instance.MouseText(useHoverText,
            _hoverText.ToString() != "???" && HubUI.ShinyActive
                ? ModContent.RarityType<KeyItemRarity>()
                : ItemRarityID.White);
    }
}

internal sealed class PokedexPageButton : UIHoverImageButton
{
    private static readonly Asset<Texture2D> PageButtonLeftTexture;
    private static readonly Asset<Texture2D> PageButtonRightTexture;

    private readonly PokedexPageDisplay _pageDisplay;
    private readonly bool _right;
    private bool _lastXDown;
    private bool _lastZDown;

    static PokedexPageButton()
    {
        PageButtonLeftTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonLeft");
        PageButtonRightTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PageButtonRight");
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
        SetHoverImage(HubUI.SmallButtonHoverTexture);
        SetVisibility(1f, 1f);
    }

    public override void Update(GameTime gameTime)
    {
        var zDown = Main.keyState.IsKeyDown(Keys.Z);
        var xDown = Main.keyState.IsKeyDown(Keys.X);
        if ((zDown && !_lastZDown && !_right) || (xDown && !_lastXDown && _right))
            ChangePage();
        _lastZDown = zDown;
        _lastXDown = xDown;

        UILinkPointNavigator.Pages[TerramonPageID.Pokedex].LinkMap[TerramonPointID.PokedexLeft + (_right ? 1 : 0)]
                .Position =
            GetDimensions().ToRectangle().Center.ToVector2() * Main.UIScale;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        ChangePage();
    }

    private void ChangePage()
    {
        var currentRange = _pageDisplay.GetPageRange();
        if ((!_right && currentRange.Item1 == 1) ||
            (_right && currentRange.Item2 == Terramon.HighestPokemonID))
        {
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
            {
                Volume = 0.25f
            });
            return;
        }

        _pageDisplay.ChangePage(_right.ToDirectionInt()); // -1 for left, 1 for right
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
    private static readonly Asset<Texture2D> OverviewDividerTexture;
    private readonly UIPanel _altTypePanel;
    private readonly UIText _altTypeText;
    private readonly UIContainer _dexEntryContainer;
    private readonly UIText _dexEntryText;
    private readonly UIText _dexNoText;
    private readonly UIImage _divider;
    private readonly UIImage _header;
    private readonly UIText _heightText;
    private readonly UIContainer _heightWeightContainer;
    private readonly UIList _list;
    private readonly UIPanel _mainTypePanel;
    private readonly UIText _mainTypeText;
    private readonly BetterUIText _monNameText;
    private readonly PokedexPreviewCanvas _preview;
    private readonly UIScrollbar _scrollBar;
    private readonly UIPanel _speciesPanel;
    private readonly UIText _speciesText;
    private readonly UIContainer _typeContainer;
    private readonly UIText _weightText;
    private PokedexEntryStatus _status;

    static PokedexOverviewPanel()
    {
        OverviewHeaderTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexOverviewHeader");
        OverviewDividerTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Hub/PokedexOverviewDivider");
    }

    public PokedexOverviewPanel()
    {
        Width.Set(380, 0);
        Height.Set(694, 0);
        BackgroundColor = new Color(37, 49, 90);
        SetPadding(0);
        _list = new UIList
        {
            Top = { Pixels = 58 },
            Width = { Percent = 1f },
            Height = { Pixels = 634 },
            ListPadding = 0f
        };
        _preview = new PokedexPreviewCanvas
        {
            Left = { Pixels = 43 },
            MarginTop = 18
        };
        _list.Add(_preview);
        _divider = new UIImage(OverviewDividerTexture)
        {
            Width = { Pixels = 361 },
            Height = { Pixels = 6 },
            Left = { Pixels = 2 },
            MarginTop = 34,
            Color = Color.Transparent
        };
        _list.Add(_divider);
        _speciesPanel = new UIPanel
        {
            Width = { Pixels = 307 },
            Height = { Pixels = 38 },
            Left = { Pixels = 25 },
            MarginTop = -22,
            BackgroundColor = new Color(50, 62, 114)
        };
        _speciesText = new UIText(string.Empty, 0.84f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _speciesPanel.Append(_speciesText);
        //Append(_speciesPanel);
        _typeContainer = new UIContainer(new Vector2(307, 48))
        {
            Left = { Pixels = 25 },
            MarginTop = 18
        };
        var typePanel = new UIPanel
        {
            Width = { Pixels = 307 },
            Height = { Pixels = 48 },
            BackgroundColor = new Color(37, 49, 90)
        };
        var typeDesc = new UIText("Type", 0.84f)
        {
            VAlign = 0.5f
        };
        typePanel.Append(typeDesc);
        _typeContainer.Append(typePanel);
        _mainTypePanel = new UIPanel
        {
            Width = { Pixels = 78 },
            Height = { Pixels = 28 },
            Left = { Pixels = 131 },
            Top = { Pixels = 10 },
            BackgroundColor = new Color(206, 207, 206)
        };
        _mainTypeText = new UIText(string.Empty, 0.8f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _mainTypePanel.Append(_mainTypeText);
        _typeContainer.Append(_mainTypePanel);
        _altTypePanel = new UIPanel
        {
            Width = { Pixels = 78 },
            Height = { Pixels = 28 },
            Left = { Pixels = 219 },
            Top = { Pixels = 10 },
            BackgroundColor = new Color(206, 207, 206)
        };
        _altTypeText = new UIText(string.Empty, 0.8f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _altTypePanel.Append(_altTypeText);
        _typeContainer.Append(_altTypePanel);
        //_list.Add(typeContainer);
        _dexEntryContainer = new UIContainer(new Vector2(307, 113))
        {
            Left = { Pixels = 25 },
            MarginTop = 18,
            MarginBottom = 18
        };
        var dexEntryPanel = new UIPanel
        {
            Width = { Pixels = 307 },
            Height = { Pixels = 90 },
            Top = { Pixels = 23 },
            BackgroundColor = new Color(37, 49, 90)
        };
        var dexEntryDesc = new UIText(Language.GetText("Mods.Terramon.GUI.Pokedex.Entry"), 0.84f)
        {
            Left = { Pixels = 1 }
        };
        _dexEntryContainer.Append(dexEntryDesc);
        _dexEntryText = new UIText(string.Empty, 0.84f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            Width = { Percent = 1f },
            MarginTop = 25,
            IsWrapped = true
        };
        dexEntryPanel.Append(_dexEntryText);
        _dexEntryContainer.Append(dexEntryPanel);
        //_list.Add(_dexEntryContainer);
        _heightWeightContainer = new UIContainer(new Vector2(307, 60))
        {
            Left = { Pixels = 25 },
            MarginTop = 18
        };
        var heightPanel = new UIPanel
        {
            Width = { Pixels = 144 },
            Height = { Pixels = 38 },
            Top = { Pixels = 22 },
            BackgroundColor = new Color(37, 49, 90)
        };
        heightPanel.SetPadding(0);
        var heightDesc = new UIText(Language.GetText("Mods.Terramon.GUI.Pokedex.Height"), 0.84f)
        {
            Left = { Pixels = 1 }
        };
        _heightWeightContainer.Append(heightDesc);
        _heightText = new UIText("0.0 m (0′00″)", 0.84f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        heightPanel.Append(_heightText);
        _heightWeightContainer.Append(heightPanel);
        var weightPanel = new UIPanel
        {
            Width = { Pixels = 144 },
            Height = { Pixels = 38 },
            Top = { Pixels = 22 },
            Left = { Pixels = 163 },
            BackgroundColor = new Color(37, 49, 90)
        };
        weightPanel.SetPadding(0);
        var weightDesc = new UIText(Language.GetText("Mods.Terramon.GUI.Pokedex.Weight"), 0.84f)
        {
            Left = { Pixels = 164 }
        };
        _heightWeightContainer.Append(weightDesc);
        _weightText = new UIText("0.0 kg (0.0 lbs)", 0.84f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        weightPanel.Append(_weightText);
        _heightWeightContainer.Append(weightPanel);
        //_list.Add(heightWeightContainer);
        Append(_list);
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
        _monNameText = new BetterUIText(string.Empty, 0.405f, true)
        {
            HAlign = 0.47f,
            Top = { Pixels = 22 }
        };
        Append(_monNameText);
        _scrollBar = new UIScrollbar();
        _scrollBar.Height.Set(-22, 1f);
        _scrollBar.HAlign = 1f;
        _scrollBar.VAlign = 1f;
        _scrollBar.MarginRight = 5;
        _scrollBar.MarginBottom = 11;
        _scrollBar.SetView(100f, 1000f);
        _list.SetScrollbar(_scrollBar);
        Append(_scrollBar);
    }

    public ushort Pokemon { get; private set; }

    /*public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _monNameText.TextColor = HubUI.ShinyActive ? ModContent.GetInstance<KeyItemRarity>().RarityColor : Color.White;
    }*/

    public override void Recalculate()
    {
        base.Recalculate();

        _list.Height.Set(Height.Pixels - 60, 0);
    }

    public void SetCurrentEntry(PokedexEntry entry, ushort pokemon, bool playCry = false)
    {
        var status = entry?.Status ?? PokedexEntryStatus.Undiscovered;

        if (Pokemon == pokemon && _status == status)
        {
            _preview.CaughtCount = entry?.CaughtCount ?? 0; // Still update the caught count
            return;
        }

        Pokemon = pokemon;
        _status = status;

        // Remove list items so they are not added multiple times
        _list.Remove(_dexEntryContainer);
        _list.Remove(_heightWeightContainer);
        _list.Remove(_typeContainer);
        _list.Remove(_speciesPanel);

        if (pokemon == 0)
        {
            _dexNoText.SetText(string.Empty);
            _monNameText.SetText(string.Empty);
            _header.Color = Color.Transparent;
            _divider.Color = Color.Transparent;
            _preview.IDToDraw = 0;
            _scrollBar.Height.Set(-22, 1f);
            return;
        }

        _dexNoText.SetText($"{Language.GetTextValue("Mods.Terramon.GUI.Pokedex.NumberPrefix")} {pokemon:000}");
        _monNameText.SetText(Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(pokemon));
        if (status == PokedexEntryStatus.Registered)
        {
            var schema = Terramon.DatabaseV2.GetPokemon(pokemon);
            // Play Pokémon cry
            if (playCry)
            {
                var cry = new SoundStyle("Terramon/Sounds/Cries/" + schema.Identifier)
                    { Volume = 0.15f };
                SoundEngine.PlaySound(cry);
            }

            _speciesText.SetText(Terramon.DatabaseV2.GetPokemonSpeciesDirect(pokemon));
            _dexEntryText.SetText(Terramon.DatabaseV2.GetPokemonDexEntryDirect(pokemon));
            var heightInMeters = schema.Height / 10f; // Convert from decimeters to meters
            var heightInInches = schema.Height * 3.937f; // Convert from decimeters to inches
            var feet = (int)(heightInInches / 12); // Get the number of feet
            var inches = (int)Math.Round(heightInInches % 12); // Get the remaining inches
            _heightText.SetText($"{heightInMeters:0.0} m ({feet}′{inches:00}″)");
            var weightInKg = schema.Weight / 10f; // Convert from hectograms to kilograms
            var weightInLbs = schema.Weight * 0.220462f; // Convert from hectograms to pounds
            var weightFmtString = $"{weightInKg:0.0} kg ({weightInLbs:0.0} lbs)";
            _weightText.SetText(weightFmtString, 0.84f, false);
            var minWidth = (int)_weightText.MinWidth.Pixels;
            if (minWidth > 122) // If larger than containing panel width (144 - 2 * 11)
            {
                var diff = minWidth - 122;
                // Scale the progress text to fit the new width
                _weightText.SetText(weightFmtString, 0.84f - diff / 172f, false);
            }

            // Types
            var mainType = schema.Types[0];
            _mainTypeText.SetText(Language.GetTextValue($"Mods.Terramon.Types.{mainType.ToString()}"));
            _mainTypePanel.BackgroundColor = mainType.GetColor();
            var dualType = schema.Types.Count > 1;
            if (dualType)
            {
                var altType = schema.Types[1];
                _altTypeText.SetText(Language.GetTextValue($"Mods.Terramon.Types.{altType.ToString()}"));
                _altTypePanel.BackgroundColor = altType.GetColor();
            }
            else
            {
                _altTypeText.SetText("");
                _altTypePanel.BackgroundColor = new Color(50, 62, 114);
            }
        }
        else
        {
            _speciesText.SetText("???");
            _dexEntryText.SetText("???");
            _heightText.SetText("???");
            _weightText.SetText("???");
            _mainTypePanel.BackgroundColor = new Color(50, 62, 114);
            _altTypePanel.BackgroundColor = new Color(50, 62, 114);
            _mainTypeText.SetText(string.Empty);
            _altTypeText.SetText(string.Empty);
        }

        _list.Add(_speciesPanel);
        _list.Add(_typeContainer);
        _list.Add(_heightWeightContainer);
        _list.Add(_dexEntryContainer);
        _header.Color = Color.White;
        _divider.Color = Color.White;
        _preview.IDToDraw = pokemon;
        _preview.CaughtCount = entry?.CaughtCount ?? 0;
        _scrollBar.Height.Set(-80, 1f);
    }
}

internal sealed class PokedexPreviewCanvas : UIImage
{
    private static readonly Asset<Texture2D> OverviewPreviewTexture;
    private NPC _dummyNPCForDrawing;
    private ushort _idToDraw;
    private UIImage _previewBackground;
    private UIImage _previewBackgroundOverlay;

    public int CaughtCount;

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
            if (value == 0)
            {
                Color = Color.Transparent;
                _previewBackground.Color = Color.Transparent;
                _previewBackgroundOverlay.Color = Color.Transparent;
                return;
            }

            Color = Color.White;
            GetBackgroundAssets(value, out var background, out var backgroundColor, out var overlay,
                out var overlayColor);
            _previewBackground.SetImage(background);
            _previewBackground.Color = backgroundColor;
            _previewBackgroundOverlay.SetImage(overlay ?? TextureAssets.MagicPixel);
            _previewBackgroundOverlay.Color = overlayColor;

            if (!PokemonEntityLoader.IDToNPCType.TryGetValue(value, out var type)) return;
            _dummyNPCForDrawing = new NPC
            {
                IsABestiaryIconDummy = true
            };
            _dummyNPCForDrawing.SetDefaults_ForNetId(type, 1);
            _dummyNPCForDrawing.netID = type;
            var pokemonNpc = (PokemonNPC)_dummyNPCForDrawing.ModNPC;
            pokemonNpc.Data = new PokemonData
            {
                ID = value,
                IsShiny = HubUI.ShinyActive
            };
        }
    }

    public override void OnInitialize()
    {
        _previewBackground = new UIImage(TextureAssets.MapBGs[0])
        {
            OverrideSamplerState = SamplerState.PointClamp,
            RemoveFloatingPointsFromDrawPosition = true,
            ImageScale = 2f,
            Color = Color.Transparent
        };
        var adjustedLeft = _previewBackground.Width.Pixels * 0.5f + 22;
        var adjustedTop = _previewBackground.Height.Pixels * 0.5f + 4;
        _previewBackground.Left.Pixels = adjustedLeft;
        _previewBackground.Top.Pixels = adjustedTop;
        Append(_previewBackground);

        _previewBackgroundOverlay = new UIImage(TextureAssets.MagicPixel)
        {
            Width = _previewBackground.Width,
            Height = _previewBackground.Height,
            AllowResizingDimensions = false,
            OverrideSamplerState = SamplerState.PointClamp,
            RemoveFloatingPointsFromDrawPosition = true,
            ImageScale = 2f,
            Color = Color.Transparent
        };
        _previewBackgroundOverlay.Left.Pixels = adjustedLeft;
        _previewBackgroundOverlay.Top.Pixels = adjustedTop;
        Append(_previewBackgroundOverlay);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (_idToDraw == 0) return;
        _dummyNPCForDrawing.FindFrame();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        if (_idToDraw == 0) return;

        var position = GetOuterDimensions().Position();
        var drawNPCPosition = position;
        drawNPCPosition.X += (int)(Width.Pixels / 2 - _dummyNPCForDrawing.width / 2f);
        drawNPCPosition.Y += (int)(Height.Pixels - _dummyNPCForDrawing.height + 2);
        if (_dummyNPCForDrawing.GetGlobalNPC<NPCWanderingHoverBehaviour>()
            .Enabled) // Draw the NPC a bit higher if it's a flying Pokémon
            drawNPCPosition.Y -= 6;
        if (_dummyNPCForDrawing.GetGlobalNPC<NPCWalkingBehaviour>()?.AnimationType ==
            NPCWalkingBehaviour.AnimType.IdleForward &&
            _dummyNPCForDrawing.frame.Y == 0) // Draw fix for walking Pokémon using IdleForward animation
            _dummyNPCForDrawing.frame.Y = TextureAssets.Npc[_dummyNPCForDrawing.type].Height() /
                                          Main.npcFrameCount[_dummyNPCForDrawing.type];
        Main.instance.DrawNPCDirect(spriteBatch, _dummyNPCForDrawing, false, -drawNPCPosition);

        if (HubUI.WorldDexMode) return; // Not supported in World Dex mode yet

        var ballIcon = BallAssets.GetBallIcon(GetBallTypeForCaughtCount(CaughtCount));
        var ballPosition = position + new Vector2(28, 28);
        spriteBatch.Draw(ballIcon.Value, ballPosition, null,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        var textPosition = ballPosition + new Vector2(21, 0);
        var font = FontAssets.MouseText.Value;
        var textColor = GetTextColorForCaughtCount(CaughtCount);
        var baseScale = new Vector2(0.75f);
        var origin = Vector2.Zero;
        var shadowColor = Color.Black * (textColor.A / 255f);
        var snippets = ChatManager.ParseMessage(CaughtCount.ToString(), textColor).ToArray();
        ChatManager.ConvertNormalSnippets(snippets);
        ChatManager.DrawColorCodedStringShadow(spriteBatch, font, snippets, textPosition, shadowColor, 0f, origin,
            baseScale, -1f, 1.5f);
        ChatManager.DrawColorCodedString(spriteBatch, font, snippets, textPosition, Color.White, 0f, origin, baseScale,
            out _, -1f);
    }

    private static Color GetTextColorForCaughtCount(int caughtCount)
    {
        return caughtCount >= 3 ? Color.White : new Color(192, 198, 214);
    }

    private static BallID GetBallTypeForCaughtCount(int caughtCount)
    {
        return caughtCount switch
        {
            >= 12 => BallID.MasterBall,
            >= 9 => BallID.UltraBall,
            >= 6 => BallID.GreatBall,
            _ => BallID.PokeBall
        };
    }

    private static void GetBackgroundAssets(ushort pokemon, out Asset<Texture2D> background, out Color backgroundColor,
        out Asset<Texture2D> overlay, out Color overlayColor)
    {
        background = TextureAssets.MapBGs[0];
        var schema = Terramon.DatabaseV2.GetPokemon(pokemon);
        if (schema.Types[0] == PokemonType.Ghost)
        {
            backgroundColor = new Color(35, 40, 40);
            overlay = BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Visuals.Moon.GetBackgroundOverlayImage();
            overlayColor = BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Visuals.Moon
                .GetBackgroundOverlayColor() ?? Color.White;
        }
        else
        {
            backgroundColor = Color.White;
            overlay = null;
            overlayColor = Color.Transparent;
        }
    }
}
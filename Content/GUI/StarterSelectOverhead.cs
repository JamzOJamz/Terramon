using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items.PokeBalls;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class StarterSelectOverhead : SmartUIState
{
    private readonly ushort[] _starters =
    {
        NationalDexID.Bulbasaur,
        NationalDexID.Charmander,
        NationalDexID.Squirtle,
        NationalDexID.Chikorita,
        NationalDexID.Cyndaquil,
        NationalDexID.Totodile,
        NationalDexID.Treecko,
        NationalDexID.Torchic,
        NationalDexID.Mudkip,
        NationalDexID.Turtwig,
        NationalDexID.Chimchar,
        NationalDexID.Piplup,
        NationalDexID.Snivy,
        NationalDexID.Tepig,
        NationalDexID.Oshawott,
        NationalDexID.Chespin,
        NationalDexID.Fennekin,
        NationalDexID.Froakie,
        NationalDexID.Rowlet,
        NationalDexID.Litten,
        NationalDexID.Popplio
    };

    private UIBlendedImage _background;
    private UIText _hintText;
    private UIHoverImageButton _showButton;
    private UIContainer _starterPanel;
    private bool _starterPanelShowing = true;
    private UIText _titleText;

    public override bool Visible =>
        !Main.playerInventory && !Main.LocalPlayer.dead && !TerramonPlayer.LocalPlayer.HasChosenStarter &&
        Main.LocalPlayer.talkNPC < 0;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        _showButton = new UIHoverImageButton(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Notification",
            AssetRequestMode.ImmediateLoad), string.Empty);
        _showButton.Width.Set(42, 0);
        _showButton.Height.Set(40, 0);
        _showButton.HAlign = 1;
        _showButton.VAlign = 1;
        _showButton.MarginRight = 10;
        _showButton.MarginBottom = 10;
        _showButton.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
        _showButton.OnLeftClick += (_, _) =>
        {
            _showButton.SetIsActive(false);
            SoundEngine.PlaySound(SoundID.MenuOpen);
            _starterPanel.VAlign = 0.19f;
            _starterPanelShowing = true;
        };
        Append(_showButton);

        _starterPanel = new UIContainer(new Vector2(660, 330))
        {
            HAlign = 0.5f,
            VAlign = 0.19f
        };

        _titleText = new UIText("Welcome to the world of Pokémon! Thank you for installing the Terramon mod!");
        var subText = new UIText("Now, please choose your starter Pokémon!");
        _titleText.HAlign = 0.5f;
        subText.Top.Set(26, 0);
        subText.HAlign = 0.5f;
        _titleText.Append(subText);
        _starterPanel.Append(_titleText);

        _background = new UIBlendedImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Background",
            AssetRequestMode.ImmediateLoad));
        _hintText = new UIText("(Press Backspace to Close)", 0.85f)
        {
            HAlign = 0.5f,
            Top = { Pixels = 281 },
            TextColor = new Color(173, 173, 198)
        };
        _starterPanel.Append(_hintText);

        _background.Width.Set(626, 0);
        _background.Height.Set(221, 0);
        _background.HAlign = 0.5f;
        _background.Top.Set(53, 0);
        for (var i = 0; i < _starters.Length; i++)
        {
            var starter = _starters[i];
            var item = new StarterButton(ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(starter)}_Mini",
                AssetRequestMode.ImmediateLoad), starter);
            item.Width.Set(80, 0);
            item.Height.Set(60, 0);
            item.Left.Set(30 + (int)(i / 3f) * 80, 0);
            switch ((i + 1) % 3)
            {
                case 1:
                    item.Top.Set(16, 0);
                    break;
                case 2:
                    item.Top.Set(76, 0);
                    break;
                case 0:
                    item.Top.Set(136, 0);
                    break;
            }

            _background.Append(item);
        }

        _starterPanel.Append(_background);
        Append(_starterPanel);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (_starterPanelShowing && !Main.drawingPlayerChat && Main.keyState.IsKeyDown(Keys.Back))
        {
            _showButton.SetIsActive(true);
            SoundEngine.PlaySound(SoundID.MenuClose);
            _starterPanel.VAlign = -2f;
            _starterPanelShowing = false;
        }

        Recalculate();
    }
}

public class StarterButton : UIHoverImage
{
    public StarterButton(Asset<Texture2D> texture, ushort pokemon) : base(texture,
        Terramon.DatabaseV2.IsAvailableStarter(pokemon)
            ? Terramon.DatabaseV2.GetLocalizedPokemonName(pokemon).Value
            : Language.GetTextValue("Mods.Terramon.GUI.Starter.ComingSoon"))
    {
        if (!Terramon.DatabaseV2.IsAvailableStarter(pokemon)) return;
        var cacheHoverTexture = ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(pokemon)}_Mini_Highlighted",
            AssetRequestMode.ImmediateLoad);
        OnMouseOver += (_, _) =>
        {
            SetImage(cacheHoverTexture);
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        OnMouseOut += (_, _) => { SetImage(texture); };
        OnLeftClick += (_, _) =>
        {
            var player = TerramonPlayer.LocalPlayer;
            var data = PokemonData.Create(Main.LocalPlayer, pokemon, 5);
            if (ModContent.GetInstance<GameplayConfig>().ShinyLockedStarters && data.IsShiny)
                data.IsShiny = false;
            player.AddPartyPokemon(data);
            player.HasChosenStarter = true;
            var chosenMessage = Language.GetText("Mods.Terramon.GUI.Starter.ChosenMessage").WithFormatArgs(
                Terramon.DatabaseV2.GetPokemonSpecies(pokemon).Value,
                TypeID.GetColor(Terramon.DatabaseV2.GetPokemon(pokemon).Types[0]),
                Terramon.DatabaseV2.GetLocalizedPokemonName(pokemon).Value
            ).Value;
            Main.NewText(chosenMessage);
            SoundEngine.PlaySound(SoundID.Coins);
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(),
                ModContent.ItemType<PokeBallItem>(), 5);
        };
    }
}
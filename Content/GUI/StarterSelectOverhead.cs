using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items.PokeBalls;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public sealed class StarterSelectOverhead : SmartUIState
{
    private readonly LocalizedText _hintLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Hint");

    private readonly ushort[] _starters =
    [
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
    ];

    private readonly LocalizedText _subtitleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Subtitle");

    private readonly LocalizedText _titleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Title");

    private UIHoverImageButton _showButton;
    private UIContainer _starterPanel;
    private bool _starterPanelShowing = true;

    public override bool Visible => false;
        /*!Main.playerInventory && !Main.inFancyUI && !Main.LocalPlayer.dead &&
        !TerramonPlayer.LocalPlayer.HasChosenStarter &&
        Main.LocalPlayer.talkNPC < 0;*/

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        _showButton = new UIHoverImageButton(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Notification"),
            string.Empty);
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
        _showButton.SetIsActive(false);
        Append(_showButton);

        _starterPanel = new UIContainer(new Vector2(660, 330))
        {
            HAlign = 0.5f,
            VAlign = 0.19f
        };

        var titleText = new BetterUIText(_titleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        var subText = new BetterUIText(_subtitleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        titleText.HAlign = 0.5f;
        subText.Top.Set(26, 0);
        subText.HAlign = 0.5f;
        titleText.Append(subText);
        _starterPanel.Append(titleText);

        var background = new UIBlendedImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Background"));
        var hintText = new BetterUIText(_hintLocalizedText, 0.85f)
        {
            HAlign = 0.5f,
            Top = { Pixels = 281 },
            TextColor = new Color(173, 173, 198),
            RemoveFloatingPointsFromDrawPosition = true
        };
        _starterPanel.Append(hintText);

        background.Width.Set(626, 0);
        background.Height.Set(221, 0);
        background.HAlign = 0.5f;
        background.Top.Set(53, 0);
        for (var i = 0; i < _starters.Length; i++)
        {
            var starter = _starters[i];
            var item = new StarterButton(ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(starter)}_Mini"), starter);
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

            background.Append(item);
        }

        _starterPanel.Append(background);
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
            ? Terramon.DatabaseV2.GetLocalizedPokemonName(pokemon)
            : Language.GetText("Mods.Terramon.GUI.Starter.ComingSoon"))
    {
        RemoveFloatingPointsFromDrawPosition = true;
        
        if (!Terramon.DatabaseV2.IsAvailableStarter(pokemon))
        {
            OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                {
                    Volume = 0.25f
                });
            };
            return;
        }

        var cacheHoverTexture = ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(pokemon)}_Mini_Highlighted");
        OnMouseOver += (_, _) =>
        {
            SetImage(cacheHoverTexture);
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        OnMouseOut += (_, _) => { SetImage(texture); };
        OnLeftClick += (_, _) =>
        {
            var player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<TerramonPlayer>();
            var data = PokemonData.Create(player, pokemon, 5);
            if (ModContent.GetInstance<GameplayConfig>().ShinyLockedStarters && data.IsShiny)
                data.IsShiny = false;
            modPlayer.AddPartyPokemon(data, out _);
            modPlayer.HasChosenStarter = true;
            var schema = data.Schema;
            var chosenMessage = Language.GetText("Mods.Terramon.GUI.Starter.ChosenMessage").Format(
                DatabaseV2.GetPokemonSpeciesDirect(schema),
                schema.Types[0].GetHexColor(),
                data.LocalizedName
            );
            Main.NewText(chosenMessage);
            SoundEngine.PlaySound(SoundID.Coins);
            var itemType = ModContent.ItemType<PokeBallItem>();
            if (player.name is "Jamz" or "JamzOJamz") // Developer easter egg
                itemType = ModContent.ItemType<MasterBallItem>();
            player.QuickSpawnItem(player.GetSource_GiftOrReward(),
                itemType, 10);
        };
    }
}
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items.Mechanical;
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
    private readonly ushort[] Starters =
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

    private UIBlendedImage background;
    private UIText hintText;
    private UIHoverImageButton showButton;
    private bool starterPanelShowing = true;
    private UIText titleText;
    private UIContainer starterPanel;

    public override bool Visible =>
        !Main.playerInventory && !Main.LocalPlayer.dead && !TerramonPlayer.LocalPlayer.HasChosenStarter && Main.LocalPlayer.talkNPC < 0;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        showButton = new UIHoverImageButton(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Notification",
            AssetRequestMode.ImmediateLoad), string.Empty);
        showButton.Width.Set(42, 0);
        showButton.Height.Set(40, 0);
        showButton.HAlign = 1;
        showButton.VAlign = 1;
        showButton.MarginRight = 10;
        showButton.MarginBottom = 10;
        showButton.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
        showButton.OnLeftClick += (_, _) =>
        {
            showButton.SetIsActive(false);
            SoundEngine.PlaySound(SoundID.MenuOpen);
            starterPanel.VAlign = 0.19f;
            starterPanelShowing = true;
        };
        Append(showButton);

        starterPanel = new UIContainer(new Vector2(660, 330))
        {
            HAlign = 0.5f,
            VAlign = 0.19f,
        };

        titleText = new UIText("Welcome to the world of Pokémon! Thank you for installing the Terramon mod!");
        var subText = new UIText("Now, please choose your starter Pokémon!");
        titleText.HAlign = 0.5f;
        subText.Top.Set(26, 0);
        subText.HAlign = 0.5f;
        titleText.Append(subText);
        starterPanel.Append(titleText);
        
        background = new UIBlendedImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Background",
            AssetRequestMode.ImmediateLoad));
        hintText = new UIText("(Press Backspace to Close)", 0.85f)
        {
            HAlign = 0.5f,
            Top = {Pixels = 281},
            TextColor = new Color(173, 173, 198)
        };
        starterPanel.Append(hintText);
        
        background.Width.Set(626, 0);
        background.Height.Set(221, 0);
        background.HAlign = 0.5f;
        background.Top.Set(53, 0);
        for (var i = 0; i < Starters.Length; i++)
        {
            var starter = Starters[i];
            var item = new StarterButton(ModContent.Request<Texture2D>(
                $"Terramon/Assets/Pokemon/{Terramon.Database.GetPokemonName(starter)}_Mini",
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

            background.Append(item);
        }
        
        starterPanel.Append(background);
        Append(starterPanel);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (starterPanelShowing && !Main.drawingPlayerChat && Main.keyState.IsKeyDown(Keys.Back))
        {
            showButton.SetIsActive(true);
            SoundEngine.PlaySound(SoundID.MenuClose);
            starterPanel.VAlign = -2f;
            starterPanelShowing = false;
        }

        Recalculate();
    }
}

public class StarterButton : UIHoverImage
{
    public StarterButton(Asset<Texture2D> texture, ushort pokemon) : base(texture,
        Terramon.Database.IsAvailableStarter(pokemon)
            ? Terramon.Database.GetLocalizedPokemonName(pokemon).Value
            : "Coming soon...")
    {
        if (!Terramon.Database.IsAvailableStarter(pokemon)) return;
        var cacheHoverTexture = ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.Database.GetPokemonName(pokemon)}_Mini_Highlighted",
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
            player.AddPartyPokemon(new PokemonData(pokemon, 5));
            player.HasChosenStarter = true;
            var chosenMessage = Language.GetText("Mods.Terramon.GUI.Starter.ChosenMessage").WithFormatArgs(
                TypeID.GetColor(Terramon.Database.GetPokemon(pokemon).Types[0]),
                Terramon.Database.GetLocalizedPokemonName(pokemon).Value,
                Terramon.Database.GetPokemonSpecies(pokemon).Value
            ).Value;
            Main.NewText(chosenMessage);
            SoundEngine.PlaySound(SoundID.Coins);
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(),
                ModContent.ItemType<PokeBallItem>(), 5);
        };
    }
}
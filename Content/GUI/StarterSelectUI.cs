using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public sealed class StarterSelectUI : SmartUIState
{
    private readonly UIStarterBanner[] _banners = new UIStarterBanner[3];
    private readonly LocalizedText _hintLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Hint");

    private readonly ushort[] _starters =
    [
        NationalDexID.Bulbasaur,
        NationalDexID.Charmander,
        NationalDexID.Squirtle
    ];

    private readonly LocalizedText _subtitleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Subtitle");
    private readonly LocalizedText _titleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Title");
    private BetterUIText _hintText;
    private float _hintTextAlpha;
    private ITweener _hintTextTween;
    private UIHoverImageButton _showButton;
    private bool _starterPanelShowing = true;
    private UIContainer _topContainer;

    public override bool Visible =>
        !TerramonPlayer.LocalPlayer.HasChosenStarter && !Main.playerInventory && !Main.inFancyUI &&
        !Main.LocalPlayer.dead && Main.LocalPlayer.talkNPC < 0;

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
        _showButton.OnMouseOver += (_, _) =>
        {
            if (_starterPanelShowing) return;
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        _showButton.OnLeftClick += (_, _) =>
        {
            if (_starterPanelShowing) return;
            _showButton.SetIsActive(false);
            SoundEngine.PlaySound(SoundID.MenuOpen);
            _starterPanelShowing = true;
        };
        _showButton.SetIsActive(false);
        Append(_showButton);

        _topContainer = new UIContainer(new Vector2(372, 314))
        {
            HAlign = 0.5f
        };
        _topContainer.Top.Set(-157 + 40, 0.25f);

        var backdropImage = new UIImage(
            ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Backdrop"))
        {
            RemoveFloatingPointsFromDrawPosition = true,
            Color = Color.White * 0.2f
        };
        backdropImage.Width.Set(1028, 0f);
        backdropImage.Height.Set(589, 0f);
        backdropImage.Top.Set(-146, 0f);
        backdropImage.Left.Set(-329, 0f);
        _topContainer.Append(backdropImage);

        var titleText = new BetterUIText(_titleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            TextColor = new Color(239, 245, 255)
        };
        var subText = new BetterUIText(_subtitleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            TextColor = new Color(239, 245, 255)
        };
        titleText.HAlign = 0.5f;
        subText.Top.Set(26, 0);
        subText.HAlign = 0.5f;
        titleText.Append(subText);
        _topContainer.Append(titleText);

        var generationText = new BetterUIText(
            "Generation I (Kanto)", 0.55f, true)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            ShadowSpread = 1.88f,
            HAlign = 0.5f
        };
        generationText.Top.Set(91, 0f);
        _topContainer.Append(generationText);

        for (var i = 0; i < _banners.Length; i++)
        {
            var banner = new UIStarterBanner(_starters[i]);
            _banners[i] = banner;
            banner.Top.Set(130, 0f);
            banner.Left.Set(i * 128, 0f);
            _topContainer.Append(banner);
        }

        Append(_topContainer);

        _hintText = new BetterUIText(_hintLocalizedText, 0.86f)
        {
            HAlign = 0.5f,
            TextColor = new Color(173, 173, 198),
            RemoveFloatingPointsFromDrawPosition = true
        };
        _hintText.Top.Set(90, 0.5f);
        Append(_hintText);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (_starterPanelShowing && !Main.drawingPlayerChat && Main.keyState.IsKeyDown(Keys.Back))
        {
            _showButton.SetIsActive(true);
            SoundEngine.PlaySound(SoundID.MenuClose);
            _starterPanelShowing = false;
        }

        _topContainer.Top.Set(-157 + 40, _starterPanelShowing ? 0.25f : 4f);
        _hintText.Top.Set(90, _starterPanelShowing ? 0.5f : 4f);

        if (_starterPanelShowing && _hintTextTween is not { IsRunning: true })
            _hintTextTween = Tween.To(() => _hintTextAlpha, a => _hintTextAlpha = a, _hintTextAlpha == 1f ? 0f : 1f,
                1f);

        Recalculate();
    }


    public override void Draw(SpriteBatch spriteBatch)
    {
        _hintText.TextColor = Color.Lerp(new Color(173, 173, 198), new Color(135, 135, 163), _hintTextAlpha);

        base.Draw(spriteBatch);
    }
}

internal sealed class UIStarterBanner : UIHoverImageButton
{
    private static readonly Asset<Texture2D> ShadowTexture;
    private readonly ushort _id;
    private readonly UIImage _miniTexture;
    private readonly UIImage _shadow;
    private readonly BetterUIText _speciesText;
    private readonly BetterUIText _suffixText;
    private int _hoverTextOverrideTimeLeft;
    private int _jumpCount;
    private int _jumpTime;

    private Vector2? _lastMousePosition;
    private int _lastXDirection;
    private int _shakeCount;

    static UIStarterBanner()
    {
        if (Main.dedServ) return;

        ShadowTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Shadow");
    }

    public UIStarterBanner(ushort id) : base(ModContent.Request<Texture2D>("Terraria/Images/NPC_0"), "Pick this one!")
    {
        _id = id;

        var mainType = Terramon.DatabaseV2.GetPokemon(id).Types[0];
        var texturePath = $"Terramon/Assets/GUI/Starter/Banner{mainType}";
        if (!ModContent.HasAsset(texturePath))
            texturePath = "Terramon/Assets/GUI/Starter/BannerNormal";
        var hoverTexturePath = $"{texturePath}Hover";
        SetImage(ModContent.Request<Texture2D>(texturePath));
        SetHoverImage(ModContent.Request<Texture2D>(
            hoverTexturePath));

        RemoveFloatingPointsFromDrawPosition = true;
        Width.Set(114, 0f);
        Height.Set(184, 0f);
        SetVisibility(1f, 1f);

        var nameText = new BetterUIText(Terramon.DatabaseV2.GetLocalizedPokemonName(id), 0.97f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        };
        nameText.Width.Set(120, 0f);
        nameText.Height.Set(30, 0f);
        nameText.Top.Set(10, 0f);
        Append(nameText);

        var species = Terramon.DatabaseV2.GetPokemonSpeciesDirect(id);
        var speciesSplit = species.Split(' ');
        var speciesMod = string.Join(" ", speciesSplit.Take(speciesSplit.Length - 1));
        var suffix = speciesSplit.Last();
        _speciesText = new BetterUIText(speciesMod, 0.85f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextColor = new Color(239, 245, 255)
        };
        _speciesText.Top.Set(109, 0f);
        Append(_speciesText);
        _suffixText = new BetterUIText(suffix, 0.85f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextColor = new Color(239, 245, 255)
        };
        _suffixText.Top.Set(126, 0f);
        Append(_suffixText);

        _shadow = new UIImage(ShadowTexture)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _shadow.Left.Set(38, 0f);
        _shadow.Top.Set(78, 0f);
        Append(_shadow);

        _miniTexture = new UIImage(ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(id)}_Mini"))
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _miniTexture.Left.Set(16, 0f);
        _miniTexture.Top.Set(38, 0f);
        Append(_miniTexture);
    }

    public override void Update(GameTime gameTime)
    {
        var mouseOverThis = ContainsPoint(Main.MouseScreen);

        if (mouseOverThis)
            if (!JustHovered)
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Item_32") { Volume = 0.4f });

        base.Update(gameTime);

        if (_hoverTextOverrideTimeLeft > 0)
        {
            _hoverTextOverrideTimeLeft--;
            if (_hoverTextOverrideTimeLeft == 0) SetHoverText("Pick this one!");
        }

        if (mouseOverThis)
        {
            _lastMousePosition ??= Main.MouseScreen;

            var xDistance = Main.MouseScreen.X - _lastMousePosition.Value.X;

            // Detect horizontal movement of the mouse over the banner
            if (_hoverTextOverrideTimeLeft == 0 && Math.Abs(xDistance) > 8.5f)
            {
                if ((xDistance > 0 && _lastXDirection < 0) || (xDistance < 0 && _lastXDirection > 0)) _shakeCount++;

                // Trigger the Pokémon's cry sound effect! :)
                if (_shakeCount == 12)
                {
                    _shakeCount = 0;
                    _hoverTextOverrideTimeLeft = 150;
                    var cry = new SoundStyle("Terramon/Sounds/Cries/" + Terramon.DatabaseV2.GetPokemonName(_id))
                        { Volume = 0.15f };
                    SetHoverText(Terramon.DatabaseV2.GetLocalizedPokemonName(_id) + GetRandomHoverText());
                    SoundEngine.PlaySound(cry);
                }

                _lastXDirection = xDistance > 0 ? 1 : -1;
            }

            _lastMousePosition = Main.MouseScreen;

            _jumpTime++;
            if (_jumpTime > 28)
            {
                _jumpCount++;
                if (_jumpCount > 1)
                {
                    _jumpCount = 0;
                    _jumpTime = -75;
                }
                else
                {
                    _jumpTime = 0;
                }
            }

            if (_jumpTime < 0) return;
            var jumpHeight = GravitySim(_jumpTime / 3.5f);
            var shadowScale = MathHelper.Lerp(0.85f, 1f, (jumpHeight - 28f) / 8f);
            _miniTexture.Top.Set(jumpHeight + 2f, 0f);
            _shadow.ImageScale = shadowScale;
        }
        else
        {
            _lastMousePosition = null;
            _lastXDirection = 0;
            _shakeCount = 0;
            _jumpTime = 0;
            _jumpCount = 0;
            _miniTexture.Top.Set(38, 0f);
            _shadow.ImageScale = 1f;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (ContainsPoint(Main.MouseScreen))
        {
            _speciesText.Top.Set(119, 0f);
            _suffixText.Top.Set(136, 0f);
        }
        else
        {
            _speciesText.Top.Set(109, 0f);
            _suffixText.Top.Set(126, 0f);
        }

        _speciesText.Recalculate();
        _suffixText.Recalculate();
        base.Draw(spriteBatch);
    }

    private static float GravitySim(float x)
    {
        return 0.5f * x * x - 4f * x + 36f;
    }

    private static string GetRandomHoverText()
    {
        string[] starterTexts =
        [
            " seems ready to get started!",
            " looks like it’s in a good mood!",
            " is full of excitement!",
            " seems curious about you!",
            " looks like it’s waiting for something!",
            " seems to have a spark of energy!",
            " is bouncing with excitement!",
            " looks like it’s ready to play!",
            " is eager to show you what it can do!",
            " looks ready to impress!"
        ];

        return starterTexts[Main.rand.Next(starterTexts.Length)];
    }
}
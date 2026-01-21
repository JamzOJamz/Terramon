using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

public sealed class StarterSelectUI : SmartUIState
{
    private const float BackdropAlpha = 0.3f;
    private const float FadeDuration = 0.22f;
    private const int TopContainerOffset = -157 + 10;
    private const float ShowButtonOriginalMargin = 10f;
    private const float ShowButtonShakeInterval = 5f; // Measured in seconds
    private const float ShowButtonShakeDuration = 0.6f;

    private static UIImage _backdropImage;
    private static bool _fadeInAnimationActive;
    private static bool _fadeOutAnimationActive;
    private static BetterUIText _hintText;
    private static UIContainer _topContainer;
    private static BetterUIText _titleText;
    private static ITweener _backdropFadeTween;

    // private readonly UIStarterBanner[] _banners = new UIStarterBanner[3];

    private static readonly LocalizedText ComingSoonLocalizedText =
        Language.GetText("Mods.Terramon.GUI.Starter.ComingSoon");

    private static readonly LocalizedText HintLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Hint");

    private static readonly LocalizedText
        SubtitleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Subtitle");

    private static readonly LocalizedText TitleLocalizedText = Language.GetText("Mods.Terramon.GUI.Starter.Title");

    private static readonly LocalizedText GenerationLocalizedText =
        Language.GetText("Mods.Terramon.GUI.Starter.Generations.Gen1");

    private static readonly ushort[] Starters =
        [NationalDexID.Bulbasaur, NationalDexID.Charmander, NationalDexID.Squirtle];

    private float _hintTextAlpha;
    private ITweener _hintTextTween;
    private static UIHoverImageButton _showButton;
    private float _showButtonShakeElapsed = -1f;
    private float _showButtonShakeTimer;
    private ITweener _showButtonVisibilityTween;
    private static bool _starterPanelShowing;

    static StarterSelectUI()
    {
        On_WorldGen.playWorldCallBack += (orig, context) =>
        {
            SetPlayerNameForTitle(Main.LocalPlayer.name);
            orig(context);
        };
    }

    public override bool Visible => !ClientConfig.Instance.LegacyStarterSelectUI &&
                                    (!TerramonPlayer.LocalPlayer.HasChosenStarter ||
                                     (_fadeOutAnimationActive && _backdropImage.Color.A > 0)) &&
                                    !Main.playerInventory && !Main.inFancyUI && !Main.LocalPlayer.dead &&
                                    Main.LocalPlayer.talkNPC < 0;

    private static void SetPlayerNameForTitle(string playerName)
    {
        _titleText.SetText(Language.GetText("Mods.Terramon.GUI.Starter.Title").Format(playerName));
        _starterPanelShowing = false;
        _showButton.SetIsActive(true);
        _backdropImage.Color = Color.White * 0f;
        UILoader.GetUIState<StarterSelectUI>().SafeUpdate(null); // Update to set positions correctly
    }

    internal static void DoFadeInAnimation()
    {
        if (_fadeInAnimationActive) return;
        _fadeInAnimationActive = true;

        // Kill any existing tween  and reset animation state
        _backdropFadeTween?.Kill();
        _fadeOutAnimationActive = false;

        var startingAlpha = _backdropImage.Color.A / 255f;
        _backdropFadeTween = Tween.To(() => startingAlpha, a => _backdropImage.Color = Color.White * a, BackdropAlpha, FadeDuration);
        _backdropFadeTween.OnComplete = OnFadeInComplete;
    }

    private static void OnFadeInComplete()
    {
        _fadeInAnimationActive = false;
        _starterPanelShowing = true;
    }

    internal static void DoFadeOutAnimation()
    {
        if (_fadeOutAnimationActive) return;
        _fadeOutAnimationActive = true;

        HideUIElements();

        // Kill any existing tween and reset animation state
        _backdropFadeTween?.Kill();
        _fadeInAnimationActive = false;

        var startingAlpha = _backdropImage.Color.A / 255f;
        _backdropFadeTween = Tween.To(() => startingAlpha, a => _backdropImage.Color = Color.White * a, 0, FadeDuration);
        _backdropFadeTween.OnComplete = OnFadeOutComplete;
    }

    private static void HideUIElements()
    {
        _topContainer.Top.Set(0, float.MaxValue);
        _hintText.Top.Set(0, float.MaxValue);
    }

    private static void OnFadeOutComplete()
    {
        _fadeOutAnimationActive = false;
        _starterPanelShowing = false;
        //_backdropImage.Color = Color.White * BackdropAlpha;
        _topContainer.Top.Set(TopContainerOffset, 0.25f);
        _hintText.Top.Set(94, 0.5f);
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        _showButton = new UIHoverImageButton(
            ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Notification"), string.Empty);
        _showButton.Width.Set(42, 0);
        _showButton.Height.Set(40, 0);
        _showButton.HAlign = 1;
        _showButton.VAlign = 1;
        _showButton.MarginRight = ShowButtonOriginalMargin;
        _showButton.MarginBottom = ShowButtonOriginalMargin;
        _showButton.OnMouseOver += (_, _) =>
        {
            if (_starterPanelShowing && !_fadeOutAnimationActive) return;
            SoundEngine.PlaySound(in SoundID.MenuTick);
            _showButton.VisibilityOverride = -1f;
            _showButtonVisibilityTween?.Kill();
        };
        _showButton.OnLeftClick += (_, _) =>
        {
            if (_starterPanelShowing && !_fadeOutAnimationActive) return;
            _showButton.SetIsActive(false);
            SoundEngine.PlaySound(in SoundID.MenuOpen);
            DoFadeInAnimation();
        };
        // _showButton.SetIsActive(false);
        Append(_showButton);

        _topContainer = new UIContainer(new Vector2(494, 314)) { HAlign = 0.5f };
        _topContainer.Top.Set(TopContainerOffset, 0.25f);

        _backdropImage = new UIImage(ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/BackdropBig"))
        {
            RemoveFloatingPointsFromDrawPosition = true,
            Color = Color.White * 0f,
            ImageScale = 2.25f,
            HAlign = 0.5f
        };
        _backdropImage.Width.Set(1028, 0f);
        _backdropImage.Height.Set(589, 0f);
        _backdropImage.Top.Set(TopContainerOffset - 146, 0.25f);
        _backdropImage.Left.Set(32, 0f);
        Append(_backdropImage);

        _titleText = new BetterUIText(TitleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            TextColor = new Color(239, 245, 255),
            HAlign = 0.5f
        };
        var subText = new BetterUIText(SubtitleLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            TextColor = new Color(239, 245, 255),
            HAlign = 0.5f
        };
        subText.Top.Set(28, 0);
        _titleText.Append(subText);
        _topContainer.Append(_titleText);

        var generationText = new BetterUIText(GenerationLocalizedText, 0.605f, true)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            ShadowSpread = 1.88f,
            HAlign = 0.5f
        };
        generationText.Top.Set(86, 0f);
        _topContainer.Append(generationText);

        for (var i = 0; i < Starters.Length; i++)
        {
            var banner = new UIStarterBanner(Starters[i]);
            // _banners[i] = banner;
            banner.Top.Set(130, 0f);
            banner.Left.Set(i * 132 + 58, 0f);
            _topContainer.Append(banner);
        }

        Append(_topContainer);

        var pageLeftButton = new UIHoverImage(
            ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/PageButtonLeftDisabled"),
            ComingSoonLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        pageLeftButton.Width.Set(34, 0f);
        pageLeftButton.Height.Set(34, 0f);
        pageLeftButton.Left.Set(2, 0f);
        pageLeftButton.Top.Set(194, 0f);
        pageLeftButton.OnLeftClick += (_, _) => SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
        _topContainer.Append(pageLeftButton);

        var pageRightButton = new UIHoverImage(
            ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/PageButtonRightDisabled"),
            ComingSoonLocalizedText)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        pageRightButton.Width.Set(34, 0f);
        pageRightButton.Height.Set(34, 0f);
        pageRightButton.Left.Set(458, 0f);
        pageRightButton.Top.Set(194, 0f);
        pageRightButton.OnLeftClick += (_, _) => SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
        _topContainer.Append(pageRightButton);

        _hintText = new BetterUIText(HintLocalizedText)
        {
            HAlign = 0.5f,
            TextColor = new Color(193, 193, 226),
            RemoveFloatingPointsFromDrawPosition = true
        };
        _hintText.Top.Set(95, 0.5f);
        Append(_hintText);

        SafeUpdate(null); // Initial update to set positions correctly
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        var isVisibleCondition = _fadeOutAnimationActive || _fadeInAnimationActive || _starterPanelShowing;

        if (isVisibleCondition)
        {
            if (!Main.drawingPlayerChat && Main.keyState.IsKeyDown(Keys.Back) && !_fadeOutAnimationActive)
            {
                _showButton.SetIsActive(true);
                SoundEngine.PlaySound(in SoundID.MenuClose);
                DoFadeOutAnimation();
            }

            if (_hintTextTween is not { IsRunning: true })
                _hintTextTween = Tween.To(() => _hintTextAlpha, a => _hintTextAlpha = a,
                    _hintTextAlpha == 1f ? 0f : 1f, 1f);
        }

        UpdateShowButtonAnimation(gameTime);

        if (!_fadeOutAnimationActive)
        {
            _topContainer.Top.Set(TopContainerOffset, isVisibleCondition ? 0.25f : 4f);
            _backdropImage.Top.Set(TopContainerOffset - 146, isVisibleCondition ? 0.25f : 4f);
            _hintText.Top.Set(94, isVisibleCondition ? 0.5f : 4f);   
        }

        Recalculate();
    }

    private void UpdateShowButtonAnimation(GameTime gameTime)
    {
        if (_starterPanelShowing)
        {
            _showButtonShakeElapsed = -1f;
            _showButton.MarginRight = ShowButtonOriginalMargin;
            _showButton.VisibilityOverride = -1f;
            return;
        }

        var elapsed = gameTime == null ? 0f : (float)gameTime.ElapsedGameTime.TotalSeconds;
        _showButtonShakeTimer += elapsed;

        if (_showButtonShakeTimer >= ShowButtonShakeInterval && _showButtonShakeElapsed <= 0f)
        {
            _showButtonShakeTimer -= ShowButtonShakeInterval;
            _showButtonShakeElapsed = 0f;

            if (!_showButton.IsMouseHovering)
            {
                _showButton.VisibilityOverride = _showButton.VisibilityActive;
                _showButtonVisibilityTween = Tween.To(
                    () => _showButton.VisibilityOverride,
                    v => _showButton.VisibilityOverride = v,
                    _showButton.VisibilityInactive,
                    ShowButtonShakeDuration);
                _showButtonVisibilityTween.OnComplete = () => _showButton.VisibilityOverride = -1f;
            }
        }

        if (_showButtonShakeElapsed is >= 0f and < ShowButtonShakeDuration)
        {
            _showButtonShakeElapsed += elapsed;
            var t = _showButtonShakeElapsed / ShowButtonShakeDuration;
            var easeOut = 1f - t;
            const float freq = 12f;
            const float amplitude = 6f;
            var offset = MathF.Sin(t * freq * MathF.PI * 2f) * amplitude * easeOut;
            _showButton.MarginRight = ShowButtonOriginalMargin + offset;
        }
        else if (_showButtonShakeElapsed >= ShowButtonShakeDuration)
        {
            _showButtonShakeElapsed = -1f;
            _showButton.MarginRight = ShowButtonOriginalMargin;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        _hintText.TextColor = Color.Lerp(new Color(193, 193, 226), new Color(157, 157, 184), _hintTextAlpha);
        base.Draw(spriteBatch);
    }
}

internal sealed class UIStarterBanner : UIHoverImageButton
{
    private static readonly Asset<Texture2D> ShadowTexture;

    private static readonly LocalizedText PickThisOneLocalizedText =
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.PickThisOne");

    private static readonly LocalizedText[] SecretHoverTextLocalizedTexts =
    [
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.ReadyToStart"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.GoodMood"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.FullOfExcitement"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.Curious"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.Waiting"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.SparkOfEnergy"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.BouncingWithExcitement"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.ReadyToPlay"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.EagerToShow"),
        Language.GetText("Mods.Terramon.GUI.Starter.Banner.SecretHoverText.ReadyToImpress")
    ];

    private readonly UIImage _miniTexture;
    private readonly ushort _pokemon;
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

    public UIStarterBanner(ushort pokemon) : base(TextureAssets.Npc[0], PickThisOneLocalizedText)
    {
        _pokemon = pokemon;

        var mainType = Terramon.DatabaseV2.GetPokemon(pokemon).Types[0];
        var texturePath = $"Terramon/Assets/GUI/Starter/Banner{mainType}";
        if (!ModContent.HasAsset(texturePath))
            texturePath = "Terramon/Assets/GUI/Starter/BannerNormal";
        SetImage(ModContent.Request<Texture2D>(texturePath));
        SetHoverImage(ModContent.Request<Texture2D>($"{texturePath}Hover"));

        OnLeftClick += OnBannerClicked;

        RemoveFloatingPointsFromDrawPosition = true;
        Width.Set(114, 0f);
        Height.Set(184, 0f);
        SetVisibility(1f, 1f);

        var nameText = new BetterUIText(Terramon.DatabaseV2.GetLocalizedPokemonName(pokemon), 0.972f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        };
        nameText.Width.Set(120, 0f);
        nameText.Height.Set(30, 0f);
        nameText.Top.Set(9, 0f);
        Append(nameText);

        var species = Terramon.DatabaseV2.GetPokemonSpeciesDirect(pokemon);
        var speciesSplit = species.Split(' ');
        var speciesMod = string.Join(" ", speciesSplit.Take(speciesSplit.Length - 1));
        var suffix = speciesSplit.Last();

        _speciesText = new BetterUIText(speciesMod, 0.87f)
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

        _shadow = new UIImage(ShadowTexture) { RemoveFloatingPointsFromDrawPosition = true };
        _shadow.Left.Set(38, 0f);
        _shadow.Top.Set(78, 0f);
        Append(_shadow);

        _miniTexture = new UIImage(ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(pokemon)}_Mini"))
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _miniTexture.Left.Set(16, 0f);
        _miniTexture.Top.Set(38, 0f);
        Append(_miniTexture);
    }

    private void OnBannerClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        var player = Main.LocalPlayer;
        var modPlayer = player.Terramon();
        var dataBuilder = PokemonData.Create(_pokemon, 5).CaughtBy(player);
        if (GameplayConfig.Instance.ShinyLockedStarters)
            dataBuilder.ForceShiny(false);
        var data = dataBuilder.Build();
        modPlayer.AddPartyPokemon(data, out _);
        modPlayer.HasChosenStarter = true;
        StarterSelectUI.DoFadeOutAnimation();

        var schema = data.Schema;
        var chosenMessage = Language.GetText("Mods.Terramon.GUI.Starter.ChosenMessage")
            .Format(DatabaseV2.GetPokemonSpeciesDirect(schema), schema.Types[0].GetHexColor(), data.LocalizedName);
        Main.NewText(chosenMessage);
        SoundEngine.PlaySound(in SoundID.Coins);

        var ballItemType = ModContent.ItemType<PokeBallItem>();
        if (player.name is "Jamz" or "JamzOJamz") // Developer easter egg
            ballItemType = ModContent.ItemType<MasterBallItem>();
        var giftItemSource = player.GetSource_GiftOrReward();
        player.QuickSpawnItem(giftItemSource, ballItemType, 10);
        player.QuickSpawnItem(giftItemSource, ModContent.ItemType<Potion>(), 3);
    }

    public override void Update(GameTime gameTime)
    {
        var mouseOverThis = ContainsPoint(Main.MouseScreen);

        if (mouseOverThis && !JustHovered)
            SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.3f });

        base.Update(gameTime);

        if (_hoverTextOverrideTimeLeft > 0)
        {
            _hoverTextOverrideTimeLeft--;
            if (_hoverTextOverrideTimeLeft == 0)
                SetHoverText(PickThisOneLocalizedText);
        }

        if (mouseOverThis)
        {
            HandleMouseInteraction();
            UpdateJumpAnimation();
        }
        else
        {
            ResetAnimationState();
        }
    }

    private void HandleMouseInteraction()
    {
        _lastMousePosition ??= Main.MouseScreen;

        var xDistance = Main.MouseScreen.X - _lastMousePosition.Value.X;

        if (_hoverTextOverrideTimeLeft == 0 && Math.Abs(xDistance) > 8.5f)
        {
            if ((xDistance > 0 && _lastXDirection < 0) || (xDistance < 0 && _lastXDirection > 0))
                _shakeCount++;

            if (_shakeCount == 12)
            {
                _shakeCount = 0;
                _hoverTextOverrideTimeLeft = 150;
                var cry = TerramonSoundID.GetCry(_pokemon);
                SetHoverText(GetRandomSecretHoverText());
                SoundEngine.PlaySound(in cry);
            }

            _lastXDirection = xDistance > 0 ? 1 : -1;
        }

        _lastMousePosition = Main.MouseScreen;
    }

    private void UpdateJumpAnimation()
    {
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

    private void ResetAnimationState()
    {
        _lastMousePosition = null;
        _lastXDirection = 0;
        _shakeCount = 0;
        _jumpTime = 0;
        _jumpCount = 0;
        _miniTexture.Top.Set(38, 0f);
        _shadow.ImageScale = 1f;
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

    private static float GravitySim(float x) => 0.5f * x * x - 4f * x + 36f;

    private string GetRandomSecretHoverText()
    {
        return SecretHoverTextLocalizedTexts[Main.rand.Next(SecretHoverTextLocalizedTexts.Length)]
            .WithFormatArgs(Terramon.DatabaseV2.GetLocalizedPokemonName(_pokemon)).Value;
    }
}
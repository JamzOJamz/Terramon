using ReLogic.Content;
using Terramon.Content.GUI.Common;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Terramon.Content.GUI;

public sealed class StarterSelectUI : SmartUIState
{
    private readonly UIStarterBanner[] _banners = new UIStarterBanner[3];

    private readonly ushort[] _starters =
    [
        NationalDexID.Bulbasaur,
        NationalDexID.Charmander,
        NationalDexID.Squirtle
    ];

    public override bool Visible =>
        !Main.playerInventory && !Main.inFancyUI && !Main.LocalPlayer.dead &&
        !TerramonPlayer.LocalPlayer.HasChosenStarter &&
        Main.LocalPlayer.talkNPC < 0;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public override void OnInitialize()
    {
        var topContainer = new UIContainer(new Vector2(362, 210))
        {
            HAlign = 0.5f
        };
        topContainer.Top.Set(Main.screenHeight / 4f - 105, 0f);

        for (var i = 0; i < _banners.Length; i++)
        {
            var banner = new UIStarterBanner(_starters[i]);
            _banners[i] = banner;
            banner.Top.Set(56, 0f);
            banner.Left.Set(i * 126, 0f);
            topContainer.Append(banner);
        }

        Append(topContainer);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        Recalculate();
    }
}

internal sealed class UIStarterBanner : UIHoverImageButton
{
    private static readonly Asset<Texture2D> BackgroundTexture;
    private static readonly Asset<Texture2D> BackgroundHoverTexture;
    private static readonly Asset<Texture2D> ShadowTexture;
    private readonly UIImage _miniTexture;
    private readonly UIImage _shadow;
    private int _jumpCount;
    private int _jumpTime;
    private bool _nameGrowing = true;
    private readonly BetterUIText _nameText;

    private ITweener _nameTween;

    static UIStarterBanner()
    {
        if (Main.dedServ) return;

        BackgroundTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Banner");
        BackgroundHoverTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/BannerHover");
        ShadowTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Starter/Shadow");
    }

    public UIStarterBanner(ushort id) : base(BackgroundTexture, string.Empty)
    {
        RemoveFloatingPointsFromDrawPosition = true;
        Width.Set(110, 0f);
        Height.Set(154, 0f);
        SetHoverImage(BackgroundHoverTexture);
        SetVisibility(1f, 1f);

        _nameText = new BetterUIText(Terramon.DatabaseV2.GetLocalizedPokemonName(id), 0.97f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextOriginY = 0.5f
        };
        _nameText.Height.Set(30, 0f);
        _nameText.Top.Set(7, 0f);
        Append(_nameText);

        var species = Terramon.DatabaseV2.GetPokemonSpeciesDirect(id);
        var speciesSplit = species.Split(' ');
        var speciesMod = string.Join(" ", speciesSplit.Take(speciesSplit.Length - 1));
        var suffix = speciesSplit.Last();
        var speciesText = new BetterUIText(speciesMod, 0.85f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextColor = new Color(232, 241, 255)
        };
        speciesText.Top.Set(105, 0f);
        Append(speciesText);
        var suffixText = new BetterUIText(suffix, 0.85f)
        {
            RemoveFloatingPointsFromDrawPosition = true,
            HAlign = 0.5f,
            TextColor = new Color(232, 241, 255)
        };
        suffixText.Top.Set(122, 0f);
        Append(suffixText);

        _shadow = new UIImage(ShadowTexture)
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _shadow.Left.Set(36, 0f);
        _shadow.Top.Set(76, 0f);
        Append(_shadow);

        _miniTexture = new UIImage(ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Terramon.DatabaseV2.GetPokemonName(id)}_Mini"))
        {
            RemoveFloatingPointsFromDrawPosition = true
        };
        _miniTexture.Left.Set(14, 0f);
        _miniTexture.Top.Set(36, 0f);
        Append(_miniTexture);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (ContainsPoint(Main.MouseScreen))
        {
            if (_nameTween is not { IsRunning: true })
            {
                _nameTween = Tween.To(() => _nameText.TextScale, _nameText.SetTextScale, _nameGrowing ? 1.1f : 0.97f,
                    1f / 3f);
                _nameGrowing = !_nameGrowing;
            }

            _jumpTime++;
            if (_jumpTime > 32)
            {
                _jumpCount++;
                if (_jumpCount > 1)
                {
                    _jumpCount = 0;
                    _jumpTime = -85;
                }
                else
                {
                    _jumpTime = 0;
                }
            }

            if (_jumpTime < 0) return;
            var jumpHeight = GravitySim(_jumpTime / 4f);
            var shadowScale = MathHelper.Lerp(0.85f, 1f, (jumpHeight - 28f) / 8f);
            _miniTexture.Top.Set(jumpHeight, 0f);
            _shadow.ImageScale = shadowScale;
        }
        else
        {
            if (_nameTween is { IsRunning: true }) _nameTween.Kill();
            _jumpTime = 0;
            _jumpCount = 0;
            _miniTexture.Top.Set(36, 0f);
            _shadow.ImageScale = 1f;
            _nameText.SetTextScale(0.97f);
            _nameGrowing = true;
        }
    }

    private static float GravitySim(float x)
    {
        return 0.5f * x * x - 4f * x + 36f;
    }
}
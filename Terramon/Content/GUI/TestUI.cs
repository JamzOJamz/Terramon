using Microsoft.Xna.Framework.Graphics;
using rail;
using ReLogic.Content;
using System.Runtime.InteropServices;
using Terramon.Content.NPCs;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;
public sealed class TestBattleUI : SmartUIState
{
    public static TestBattleUI Instance { get; private set; }
    public static Asset<Texture2D> Forest;
    private readonly static UIElement _optionsPanel;
    private readonly static UIElement _movesPanel;
    private readonly static UIElement _pokemonPanel;
    private static DynamicPixelRatioElement _mainPanel;
    static TestBattleUI()
    {
        // Create options panel
        _optionsPanel = new();
        _optionsPanel.Left.Percent = _optionsPanel.Width.Percent = 0.5f;
        _optionsPanel.Height.Percent = 1f;
        for (ButtonType i = ButtonType.Fight; i <= ButtonType.Run; i++)
        {
            int xFactor = (int)i % 2;
            int yFactor = (int)i / 2;
            DynamicPixelRatioElement button = new($"Terramon/Assets/GUI/TurnBased/{i}Button", buttonLike: true);
            button.Left.Percent = xFactor * 0.5f;
            button.Top.Percent = yFactor * 0.5f;
            button.Width.Percent = button.Height.Percent = 0.5f;
            button.OnLeftClick += i switch
            {
                ButtonType.Fight => FightButton,
                ButtonType.Bag => BagButton,
                ButtonType.Pokemon => PokemonButton,
                ButtonType.Run => RunButton,
                _ => null,
            };
            UIText label = new(Terramon.Instance.GetLocalization($"GUI.TurnBased.ButtonLabels.{i}"), 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            _optionsPanel.Append(button);
        }

        // Create moves panel (blank)
        _movesPanel = new();
        _movesPanel.Width.Percent = _movesPanel.Height.Percent = 1f;
        for (int i = 0; i < 4; i++)
        {
            float xFactor = i % 2 * 0.5f;
            float yFactor = i / 2 * 0.5f;
            DynamicPixelRatioElement button = new(null, null, buttonLike: true);
            button.Left.Percent = xFactor;
            button.Top.Percent = yFactor;
            button.Width.Percent = button.Height.Percent = 0.5f;
            UIText label = new(string.Empty, 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            button.Append(new MoveReference());
            button.OnLeftClick += ClickMoveButton;
            _movesPanel.Append(button);
        }

        // Create Pokémon panel (blank)
        _pokemonPanel = new();
        _pokemonPanel.Width.Percent = _pokemonPanel.Height.Percent = 1f;
        for (int i = 0; i < 6; i++)
        {
            float xFactor = i % 3 / 3f;
            float yFactor = i / 9f;
            DynamicPixelRatioElement button = new("Terramon/Assets/GUI/TurnBased/Simple", buttonLike: true);
            button.Left.Percent = xFactor;
            button.Top.Percent = yFactor;
            button.Width.Percent = 1f / 3f;
            button.Height.Percent = 0.5f;
            UIText label = new(string.Empty, 0.75f, true)
            {
                HAlign = 0.6f,
                VAlign = 0.5f
            };
            UIImage image = new(TextureAssets.Npc[0])
            {
                VAlign = 0.5f
            };
            button.Append(label);
            button.Append(image);
            button.Append(new PokemonReference());
            button.OnLeftClick += ClickPokemonButton;
            _pokemonPanel.Append(button);
        }
    }

    public TestBattleUI()
    {
        Instance = this;
    }
    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        if (_opened)
        {
            foreach (var layer in CollectionsMarshal.AsSpan(layers))
            {
                var name = layer.Name;
                if (name is "Vanilla: Resource Bars" or "Vanilla: Hotbar" or "Vanilla: Cursor" or "Vanilla: Player Chat")
                    continue;
                layer.Active = false;
            }
        }

        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }
    public override bool Visible => _opened;
    public static void HandleExit()
    {
        if (_optionsPanel.Parent != null)
            return;
        ChangePanel(_optionsPanel);
    }
    public override void OnInitialize()
    {
        // Forest ??= Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/Forest_NineSlice");

        
        var parallelogram = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/PlayerPanel_Simple");
        ParallelogramElement player = new(parallelogram, -96f);
        player.Width.Pixels = 378f;
        player.Height.Pixels = 128f;
        player.Top.Pixels = 256f;
        player.OnDraw += OnDrawPlayerPanel;
        Append(player);
        

        DynamicPixelRatioElement p = new("Terramon/Assets/GUI/TurnBased/Simple")
        {
            BlockInput = true,
            HAlign = 0.5f,
            VAlign = 0.9f,
        };
        p.Width.Percent = 0.5f;
        p.Height.Percent = 0.3f;
        p.SetPadding(32f);

        p.Append(_optionsPanel);

        Append(p);

        _mainPanel = p;
    }

    private static void OnDrawPlayerPanel(SpriteBatch sb, ParallelogramElement element)
    {
        var activeMon = Main.LocalPlayer.Terramon().GetActivePokemon();
        if (activeMon is null)
            return;
        var hpBar = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/HPBar").Value;
        var expBar = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/EXPBar").Value;
        var gender = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/Gender").Value;
        var ballSlots = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/BallSlots_Simple").Value;

        var dims = element.GetDimensions();
        Vector2 pos = dims.Position();
        Vector2 textDrawPos = pos + new Vector2(32f, 16f);
        float textDrawScale = 0.7f;
        Vector2 genderDrawPos = textDrawPos + new Vector2(FontAssets.DeathText.Value.MeasureString(activeMon.DisplayName).X * textDrawScale + 8f, 0f);
        int drawFrame = (int)activeMon.Gender;
        if (drawFrame != 0)
            sb.Draw(gender, genderDrawPos, gender.Frame(2, 1, drawFrame - 1), Color.White);
        Vector2 hpDrawPos = textDrawPos + new Vector2(-(hpBar.Width / 3), 45f);
        float hpWidth = dims.Width - 48f;
        float hpFactor = activeMon.HP / (float)activeMon.MaxHP;
        float zeroToOne = (MathF.Sin((float)Main.timeForVisualEffects * 0.025f) + 1f) * 0.5f;
        DynamicPixelRatioElement.DrawAdjustableBar(sb, hpBar, hpDrawPos, hpWidth, Color.Black, Main.GameZoomTarget);
        DynamicPixelRatioElement.DrawAdjustableBar(sb, hpBar, hpDrawPos, hpWidth * hpFactor, Color.White, Main.GameZoomTarget);
        string hpDisplay = $"HP: {activeMon.HP} / {activeMon.MaxHP}";
        Vector2 hpDisplaySize = FontAssets.MouseText.Value.MeasureString(hpDisplay);
        float xDifference = 128f;
        Vector2 expDrawPos = hpDrawPos + new Vector2(xDifference, hpBar.Height);
        float expWidth = hpWidth - xDifference - (hpBar.Width / 3);
        float expFactor = activeMon.Level == Terramon.MaxPokemonLevel ? 1f : activeMon.TotalEXP / (float)ExperienceLookupTable.GetLevelTotalExp(activeMon.Level + 1, activeMon.Schema.GrowthRate);
        DynamicPixelRatioElement.DrawAdjustableBar(sb, expBar, expDrawPos, expWidth, Color.Black, Main.GameZoomTarget);
        DynamicPixelRatioElement.DrawAdjustableBar(sb, expBar, expDrawPos, expWidth * expFactor, Color.White, Main.GameZoomTarget);

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        ChatManager.DrawColorCodedStringWithShadow
            (sb, FontAssets.DeathText.Value, activeMon.DisplayName, textDrawPos, Color.White, 0f, Vector2.Zero, Vector2.One * textDrawScale, -1, 2);
        ChatManager.DrawColorCodedStringWithShadow
            (sb, FontAssets.MouseText.Value, hpDisplay, hpDrawPos + new Vector2(hpWidth * 0.5f, (hpBar.Height + 8) * 0.5f * Main.GameZoomTarget) - hpDisplaySize * 0.5f, Color.White, 0f, Vector2.Zero, Vector2.One);
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
    }

    private static Point16 _dimensions;
    private static bool _opened;
    public static void Open()
    {
        Instance.Recalculate();
        _dimensions = new(Main.screenWidth, Main.screenHeight);
        if (Main.playerInventory)
            Main.LocalPlayer.ToggleInv();
        _opened = true;
    }
    public static void Close()
    {
        _opened = false;
    }
    public static void ChangePanel(UIElement to)
    {
        _mainPanel.RemoveAllChildren();
        _mainPanel.Append(to);
        if (to == _movesPanel)
            UpdateMoves();
        else if (to == _pokemonPanel)
            UpdatePokemon();
        _mainPanel.RecalculateChildren();
    }
    public static void UpdateMoves(PokemonData data = null)
    {
        data ??= TerramonPlayer.LocalPlayer.GetActivePokemon();
        if (data is null)
            return;
        int cur = 0;
        foreach (var moveButton in _movesPanel.Children)
        {
            var actual = (DynamicPixelRatioElement)moveButton;
            var label = (UIText)actual.Children.First(e => e is UIText);
            var moveRef = (MoveReference)actual.Children.First(e => e is MoveReference);

            var move = data.Moves[cur];
            actual.Color = Main.rand.NextFromCollection(Enum.GetValues<PokemonType>().ToList()).GetColor();
            actual.UpdateTextures($"Terramon/Assets/GUI/TurnBased/MoveButton_Normal");
            label.SetText(move.ID.ToString());
            moveRef.DataRef = data;
            moveRef.Move = cur;

            cur++;
        }
    }
    private static void ClickMoveButton(UIMouseEvent evt, UIElement listeningElement)
    {
        Console.WriteLine("Testing1");
        var move = (MoveReference)listeningElement.Children.First(e => e is MoveReference);
        var battle = TerramonPlayer.LocalPlayer.Battle;
        if (battle.MakeMove(move.Move + 1))
        {
            ChangePanel(_optionsPanel);
            battle.MakeMove(2, -1);
        }
    }
    public static void UpdatePokemon()
    {
        var team = TerramonPlayer.LocalPlayer.Party;
        for (int i = 0; i < team.Length; i++)
        {
            var pokemonButton = _pokemonPanel.Children.ElementAt(i);
            var pokemon = team[i];
            if (pokemon is null)
                continue;
            UIText label = (UIText)pokemonButton.Children.First(e => e is UIText);
            UIImage image = (UIImage)pokemonButton.Children.First(e => e is UIImage);
            PokemonReference poke = (PokemonReference)pokemonButton.Children.First(e => e is PokemonReference);
            label.SetText(DatabaseV2.GetLocalizedPokemonName(pokemon.Schema));
            image.SetImage(pokemon.GetMiniSprite());
            poke.DataRef = pokemon;
            poke.PartySlot = i;
        }
    }
    private static void ClickPokemonButton(UIMouseEvent evt, UIElement listeningElement)
    {
        Console.WriteLine("Testing2");
        var pokeRef = ((PokemonReference)listeningElement.Children.First(e => e is PokemonReference));
        if (pokeRef is null)
            return;
        var data = pokeRef.DataRef;
        string send = string.IsNullOrEmpty(data.Nickname) ? data.Schema.Identifier : data.Nickname;
        var terramon = TerramonPlayer.LocalPlayer;
        var battle = terramon.Battle;
        if (battle.MakeSwitch(send))
        {
            ChangePanel(_optionsPanel);
            battle.MakeMove(2, -1);
            terramon.ActiveSlot = pokeRef.PartySlot;
        }
    }

    private static void FightButton(UIMouseEvent evt, UIElement listeningElement)
    {
        // for quick asset testing
        /*
        string path = Terramon.Instance.SourceFolder + """\Assets\GUI\TurnBased\Forest_NineSlice.png""";
        // Main.NewText(Path.Exists(path));
        using Stream stream = File.OpenRead(path);
        Forest = Terramon.Instance.Assets.CreateUntracked<Texture2D>(stream, path);
        */
        if (TerramonPlayer.LocalPlayer.Battle?.HasToSwitch ?? false)
            return;
        ChangePanel(_movesPanel);
    }
    private static void BagButton(UIMouseEvent evt, UIElement listeningElement)
    {

    }
    private static void PokemonButton(UIMouseEvent evt, UIElement listeningElement)
    {
        ChangePanel(_pokemonPanel);
    }
    private static void RunButton(UIMouseEvent evt, UIElement listeningElement)
    {
        var plr = TerramonPlayer.LocalPlayer;
        var battle = plr.Battle;
        if (battle != null)
        {
            battle.BattleStream?.Dispose();
            battle.WildNPC?.EndBattle();
            plr.Battle = null;
        }
        PartyDisplay.Sidebar.Open();
        Close();
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Point16 newDim = new(Main.screenWidth, Main.screenHeight);
        if (newDim != _dimensions)
        {
            Recalculate();
            _dimensions = newDim;
        }
        /*
        RemoveAllChildren();
        OnInitialize();
        Recalculate();
        */
    }
}
public sealed class ParallelogramElement : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private readonly float _xOffset;
    public event Action<SpriteBatch, ParallelogramElement> OnDraw;
    public ParallelogramElement(Asset<Texture2D> texture, float xOffset = 0f)
    {
        _texture = texture;
        _xOffset = xOffset;
        OverrideSamplerState = SamplerState.PointClamp;
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_texture is null)
            return;
        float zoom = Main.GameZoomTarget;
        var bounds = GetDimensions();
        Texture2D tex = _texture.Value!;
        float textureXOffset = (tex.Width - (tex.Width / zoom)) * zoom;
        Vector2 pos = new(bounds.X - (textureXOffset - _xOffset), bounds.Y);
        float yOffset = (tex.Height / zoom) - bounds.Height;
        DrawAdjustableParallelogram(spriteBatch, tex, pos, Color.White, yOffset, zoom);
        OnDraw?.Invoke(spriteBatch, this);
        // spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds.ToRectangle(), Color.White * 0.5f);
    }
    public static void DrawAdjustableParallelogram(SpriteBatch spriteBatch, Texture2D tex, Vector2 position, Color col, float bottomOffset, float scale)
    {
        Rectangle frame = tex.Frame(1, 2);
        bottomOffset *= scale;
        spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        position += new Vector2(bottomOffset * -0.5f, (frame.Height * scale) + bottomOffset);
        frame.Y += frame.Height;
        spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
public sealed class MoveReference : UIElement
{
    public PokemonData DataRef;
    public int Move;
}
public sealed class PokemonReference : UIElement
{
    public PokemonData DataRef;
    public int PartySlot;
}
public sealed class DynamicPixelRatioElement : UIElement
{
    private Asset<Texture2D> _nineSlice;
    private Asset<Texture2D> _corners;
    private bool _noStretching;
    private bool _buttonLike;
    private bool _hovered;
    private float _hoverTime;
    public bool BlockInput;
    public Color Color { get; set; } = Color.White;
    public DynamicPixelRatioElement(string basePath, bool noStretching = false, bool buttonLike = false)
    {
        UpdateTextures(basePath, noStretching);
        SetupCommon(buttonLike);
    }

    private static void Unhovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        dyn._hovered = false;
    }

    private static void Hovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        dyn._hovered = true;
    }

    public DynamicPixelRatioElement(Asset<Texture2D> nineSlice, Asset<Texture2D> corners, bool noStretching = false, bool buttonLike = false)
    {
        UpdateTextures(nineSlice, corners, noStretching);
        SetupCommon(buttonLike);
    }
    private void SetupCommon(bool buttonLike)
    {
        OverrideSamplerState = SamplerState.PointClamp;
        if (!buttonLike)
            return;
        _buttonLike = true;
        OnMouseOver += Hovered;
        OnMouseOut += Unhovered;
    }
    public DynamicPixelRatioElement UpdateTextures(string newBasePath, bool noStretching = false)
    {
        if (ModContent.RequestIfExists(newBasePath + "_NineSlice", out _nineSlice))
            _noStretching = noStretching;
        else if (ModContent.RequestIfExists(newBasePath + "_NineSlice_Stretch", out _nineSlice))
            _noStretching = false;
        ModContent.RequestIfExists(newBasePath + "_Corners", out _corners);
        return this;
    }
    public DynamicPixelRatioElement UpdateTextures(Asset<Texture2D> nineSlice, Asset<Texture2D> corners, bool noStretching = false)
    {
        _nineSlice = nineSlice;
        _corners = corners;
        _noStretching = noStretching;
        return this;
    }
    public override void Update(GameTime gameTime)
    {
        if (_hovered && _hoverTime < 1f)
            _hoverTime = Math.Min(_hoverTime + 0.1f, 1f);
        else if (!_hovered && _hoverTime > 0f)
            _hoverTime = Math.Max(_hoverTime - 0.1f, 0f);
        base.Update(gameTime);
    }
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (BlockInput && ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        Rectangle bounds = GetDimensions().ToRectangle();
        Color drawColor = Color;
        if (_buttonLike)
        {
            int infl = (int)(_hoverTime * 2f);
            bounds.Inflate(infl, infl);
            drawColor.A = (byte)((1f - _hoverTime) * byte.MaxValue);
        }
        if (_nineSlice != null)
            DrawAdjustableBox(spriteBatch, _nineSlice.Value, bounds, drawColor, Main.GameZoomTarget, _noStretching);
        if (_corners != null)
            DrawAdjustableCorners(spriteBatch, _corners.Value, bounds, drawColor, Main.GameZoomTarget);
    }
    public static void DrawAdjustableCorners(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col, float scale)
    {
        Rectangle frame = tex.Frame(2, 1);
        spriteBatch.Draw(tex, rect.TopLeft(), frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        Vector2 og = new(tex.Width * 0.5f, tex.Height);
        spriteBatch.Draw(tex, rect.BottomRight(), frame, col, 0f, og, scale, SpriteEffects.None, 0f);
    }
    public static void DrawAdjustableBar(SpriteBatch spriteBatch, Texture2D tex, Vector2 pos, float width, Color col, float pxScale)
    {
        if (width <= 0f)
            return;
        Vector2 drawPos = pos;
        float quadWidth = tex.Width / 3f * pxScale;
        var xScale = (width - quadWidth * 2f) / quadWidth;
        Rectangle frame = tex.Frame(3, 1);
        int oldWidth = frame.Width;
        if (width < quadWidth)
            frame.Width = (int)(frame.Width * (width / quadWidth));
        spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, pxScale, SpriteEffects.None, 0f);
        frame.Width = oldWidth;
        frame.X += frame.Width;
        drawPos.X += quadWidth;
        if (xScale > 0f)
        {
            spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, new Vector2(xScale * pxScale, pxScale), SpriteEffects.None, 0f);
            var pxs = quadWidth * xScale;
            drawPos.X += pxs;
        }
        else
            drawPos.X = pos.X + quadWidth;
        if (width <= quadWidth)
            return;
        frame.X += frame.Width;
        if (width < quadWidth * 2f)
            frame.Width = (int)(frame.Width * ((width - quadWidth) / quadWidth));
        spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, pxScale, SpriteEffects.None, 0f);
    }
    public static void DrawAdjustableBox(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col, float pxScale, bool noStretching = false, SkipDrawBoxSegment flags = SkipDrawBoxSegment.None)
    {
        Vector2 quadSize = new Vector2(tex.Width / 3f, tex.Height / 3f) * pxScale;
        var xScale = (rect.Width - quadSize.X * 2f) / quadSize.X;
        var yScale = (rect.Height - quadSize.Y * 2f) / quadSize.Y;

        void DrawSegment(in Vector2 position, in Rectangle frame, float xSize = 1f, float ySize = 1f)
        {
            spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, new Vector2(xSize * pxScale, ySize * pxScale), SpriteEffects.None, 0f);
        }
        void DrawWrapped(Vector2 position, Rectangle frame, float scale, bool down)
        {
            for (float currentScale = 0f; currentScale < scale; currentScale++)
            {
                if (currentScale != 0f)
                {
                    if (down)
                        position.Y += quadSize.Y;
                    else
                        position.X += quadSize.X;
                }
                float overshoot = currentScale + 1f;
                if (overshoot > scale)
                {
                    if (down)
                        frame.Height = (int)MathF.Ceiling(frame.Height * (scale - currentScale));
                    else
                        frame.Width = (int)MathF.Ceiling(frame.Width * (scale - currentScale));
                }
                DrawSegment(in position, in frame);
            }
        }
        // Draw center
        if ((flags & SkipDrawBoxSegment.CenterCenter) == 0)
        {
            Rectangle centerFrame = tex.Frame(3, 3, 1, 1);
            DrawSegment(new Vector2(rect.X + quadSize.X, rect.Y + quadSize.Y), centerFrame, xScale, yScale);
        }

        // Draw sides
        if ((flags & SkipDrawBoxSegment.TopCenter) == 0)
        {
            Rectangle topSideFrame = tex.Frame(3, 3, 1, 0);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y);
            if (noStretching)
                DrawWrapped(pos, topSideFrame, xScale, false);
            else
                DrawSegment(pos, topSideFrame, xScale, 1f);
        }

        if ((flags & SkipDrawBoxSegment.CenterLeft) == 0)
        {
            Rectangle leftSideFrame = tex.Frame(3, 3, 0, 1);
            Vector2 pos = new(rect.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, leftSideFrame, yScale, true);
            else
                DrawSegment(pos, leftSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.CenterRight) == 0)
        {
            Rectangle rightSideFrame = tex.Frame(3, 3, 2, 1);
            Vector2 pos = new(rect.X + rect.Width - quadSize.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, rightSideFrame, yScale, true);
            else
                DrawSegment(pos, rightSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.BottomCenter) == 0)
        {
            Rectangle bottomSideFrame = tex.Frame(3, 3, 1, 2);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y + rect.Height - quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, bottomSideFrame, xScale, false);
            else
                DrawSegment(pos, bottomSideFrame, xScale, 1f);
        }

        // Draw corners

        if ((flags & SkipDrawBoxSegment.TopLeft) == 0)
        {
            Rectangle topLeftCorner = tex.Frame(3, 3, 0, 0);
            DrawSegment(new Vector2(rect.X, rect.Y), topLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.TopRight) == 0)
        {
            Rectangle topRightCorner = tex.Frame(3, 3, 2, 0);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y), topRightCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomLeft) == 0)
        {
            Rectangle bottomLeftCorner = tex.Frame(3, 3, 0, 2);
            DrawSegment(new Vector2(rect.X, rect.Y + rect.Height - quadSize.Y), bottomLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomRight) == 0)
        {
            Rectangle bottomRightCorner = tex.Frame(3, 3, 2, 2);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y + rect.Height - quadSize.Y), bottomRightCorner);
        }
    }
}
internal enum ButtonType
{
    Fight,
    Bag,
    Pokemon,
    Run,
}
[Flags]
public enum SkipDrawBoxSegment
{
    None = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 4,
    CenterLeft = 8,
    CenterCenter = 16,
    CenterRight = 32,
    BottomLeft = 64,
    BottomCenter = 128,
    BottomRight = 256,
}
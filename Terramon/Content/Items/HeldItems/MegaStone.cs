using Terramon.Content.Rarities;
using Terramon.Core.Loaders;
using Terramon.Helpers;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.Items.HeldItems;

[Autoload(false)]
public sealed class MegaStone(MegaStoneID id) : HeldItem
{
    public readonly record struct MegaStonePalette(Color Main, Color Streak, Color StreakA, Color StreakB);
    public static MegaStonePalette[] Palettes { get; private set; }

    private static readonly Vector3[] _colorsBuf = new Vector3[8];

    private static RenderTarget2D _rt;

    public MegaStoneID ID = id;

    private LocalizedText _pokeName;

    private LocalizedText _formattedTip;

    protected override bool CloneNewInstances => true;
    public override string Name => $"{ID}MegaStone";
    protected override int UseRarity => ModContent.RarityType<MegaRarity>();

    public override LocalizedText DisplayName => Mod.GetLocalization($"MegaStoneNames.{ID}", ID.ToString);
    public override LocalizedText Tooltip => _formattedTip;

    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
            _rt = new(Main.graphics.GraphicsDevice, 22, 22));
    }

    public override void Unload() => _rt.Dispose();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        _pokeName =
            Terramon.DatabaseV2.GetLocalizedPokemonName(PokemonEntityLoader.MegaStoneToID[(ushort)Type]);
        _formattedTip =
            Mod.GetLocalization("CommonTooltips.MegaStoneTip").WithFormatArgs(_pokeName);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(
            new TooltipLine(Mod, "MegaStone", Mod.GetLocalization("CommonTooltips.MegaStone").Format(_pokeName.Value)) 
            { 
                OverrideColor = Color.Aquamarine 
            });
        base.ModifyTooltips(tooltips);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = Item.height = 22;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        PrepareRenderTarget(spriteBatch);

        spriteBatch.Draw(_rt, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        PrepareRenderTarget(spriteBatch);

        var drawOrigin = Item.Size * 0.5f;
        var drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, drawOrigin.Y);

        spriteBatch.Draw(_rt, drawPosition, null, lightColor, rotation, drawOrigin, scale, SpriteEffects.None, 0f);

        return false;
    }

    private void PrepareRenderTarget(SpriteBatch sb)
    {
        var shader = ShaderAssets.Palette.Value;
        var tex = TextureAssets.Item[Type].Value;

        ref var palette = ref Palettes[(int)ID];

        var main = palette.Main;

        _colorsBuf[1] = main.HueShift(0.1f, -0.5f).ToVector3();
        _colorsBuf[2] = main.HueShift(0.05f, -0.1f).ToVector3();
        _colorsBuf[3] = main.ToVector3();
        _colorsBuf[4] = palette.Streak.ToVector3();
        _colorsBuf[5] = palette.StreakA.ToVector3();
        _colorsBuf[6] = palette.StreakB.ToVector3();

        shader.Parameters["uColors"].SetValue(_colorsBuf);

        var gd = Main.graphics.GraphicsDevice;

        // This is necessary to avoid funky RenderTarget stuff with other mods that might set this back to DiscardContents
        gd.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        sb.End(out var oldData);

        var oldTargets = gd.GetRenderTargets();
        gd.SetRenderTarget(_rt);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader);
        sb.Draw(tex, Vector2.Zero, Color.White);
        sb.End();

        for (int i = 0; i < oldTargets.Length; i++)
        {
            if (oldTargets[i].RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        gd.SetRenderTargets(oldTargets);

        sb.Begin(in oldData);
    }

    internal static void LoadPalettes(Mod mod)
    {
        _colorsBuf[0] = Color.Black.ToVector3();
        _colorsBuf[^1] = Color.White.ToVector3();

        using var stream = mod.GetFileStream("Assets/Items/HeldItems/MegaStonePalettes.plt");
        int max = (int)MegaStoneID.Baxcalibur + 1;
        Palettes = new MegaStonePalette[max];
        Span<byte> buffer = stackalloc byte[3];
        for (int i = 0; i < max; i++)
        {
            // Main
            stream.ReadExactly(buffer);
            var main = new Color(buffer[0], buffer[1], buffer[2]);
            // Streak
            stream.ReadExactly(buffer);
            var streak = new Color(buffer[0], buffer[1], buffer[2]);
            // StreakA
            stream.ReadExactly(buffer);
            var streakA = new Color(buffer[0], buffer[1], buffer[2]);
            // StreakB
            stream.ReadExactly(buffer);
            var streakB = new Color(buffer[0], buffer[1], buffer[2]);

            Palettes[i] = new(main, streak, streakA, streakB);
        }
    }
}

public sealed class MegaRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new(233, 234, 96),
        new(30, 183, 223),
        new(223, 139, 190)
    ];

    protected override float Time => base.Time;
}

public enum MegaStoneID
{
    Missingno,
    Gengar,
    Gardevoir,
    Ampharos,
    Venusaur,
    CharizardX,
    Blastoise,
    MewtwoX,
    MewtwoY,
    Blaziken,
    Medicham,
    Houndoom,
    Aggron,
    Banette,
    Tyranitar,
    Scizor,
    Pinsir,
    Aerodactyl,
    Lucario,
    Abomasnow,
    Kangaskhan,
    Gyarados,
    Absol,
    CharizardY,
    Alakazam,
    Heracross,
    Mawile,
    Manectric,
    Garchomp,
    Latias,
    Latios,
    Swampert,
    Sceptile,
    Sableye,
    Altaria,
    Gallade,
    Audino,
    Metagross,
    Sharpedo,
    Slowbro,
    Steelix,
    Pidgeot,
    Glalie,
    Diancie,
    Camerupt,
    Lopunny,
    Salamence,
    Beedrill,
    Clefable,
    Victreebel,
    Starmie,
    Dragonite,
    Meganium,
    Feraligatr,
    Skarmory,
    Froslass,
    Emboar,
    Excadrill,
    Scolipede,
    Scrafty,
    Eelektross,
    Chandelure,
    Chesnaught,
    Delphox,
    Greninja,
    Pyroar,
    Floette,
    Malamar,
    Barbaracle,
    Dragalge,
    Hawlucha,
    Zygarde,
    Drampa,
    Falinks,
    RaichuX,
    RaichuY,
    Baxcalibur,
}


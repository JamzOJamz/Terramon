using Terramon.Content.Rarities;
using Terramon.Core.Loaders;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.Items;

[Autoload(false)]
public sealed class MegaStone(MegaStoneID id, ushort evolves) : HeldItem
{
    private static readonly Vector3[] ColorsBuf = new Vector3[8];
    private static readonly TerramonItemRegistry.GroupBuilder MegaStoneItemGroup =
        TerramonItemRegistry.Group(TerramonItemGroup.MegaStones);
    private static RenderTarget2D _rt;
    
    private static Palette[] Palettes { get; set; }
    
    private LocalizedText _pokeName;
    
    protected override bool CloneNewInstances => true;
    public override string Name => $"{id}MegaStone";
    protected override int UseRarity => ModContent.RarityType<MegaRarity>();
    public override string Texture => "Terramon/Assets/Items/HeldItems/MegaStone";
    public override LocalizedText DisplayName => Mod.GetLocalization($"MegaStoneNames.{id}", id.ToString);
    public override LocalizedText Tooltip =>
        Mod.GetLocalization("CommonTooltips.MegaStoneTip").WithFormatArgs(_pokeName);

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
        _pokeName = Terramon.DatabaseV2.GetLocalizedPokemonName(evolves);
    }
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = Item.height = 22;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(
            new TooltipLine(Mod, "MegaStone", Mod.GetLocalization("CommonTooltips.MegaStone").Format(_pokeName.Value))
            {
                OverrideColor = Color.Aquamarine
            });
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor, Vector2 origin, float scale)
    {
        PrepareRenderTarget(spriteBatch);

        spriteBatch.Draw(_rt, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation,
        ref float scale, int whoAmI)
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

        ref var palette = ref Palettes[(int)id];

        var main = palette.Main;

        ColorsBuf[1] = main.HueShift(0.1f, -0.5f).ToVector3();
        ColorsBuf[2] = main.HueShift(0.05f, -0.1f).ToVector3();
        ColorsBuf[3] = main.ToVector3();
        ColorsBuf[4] = palette.Streak.ToVector3();
        ColorsBuf[5] = palette.StreakA.ToVector3();
        ColorsBuf[6] = palette.StreakB.ToVector3();

        shader.Parameters["uColors"].SetValue(ColorsBuf);

        var gd = Main.graphics.GraphicsDevice;

        // This is necessary to avoid funky RenderTarget stuff with other mods that might set this back to DiscardContents
        gd.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        sb.End(out var oldData);

        var oldTargets = gd.GetRenderTargets();
        gd.SetRenderTarget(_rt);
        gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullNone, shader);
        sb.Draw(tex, Vector2.Zero, Color.White);
        sb.End();

        foreach (var t in oldTargets)
        {
            if (t.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        gd.SetRenderTargets(oldTargets);

        sb.Begin(in oldData);
    }

    private static void LoadPalettes(Mod mod)
    {
        ColorsBuf[0] = Color.Black.ToVector3();
        ColorsBuf[^1] = Color.White.ToVector3();

        using var stream = mod.GetFileStream("Assets/Items/HeldItems/MegaStonePalettes.plt");
        const int max = (int)MegaStoneID.Baxcalibur + 1;
        Palettes = new Palette[max];
        Span<byte> buffer = stackalloc byte[3];
        for (var i = 0; i < max; i++)
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

            Palettes[i] = new Palette(main, streak, streakA, streakB);
        }
    }

    internal static void LoadMegaStones()
    {
        // Load palettes from file
        LoadPalettes(Terramon.Instance);

        // Load RT for drawing mega stones
        Main.QueueMainThreadAction(() =>
            _rt = new RenderTarget2D(Main.graphics.GraphicsDevice, 22, 22));

        // Load mega stones
        for (var start = MegaStoneID.Gengar; start <= MegaStoneID.Baxcalibur; start++)
        {
            var startName = start.ToString().TrimEnd('X', 'Y');
            if (NationalDexID.Search.TryGetId(startName, out var id))
                MegaStoneItemGroup.Add(new MegaStone(start, (ushort)id));
        }
    }
    
    public override void Unload() => Main.QueueMainThreadAction(_rt.Dispose);

    private readonly record struct Palette(Color Main, Color Streak, Color StreakA, Color StreakB);
}

public sealed class MegaRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new(233, 234, 96),
        new(30, 183, 223),
        new(223, 139, 190)
    ];
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
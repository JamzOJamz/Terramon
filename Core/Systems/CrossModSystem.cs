namespace Terramon.Core.Systems;

public sealed class CrossModSystem : ModSystem
{
    public static Mod Wikithis { get; private set; }

    public override void OnModLoad()
    {
        if (Main.dedServ)
            return;

        // Wikithis compatibility
        if (!ModLoader.TryGetMod("Wikithis", out var wikithis)) return;
        Wikithis = wikithis;
        wikithis.Call(0, Mod, "https://terrariamods.wiki.gg/wiki/Terramon_Mod/{}");
        wikithis.Call(3, Mod, ModContent.Request<Texture2D>("Terramon/icon_small"));
    }
}
namespace Terramon.Core.Systems;

public sealed class CrossModSystem : ModSystem
{
    public static Mod Wikithis { get; private set; }

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        // Wikithis compatibility
        if (!ModLoader.TryGetMod("Wikithis", out var wikithis)) return;
        Wikithis = wikithis;
        wikithis.Call(0, this, "https://terrariamods.wiki.gg/wiki/Terramon_Mod/{}");
        wikithis.Call(3, this, ModContent.Request<Texture2D>("Terramon/icon_small"));
    }
}
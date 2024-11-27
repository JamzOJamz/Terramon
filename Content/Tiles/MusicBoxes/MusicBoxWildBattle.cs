namespace Terramon.Content.Tiles.MusicBoxes;

public class MusicBoxWildBattle : MusicTile
{
    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<MusicItemWildBattle>();
    }
}

public class MusicItemWildBattle : MusicItem
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BattleWild"),
            ModContent.ItemType<MusicItemWildBattle>(), ModContent.TileType<MusicBoxWildBattle>());
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.createTile = ModContent.TileType<MusicBoxWildBattle>();
    }
}
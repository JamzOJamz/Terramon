using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.MusicBoxes;

public abstract class MusicTile : ModTile
{
    public override string Texture => "Terramon/Assets/Tiles/MusicBoxes/" + GetType().Name;
    
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        TileObjectData.addTile(Type);
        DustType = DustID.Titanium;

        var name = CreateMapEntryName();
        AddMapEntry(new Color(200, 200, 200), name);
    }
    
    public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
    {
        if (Main.tile[i, j].TileFrameX != 36 || (int)Main.timeForVisualEffects % 7 != 0 ||
            !Main._rand.NextBool(5)) return;
        var MusicNote = Main._rand.Next(570, 573);
        Vector2 SpawnPosition = new(i * 16 + 8, j * 16 - 8);
        Vector2 NoteMovement = new(Main.WindForVisuals * 2f, -0.5f);
        NoteMovement.X *= 1f + Main._rand.Next(-50, 51) * 0.01f;
        NoteMovement.Y *= 1f + Main._rand.Next(-50, 51) * 0.01f;
        switch (MusicNote)
        {
            case 572:
                SpawnPosition.X -= 8f;
                break;
            case 571:
                SpawnPosition.X -= 4f;
                break;
        }

        Gore.NewGore(new EntitySource_Misc(""), SpawnPosition, NoteMovement, MusicNote, 0.8f);
    }
}
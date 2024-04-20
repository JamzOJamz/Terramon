using Terraria.ID;

namespace Terramon.Content.Buffs;

public class PokemonCompanion : ModBuff
{
    public override string Texture => "Terramon/Assets/Buffs/" + GetType().Name;

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        rare = ItemRarityID.Expert;
    }
}
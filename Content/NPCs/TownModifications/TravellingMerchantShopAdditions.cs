using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.NPCs.TownModifications;

public class TravellingMerchantShopAdditions : GlobalNPC
{
    public override void SetupTravelShop(int[] shop, ref int nextSlot)
    {
        if (!Main.rand.NextBool(9)) return;
        shop[nextSlot] = ModContent.ItemType<CherishBallItem>();
        nextSlot++;
    }
}
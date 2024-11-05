using Terramon.Content.Items;

namespace Terramon.Content.NPCs.TownModifications;

public class MechanicShopAdditions : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == NPCID.Mechanic;
    }

    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType == NPCID.Mechanic)
            shop.Add<LinkingCord>();
    }
}
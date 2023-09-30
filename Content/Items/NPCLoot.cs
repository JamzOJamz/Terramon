using Terramon.Content.Items.Vitamins;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace Terramon.Content.Items;

internal class NPCLoot : GlobalNPC
{
    /* TODO: Add this back once link cable item exists
    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType == NPCID.Mechanic)
            shop.Add<LinkCable>();
    }
    */

    public override void ModifyNPCLoot(NPC npc, Terraria.ModLoader.NPCLoot npcLoot)
    {
        if (!npc.boss)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RareCandy>(), 32));
        }
        else
        {
            var amount = 0;
            switch (npc.type)
            {
                case NPCID.KingSlime:
                case NPCID.Deerclops:
                case NPCID.QueenBee:
                case NPCID.Spazmatism:
                case NPCID.Retinazer:
                    amount = 3;
                    break;

                case NPCID.EyeofCthulhu:
                case NPCID.SkeletronHead:
                case NPCID.BrainofCthulhu:
                    amount = 5;
                    break;

                case NPCID.WallofFlesh:
                case NPCID.QueenSlimeBoss:
                case NPCID.SkeletronPrime:
                case NPCID.TheDestroyer:
                    amount = 7;
                    break;

                case NPCID.Plantera:
                case NPCID.Golem:
                case NPCID.DukeFishron:
                    amount = 9;
                    break;

                case NPCID.HallowBoss:
                case NPCID.CultistBoss:
                case NPCID.MoonLordHead:
                    amount = 11;
                    break;
            }

            if (Main.expertMode)
                amount = (int)(amount * 1.5);
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RareCandy>(), minimumDropped: amount,
                maximumDropped: amount));
        }
    }
}
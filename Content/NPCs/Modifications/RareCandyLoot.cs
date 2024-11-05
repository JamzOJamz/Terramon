using Terramon.Content.Items;
using Terraria.GameContent.ItemDropRules;

namespace Terramon.Content.NPCs.Modifications;

internal class RareCandyLoot : GlobalNPC
{
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (npc.friendly) return;
        if (!npc.boss)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RareCandy>(), 32));
            return;
        }

        var amount = npc.type switch
        {
            NPCID.KingSlime or NPCID.Deerclops or NPCID.QueenBee or NPCID.Spazmatism or NPCID.Retinazer => 3,
            NPCID.EyeofCthulhu or NPCID.SkeletronHead or NPCID.BrainofCthulhu => 5,
            NPCID.WallofFlesh or NPCID.QueenSlimeBoss or NPCID.SkeletronPrime or NPCID.TheDestroyer => 7,
            NPCID.Plantera or NPCID.Golem or NPCID.DukeFishron => 9,
            NPCID.HallowBoss or NPCID.CultistBoss or NPCID.MoonLordHead => 11,
            _ => 0
        };

        if (Main.expertMode)
            amount = (int)(amount * 1.5);
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RareCandy>(), minimumDropped: amount,
            maximumDropped: amount));
    }
}
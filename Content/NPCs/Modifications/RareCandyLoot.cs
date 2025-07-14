using Terramon.Content.Items;
using Terramon.Content.Items.Vitamins;
using Terraria.GameContent.ItemDropRules;

namespace Terramon.Content.NPCs.Modifications;

internal class RareCandyLoot : GlobalNPC
{
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (!npc.boss) return;

        var amount = npc.type switch
        {
            NPCID.KingSlime or NPCID.Deerclops or NPCID.QueenBee or NPCID.Spazmatism or NPCID.Retinazer => 3,
            NPCID.EyeofCthulhu or NPCID.SkeletronHead or NPCID.BrainofCthulhu => 5,
            NPCID.WallofFlesh or NPCID.QueenSlimeBoss or NPCID.SkeletronPrime or NPCID.TheDestroyer => 7,
            NPCID.Plantera or NPCID.Golem or NPCID.DukeFishron => 9,
            NPCID.HallowBoss or NPCID.CultistBoss or NPCID.MoonLordHead => 11,
            _ => 0
        };

        if (amount == 0) return;
        if (Main.expertMode)
            amount = (int)(amount * 1.5);
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RareCandy>(), minimumDropped: amount,
            maximumDropped: amount));
    }

    public override void ModifyGlobalLoot(GlobalLoot globalLoot)
    {
        globalLoot.Add(ItemDropRule.ByCondition(new RareCandyCommonDropCondition(), ModContent.ItemType<RareCandy>(),
            32));
    }
}

internal class RareCandyCommonDropCondition : IItemDropRuleCondition
{
    public bool CanDrop(DropAttemptInfo info)
    {
        return !info.npc.friendly && !info.npc.boss;
    }

    public bool CanShowItemDropInUI()
    {
        return false;
    }

    public string GetConditionDescription()
    {
        return string.Empty;
    }
}
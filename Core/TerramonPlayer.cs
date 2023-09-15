using Terramon.Content.GUI;
using Terramon.Content.Items.Mechanical;
using Terramon.Core.Loaders.UILoading;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer
{
    public readonly PokemonData[] Party = new PokemonData[6];
    public bool HasChosenStarter;
    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<TerramonPlayer>();
    public int premierBonusCount = 0;

    public override void OnEnterWorld()
    {
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(Party);
    }

    public override void PreUpdate()
    {
        if (premierBonusCount > 0 && Main.npcShop == 0)
        {
            int premierBonus = premierBonusCount / 10;
            if (premierBonus > 0)
            {
                if (premierBonus == 1)
                    Main.NewText(Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonus"));
                else
                    Main.NewText(Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonusPlural", premierBonus));

                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(), premierBonus);
            }
            premierBonusCount = 0;
        }
    }

    public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item)
    {
        //use CanSellItem rather than PostSellItem because PostSellItem doesn't return item data correctly
        if (item.type == ModContent.ItemType<PokeBallItem>())
            premierBonusCount -= item.stack;

        if (item.type == ModContent.ItemType<MasterBallItem>())
            return false;
        else
            return base.CanSellItem(vendor,shopInventory, item);
    }

    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        if (item.type == ModContent.ItemType<PokeBallItem>())
            premierBonusCount++;
    }

    /// <summary>
    ///     Adds a Pokémon to the player's party. Returns false if their party is full; otherwise returns true.
    /// </summary>
    public bool AddPartyPokemon(PokemonData data)
    {
        var nextIndex = NextFreePartyIndex();
        if (nextIndex == 6) return false;
        Party[nextIndex] = data;
        UILoader.GetUIState<PartyDisplay>().UpdateSlot(data, nextIndex);
        return true;
    }

    public void SwapParty(int first, int second)
    {
        (Party[first], Party[second]) = (Party[second], Party[first]);
    }

    public int NextFreePartyIndex()
    {
        for (var i = 0; i < Party.Length; i++)
            if (Party[i] == null)
                return i;

        return 6;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["starterChosen"] = HasChosenStarter;
        SaveParty(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        HasChosenStarter = tag.GetBool("starterChosen");
        LoadParty(tag);
    }

    private void SaveParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            if (Party[i] == null) continue;
            tag[$"p{i}"] = Party[i];
        }
    }

    private void LoadParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            var tagName = $"p{i}";
            if (tag.ContainsKey(tagName)) Party[i] = tag.Get<PokemonData>(tagName);
        }
    }
}
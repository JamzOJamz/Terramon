using System.Linq;
using Terramon.Content.Buffs;
using Terramon.Content.GUI;
using Terramon.Content.Items.Mechanical;
using Terramon.Core.Loaders.UILoading;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer
{
    public readonly PokemonData[] Party = new PokemonData[6];
    private readonly PokedexService Pokedex = new();
    public bool HasChosenStarter;

    private bool lastPlayerInventory;
    public int premierBonusCount;
    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<TerramonPlayer>();

    public PokedexService GetPokedex()
    {
        return Pokedex;
    }

    public override void OnEnterWorld()
    {
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(Party);
        // TODO: Apply this only when a Pokémon pet projectile is spawned the party
        Player.AddBuff(ModContent.BuffType<PokemonCompanion>(), 60 * 60);
    }

    public override void PreUpdate()
    {
        if (Main.playerInventory && !lastPlayerInventory)
            UILoader.GetUIState<PartyDisplay>().Sidebar.ForceKillAnimation();

        lastPlayerInventory = Main.playerInventory;

        if (premierBonusCount <= 0 || Main.npcShop != 0) return;
        var premierBonus = premierBonusCount / 10;
        if (premierBonus > 0)
        {
            Main.NewText(premierBonus == 1
                ? Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonus")
                : Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonusPlural", premierBonus));

            Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(),
                premierBonus);
        }

        premierBonusCount = 0;
    }

    public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item)
    {
        //use CanSellItem rather than PostSellItem because PostSellItem doesn't return item data correctly
        if (item.type == ModContent.ItemType<PokeBallItem>())
            premierBonusCount -= item.stack;

        return item.type != ModContent.ItemType<MasterBallItem>() && base.CanSellItem(vendor, shopInventory, item);
    }

    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        if (item.type == ModContent.ItemType<PokeBallItem>())
            premierBonusCount++;
    }

    /// <summary>
    ///     Adds a Pokémon to the player's party. Returns false if their party is full; otherwise returns true.
    /// </summary>
    public bool AddPartyPokemon(PokemonData data, bool addToPokedex = true)
    {
        if (addToPokedex)
            UpdatePokedex(data.ID, PokedexEntryStatus.Registered);

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

    public bool UpdatePokedex(ushort id, byte status)
    {
        var containsId = Pokedex.Entries.ContainsKey(id);
        if (containsId) Pokedex.Entries[id] = status;
        return containsId;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["starterChosen"] = HasChosenStarter;
        SaveParty(tag);
        SavePokedex(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        HasChosenStarter = tag.GetBool("starterChosen");
        LoadParty(tag);
        LoadPokedex(tag);
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

    private void SavePokedex(TagCompound tag)
    {
        tag["pokedex"] = Pokedex.Entries.Select(entry => new int[] { entry.Key, entry.Value }).ToList();
    }

    private void LoadPokedex(TagCompound tag)
    {
        const string tagName = "pokedex";
        if (!tag.ContainsKey(tagName)) return;
        var entries = tag.GetList<int[]>(tagName);
        foreach (var entry in entries) UpdatePokedex((ushort)entry[0], (byte)entry[1]);
    }
}
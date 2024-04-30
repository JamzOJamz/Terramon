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
    private readonly PCService PC = new();
    private readonly PokedexService Pokedex = new();

    private int _activeSlot = -1;

    private bool _lastPlayerInventory;
    private int _premierBonusCount;

    public bool HasChosenStarter;

    public int ActiveSlot
    {
        get => _activeSlot;
        set
        {
            // Toggle off dedicated pet slot
            if (_activeSlot == -1)
                Player.hideMisc[0] = true;
            _activeSlot = value;
            var buffType = ModContent.BuffType<PokemonCompanion>();
            Player.ClearBuff(buffType);
            if (value >= 0) Player.AddBuff(buffType, 2);
        }
    }

    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<TerramonPlayer>();

    public PokedexService GetPokedex()
    {
        return Pokedex;
    }

    public override void OnEnterWorld()
    {
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(Party);
    }

    public override void PreUpdate()
    {
        if (!Player.HasBuff<PokemonCompanion>() && ActiveSlot >= 0)
        {
            var oldSlot = ActiveSlot;
            ActiveSlot = -1;
            UILoader.GetUIState<PartyDisplay>().RecalculateSlot(oldSlot);
        }
        
        if (Main.playerInventory && !_lastPlayerInventory)
            UILoader.GetUIState<PartyDisplay>().Sidebar.ForceKillAnimation();

        _lastPlayerInventory = Main.playerInventory;

        if (_premierBonusCount <= 0 || Main.npcShop != 0) return;
        var premierBonus = _premierBonusCount / 10;
        if (premierBonus > 0)
        {
            Main.NewText(premierBonus == 1
                ? Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonus")
                : Language.GetTextValue("Mods.Terramon.GUI.Shop.PremierBonusPlural", premierBonus));

            Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(),
                premierBonus);
        }

        _premierBonusCount = 0;
    }

    public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item)
    {
        //use CanSellItem rather than PostSellItem because PostSellItem doesn't return item data correctly
        if (item.type == ModContent.ItemType<PokeBallItem>())
            _premierBonusCount -= item.stack;

        return item.type != ModContent.ItemType<MasterBallItem>() && base.CanSellItem(vendor, shopInventory, item);
    }

    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        if (item.type == ModContent.ItemType<PokeBallItem>())
            _premierBonusCount++;
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
        if (ActiveSlot == first)
            ActiveSlot = second;
        else if (ActiveSlot == second)
            ActiveSlot = first;
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

    public PCBox TransferPokemonToPC(PokemonData data)
    {
        return PC.StorePokemon(data);
    }

    public string GetDefaultNameForPCBox(PCBox box)
    {
        return "Box " + (PC.Boxes.IndexOf(box) + 1);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["starterChosen"] = HasChosenStarter;
        if (ActiveSlot >= 0)
            tag["activeSlot"] = ActiveSlot;
        SaveParty(tag);
        SavePokedex(tag);
        SavePC(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        HasChosenStarter = tag.GetBool("starterChosen");
        if (tag.TryGet("activeSlot", out int slot))
            ActiveSlot = slot;
        LoadParty(tag);
        LoadPokedex(tag);
        LoadPC(tag);
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
        tag["pokedex"] = Pokedex.Entries.Select(entry => new[] { entry.Key, entry.Value }).ToList();
    }

    private void LoadPokedex(TagCompound tag)
    {
        const string tagName = "pokedex";
        if (!tag.ContainsKey(tagName)) return;
        var entries = tag.GetList<int[]>(tagName);
        foreach (var entry in entries) UpdatePokedex((ushort)entry[0], (byte)entry[1]);
    }

    private void SavePC(TagCompound tag)
    {
        tag["pc"] = PC.Boxes;
    }

    private void LoadPC(TagCompound tag)
    {
        const string tagName = "pc";
        if (!tag.ContainsKey(tagName)) return;
        PC.Boxes.Clear();
        var boxes = tag.GetList<PCBox>(tagName);
        foreach (var box in boxes)
        {
            box.Service = PC;
            PC.Boxes.Add(box);
        }
    }
}
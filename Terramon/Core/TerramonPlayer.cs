using System.Text;
using EasyPacketsLib;
using Terramon.Content.Buffs;
using Terramon.Content.Commands;
using Terramon.Content.GUI;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Packets;
using Terramon.Content.Projectiles;
using Terramon.Content.Tiles.Banners;
using Terramon.Content.Tiles.Interactive;
using Terramon.Core.Battling;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer
{
    private readonly PCService _pc = new();
    private readonly PokedexService _pokedex = new();
    private readonly PokedexService _shinyDex = new();

    private int _activePCTileEntityID = -1;
    private int _activeSlot;
    private int _lastActiveSlot = -1;
    private PokemonPet _activePetProjectile;
    private bool _lastPlayerInventory;
    private int _premierBonusCount;
    private bool _receivedShinyCharm;
    
    public Vector3 ColorPickerHSL;
    public bool HasChosenStarter;
    public bool HasPokeBanner;
    public bool HasShinyCharm;
    public BattleInstance Battle;

    public int ActivePCTileEntityID
    {
        get => _activePCTileEntityID;
        set
        {
            _activePCTileEntityID = value;
            if (Main.myPlayer != Player.whoAmI) return; // Only the local player should handle this
            if (value != -1)
                PCInterface.OnOpen();
            else
                PCInterface.OnClose();
        }
    }

    public PokemonData[] Party { get; } = new PokemonData[6];

    public int ActiveSlot
    {
        get => _activeSlot;
        set
        {
            // Don't double execute logic here
            if (_activeSlot == value) return;
            
            // Toggle off dedicated pet slot
            if (_activeSlot == -1 && !Player.miscEquips[0].IsAir)
                Player.hideMisc[0] = true;

            // Cancel Pokémon cry sound in party display UI
            if (Player == Main.LocalPlayer)
                PartySidebarSlot.CrySoundSource?.Cancel();
            
            _activeSlot = value;
            if (value != -1)
                _lastActiveSlot = _activeSlot;

            var buffType = ModContent.BuffType<PokemonCompanion>();
            var hasBuff = Player.HasBuff(buffType);
            switch (value)
            {
                case >= 0 when !hasBuff:
                    Player.AddBuff(buffType, 2);
                    break;
                case -1 when hasBuff:
                    Player.ClearBuff(buffType);
                    break;
            }
        }
    }

    public PokemonPet ActivePetProjectile
    {
        get => _activeSlot >= 0 ? _activePetProjectile : null;
        set => _activePetProjectile = value;
    }

    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<TerramonPlayer>();

    /// <summary>
    ///     Returns this player's currently active Pokémon, or null if there is none.
    /// </summary>
    public PokemonData GetActivePokemon()
    {
        return ActiveSlot >= 0 ? Party[ActiveSlot] : null;
    }

    public PokedexService GetPokedex(bool shiny = false)
    {
        return shiny ? _shinyDex : _pokedex;
    }

    public PCService GetPC()
    {
        return _pc;
    }

    public override void OnEnterWorld()
    {
        Terramon.RefreshPartyUI();

        // Request a full sync of the World Dex from the server when joining a host in multiplayer
        if (Main.netMode == NetmodeID.MultiplayerClient) Mod.SendPacket(new RequestWorldDexRpc());
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Clear active PC when player dies
        TurnOffUsedPC();
    }

    private void TurnOffUsedPC()
    {
        if (_activePCTileEntityID != int.MaxValue)
        {
            if (_activePCTileEntityID != -1 &&
                TileEntity.ByID.TryGetValue(_activePCTileEntityID, out var entity) && entity is PCTileEntity
                {
                    PoweredOn: true
                } pc)
                pc.ToggleOnOff();
        }
        else
        {
            ActivePCTileEntityID = -1;
        }
    }

    public override void OnRespawn()
    {
        if (Player.whoAmI != Main.myPlayer) return;

        // Reapply companion buff on player respawn
        if (_activeSlot >= 0 && !Player.HasBuff(ModContent.BuffType<PokemonCompanion>()))
            Player.AddBuff(ModContent.BuffType<PokemonCompanion>(), 2);
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        ProcessActiveMonTriggers();

        if (HasChosenStarter && KeybindSystem.HubKeybind.JustPressed)
            HubUI.ToggleActive();

        if (!KeybindSystem.TogglePartyKeybind.JustPressed) return;
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (inventoryParty.Visible) inventoryParty.SimulateToggleSlots();
    }

    private void ProcessActiveMonTriggers()
    {
        var shouldPlaySound = false;

        if (KeybindSystem.TogglePokemonKeybind.JustPressed)
        {
            shouldPlaySound = true;
            if (_activeSlot != -1)
                ActiveSlot = -1;
            else
                ActiveSlot = _lastActiveSlot;
        }
        else if (KeybindSystem.NextPokemonKeybind.JustPressed)
        {
            shouldPlaySound = true;
            if (_activeSlot != -1)
                ActiveSlot = _activeSlot == 5 ? 0 : _activeSlot + 1;
            else
                ActiveSlot = _lastActiveSlot;
        }
        else if (KeybindSystem.PrevPokemonKeybind.JustPressed)
        {
            shouldPlaySound = true;
            if (_activeSlot != -1)
                ActiveSlot = _activeSlot == 0 ? 5 : _activeSlot - 1;
            else
                ActiveSlot = _lastActiveSlot;
        }

        if (!shouldPlaySound) return;
        if (_activeSlot != -1)
        {
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkmn_recall") { Volume = 0.375f });
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/Cries/" + Party[_activeSlot].InternalName)
                { Volume = 0.2525f });
        }
        else
        {
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_consume")
                { Volume = 0.35f });
        }
    }

    public override void ResetEffects()
    {
        HasShinyCharm = false;
    }

    public override void PostUpdateBuffs()
    {
        if (TooltipOverlay.IsHoldingPokemon())
            Player.controlUseItem = false;
    }

    public override void PreUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;

        if ((Player.chest != -1 || (!Main.playerInventory && !HubUI.Active)) && ActivePCTileEntityID != -1)
            TurnOffUsedPC();

        // Handle player removing companion buff manually (right-clicking the buff icon)
        if (!Player.HasBuff<PokemonCompanion>() && _activeSlot >= 0 && !Player.dead)
            ActiveSlot = -1;

        // End the sidebar animation if the player opens their inventory to prevent visual bugs
        if (Main.playerInventory && !_lastPlayerInventory)
            PartyDisplay.Sidebar.ForceKillAnimation();

        _lastPlayerInventory = Main.playerInventory;

        if (_premierBonusCount <= 0 || Main.npcShop != 0) return;
        var premierBonus = _premierBonusCount / 10;
        if (premierBonus > 0)
        {
            Main.NewText(premierBonus == 1
                ? Language.GetTextValue("Mods.Terramon.GUI.NPCShop.PremierBonus")
                : Language.GetTextValue("Mods.Terramon.GUI.NPCShop.PremierBonusPlural", premierBonus));

            Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(),
                premierBonus);
        }

        _premierBonusCount = 0;
    }

    public override void PostUpdate()
    {
        Battle?.Update();
    }

    public override void PreUpdateBuffs()
    {
        if (HasPokeBanner)
            Player.AddBuff(ModContent.BuffType<PokeBannerBuff>(), 2, quiet: false);
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
    /// <param name="data">The Pokémon to add to the party.</param>
    /// <param name="justRegistered">Whether the Pokémon was just registered in the Pokédex.</param>
    public bool AddPartyPokemon(PokemonData data, out bool justRegistered)
    {
        justRegistered = UpdatePokedex(data.ID, PokedexEntryStatus.Registered, shiny: data.IsShiny);
        var nextIndex = NextFreePartyIndex();
        if (nextIndex == 6) return false;
        Party[nextIndex] = data;

        return true;
    }

    public void SwapParty(int first, int second)
    {
        if (_activeSlot == first)
            ActiveSlot = second;
        else if (_activeSlot == second)
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

    public bool UpdatePokedex(ushort id, PokedexEntryStatus status, bool force = false, bool shiny = false)
    {
        TerramonWorld.UpdateWorldDex(id, status, Player.name, force);
        var hasEntry = _pokedex.Entries.TryGetValue(id, out var entry);
        var entryUpdated = false;

        if (hasEntry)
        {
            if (entry.Status < status || force)
            {
                entry.Status = status;
                entryUpdated = true;
            }

            if (status == PokedexEntryStatus.Registered)
            {
                entry.CaughtCount++;
                HandleCatchMilestoneRewards(id, entry.CaughtCount);
            }
        }

        if (shiny)
        {
            var hasShinyEntry = _shinyDex.Entries.TryGetValue(id, out var shinyEntry);
            if (hasShinyEntry)
            {
                if (shinyEntry.Status < status || force)
                    shinyEntry.Status = status;

                if (shinyEntry.Status == PokedexEntryStatus.Registered)
                    shinyEntry.CaughtCount++;
            }
        }

        if (HubUI.Active) UILoader.GetUIState<HubUI>().RefreshPokedex(id);
        if (!force && status == PokedexEntryStatus.Registered && entryUpdated &&
            _pokedex.RegisteredCount == Terramon.LoadedPokemonCount && !_receivedShinyCharm)
            GiveShinyCharmReward();
        return force ? hasEntry : entryUpdated;
    }

    private void HandleCatchMilestoneRewards(ushort id, int caughtCount)
    {
        var milestone = GetMilestoneFromCaughtCount(caughtCount);
        if (milestone == null) return;

        TerramonWorld.QueueNewText(Language.GetTextValue($"Mods.Terramon.Misc.CatchMilestone{caughtCount}",
            Terramon.DatabaseV2.GetLocalizedPokemonName(id).Value), TerramonCommand.ChatColorYellow);

        GiveBannerReward(id, milestone.Value);
    }

    private static BannerTier? GetMilestoneFromCaughtCount(int caughtCount)
    {
        return caughtCount switch
        {
            3 => BannerTier.Tier1,
            6 => BannerTier.Tier2,
            9 => BannerTier.Tier3,
            12 => BannerTier.Tier4,
            _ => null
        };
    }

    private void GiveBannerReward(ushort id, BannerTier tier)
    {
        if (!PokemonEntityLoader.IDToBannerType.TryGetValue(id, out var bannerType)) return;

        var bannerItem = new Item();
        bannerItem.SetDefaults(bannerType);
        var modItem = bannerItem.ModItem as PokeBannerItem;
        modItem!.Tier = tier;
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), bannerItem);
    }

    private void GiveShinyCharmReward()
    {
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
            ModContent.ItemType<ShinyCharm>());
        _receivedShinyCharm = true;
    }

    public PCBox TransferPokemonToPC(PokemonData data)
    {
        return _pc.StorePokemon(data);
    }

    public string GetDefaultNameForPCBox(PCBox box)
    {
        return Language.GetTextValue("Mods.Terramon.Misc.PCBoxDefaultName", _pc.Boxes.IndexOf(box) + 1);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["flags"] = (byte)new BitsByte(HasChosenStarter, _receivedShinyCharm);
        if (_activeSlot >= 0)
            tag["activeSlot"] = _activeSlot;
        SaveParty(tag);
        SavePokedex(tag);
        SavePC(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("flags"))
        {
            BitsByte flags = tag.GetByte("flags");
            HasChosenStarter = flags[0];
            _receivedShinyCharm = flags[1];
        }

        if (tag.TryGet("activeSlot", out int slot))
            _activeSlot = slot;

        LoadParty(tag);
        LoadPokedex(tag);
        LoadPC(tag);
    }

    private void SaveParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            if (Party[i] == null) continue;
            tag[$"p{i}"] = Party[i].SerializeData();
        }
    }

    private void LoadParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            var tagName = $"p{i}";
            if (tag.ContainsKey(tagName)) Party[i] = PokemonData.Load(tag.GetCompound(tagName));
        }
    }

    private void SavePokedex(TagCompound tag)
    {
        var pokedexEntries = _pokedex.GetEntriesForSaving();
        if (pokedexEntries.Count > 0)
            tag["pokedex"] = pokedexEntries;
        var shinyDexEntries = _shinyDex.GetEntriesForSaving();
        if (shinyDexEntries.Count > 0)
            tag["shinyDex"] = shinyDexEntries;
    }

    private void LoadPokedex(TagCompound tag)
    {
        const string tagName = "pokedex";
        const string shinyTagName = "shinyDex";
        if (tag.ContainsKey(tagName)) _pokedex.LoadEntries(tag.GetList<int[]>(tagName));
        if (tag.ContainsKey(shinyTagName)) _shinyDex.LoadEntries(tag.GetList<int[]>(shinyTagName));
    }

    private void SavePC(TagCompound tag)
    {
        tag["pc"] = _pc.Boxes.Select(box => box.SerializeData()).ToList();
    }

    private void LoadPC(TagCompound tag)
    {
        const string tagName = "pc";
        if (!tag.ContainsKey(tagName)) return;
        _pc.Boxes.Clear();
        var boxes = tag.GetList<TagCompound>(tagName).Select(PCBox.Load);
        foreach (var box in boxes)
        {
            box.Service = _pc;
            _pc.Boxes.Add(box);
        }
    }

    public string GetPackedTeam()
    {
        StringBuilder sb = new();
        foreach (var p in Party)
        {
            if (p == null) continue;
            sb.Append($"{p.GetPacked()}]");
        }

        return sb.ToString().TrimEnd(']');
    }

    #region Network Sync

    /// <summary>
    ///     The bitmask of the <see cref="PokemonData" /> fields that require syncing for the player's active Pokémon.
    ///     These fields will be observed for changes and if they are changed, their new values will be forwarded to other
    ///     clients.
    /// </summary>
    private const int ActivePokemonSyncFields = PokemonData.BitID | PokemonData.BitLevel;

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        var activePokemonData = GetActivePokemon();
        if (activePokemonData != null)
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData), toWho, fromWho);
        if (HasChosenStarter)
            Mod.SendPacket(new PlayerFlagsRpc((byte)Player.whoAmI, true), toWho, fromWho);
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        var clone = (TerramonPlayer)targetCopy;
        for (var i = 0; i < Party.Length; i++)
            if (clone.Party[i] != null && Party[i] == null)
                clone.Party[i] = null;
            else if (clone.Party[i] == null && Party[i] != null)
                clone.Party[i] = Party[i].ShallowCopy();
            else if (clone.Party[i] != null && Party[i] != null)
                Party[i].CopyNetStateTo(clone.Party[i], ActivePokemonSyncFields);
        clone.ActiveSlot = ActiveSlot;
        clone.HasChosenStarter = HasChosenStarter;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        var clone = (TerramonPlayer)clientPlayer;
        var activePokemonData = GetActivePokemon();
        var cloneActivePokemonData = clone.GetActivePokemon();
        if ((activePokemonData == null && cloneActivePokemonData != null) ||
            (activePokemonData != null && cloneActivePokemonData == null))
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData), -1, Main.myPlayer, true);
        else if (activePokemonData != null &&
                 activePokemonData.IsNetStateDirty(cloneActivePokemonData, ActivePokemonSyncFields,
                     out var dirtyFields))
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData, dirtyFields), -1,
                Main.myPlayer, true);
        if (HasChosenStarter == clone.HasChosenStarter) return;
        Mod.SendPacket(new PlayerFlagsRpc((byte)Player.whoAmI, HasChosenStarter), -1, Main.myPlayer, true);
    }

    #endregion
}
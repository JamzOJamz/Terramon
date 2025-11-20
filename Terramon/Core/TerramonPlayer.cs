using System.Text;
using EasyPacketsLib;
using MonoMod.Cil;
using Showdown.NET.Definitions;
using Terramon.Content.Buffs;
using Terramon.Content.Commands;
using Terramon.Content.GUI;
using Terramon.Content.GUI.TurnBased;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Projectiles;
using Terramon.Content.Tiles.Banners;
using Terramon.Content.Tiles.Interactive;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer, IBattleProvider
{
    private readonly PCService _pc = new();
    private readonly PokedexService _pokedex = new();
    private readonly PokedexService _shinyDex = new();

    private int _activePCTileEntityID = -1;
    private PokemonPet _activePetProjectile;
    private int _activeSlot;
    private int _lastActiveSlot = -1;
    private bool _lastPlayerInventory;
    private int _premierBonusCount;
    private bool _receivedShinyCharm;
    public BattleInstance Battle;

    public Vector3 ColorPickerHSL;
    public bool ExpShareOn;
    public bool HasChosenStarter;
    public bool HasExpCharm;
    public bool HasPokeBanner;
    public bool HasShinyCharm;
    public ExpShareSettings NonParticipantSettings = new(0.5f);
    public ExpShareSettings ParticipantSettings = new();

    internal BattleClient _battleClient;
    public static int HoveredPlayer = -1;
    public static int BattleTicks;

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

    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.Terramon();

    #region IBattleProvider
    public BattleProviderType ProviderType => BattleProviderType.Player;
    public BattleClient BattleClient => _battleClient;
    public Entity SyncedEntity => Player;
    public string BattleName => Player.name;
    public PokemonData[] GetBattleTeam() => Party;
    public void StartBattleEffects(bool before)
    {
        if (!before)
            ActivePetProjectile?.ConfrontFoe(_battleClient);
        if (Player.whoAmI == Main.myPlayer)
        {
            BattleTicks = 0;
            BattleClient.StartLocalBattle();
        }
    }
    public void StopBattleEffects()
    {
        ActivePetProjectile?.ConfrontFoe(null);
    }
    public void Reply(BattleMessage m)
    {
        switch (m)
        {
            case ChallengeQuestion:

                // Open UI to accept challenge
                Main.NewText($"This ({m.Sender.BattleName}) Fucker Wants to Battle you");

                // DEBUG: Accept immediately
                m.Return(new ChallengeAnswer(yes: true));
                break;
            case ChallengeAnswer c:

                if (c.Yes)
                {
                    Main.NewText($"{m.Sender.BattleName} has accepted your challenge");
                    // Imbalanced operation: Will send battle pick to foe here,
                    // and the other one's pick will be sent in SlotChoice to server
                    var slot = (byte)(_activeSlot == -1 ? 1 : _activeSlot + 1);
                    m.Return(new SlotChoice(slot: slot));
                }
                else
                    Main.NewText($"{m.Sender.BattleName} has declined your challenge");
                break;
            case SlotChoice:
                var slot0 = (byte)(_activeSlot == -1 ? 1 : _activeSlot + 1);
                var pick = new SlotChoice(slot: slot0)
                {
                    Sender = this
                };
                pick.Send();
                break;
            case TeamQuestion:
                m.Return(new TeamAnswer(this.GetNetTeam()));
                break;
            case TieQuestion:

                // Show like a little notification and/or change the color of the tie button
                Main.NewText($"{m.Sender.BattleName} asked to agree to a tie");
                break;
            case TieTakeback:

                // Reverse the effect from the above message
                Main.NewText($"{m.Sender.BattleName} doesn't want a tie anymore");
                break;
        }
    }
    public void Witness(BattleMessage m)
    {
        Console.WriteLine($"Client: Witnessing a {m.GetType().Name}");

        switch (m)
        {
            case ResetEverythingStatement:

                foreach (var plr in Main.ActivePlayers)
                    ((IBattleProvider)plr.Terramon()).BattleStopped();
                foreach (var npc in Main.ActiveNPCs)
                {
                    if (npc.ModNPC is IBattleProvider p)
                        p.BattleStopped();
                }

                break;
            case ChallengeQuestion:

                // Set client fields
                // Keep in mind that this is also witnessed by the sender
                m.Sender.State = ClientBattleState.Requested;
                m.Sender.Foe = m.Recipient;
                break;
            case ChallengeTakeback:

                // Unset client fields
                m.Sender.State = ClientBattleState.None;
                m.Sender.Foe = null;
                break;
            case ChallengeAnswer c:

                if (c.Yes) // If the sender accepted
                {
                    // Set client fields
                    m.Sender.State = m.Recipient.State = ClientBattleState.PollingSlot;
                    m.Sender.Foe = m.Recipient;

                    // Create battle field early to avoid nullrefs
                    var bf = new BattleField()
                    {
                        A = new(m.Recipient),
                        B = new(m.Sender),
                    };

                    m.Recipient.Field = m.Sender.Field = bf;
                }
                else
                {
                    // Unset client fields
                    m.Recipient.State = ClientBattleState.None;
                    m.Recipient.Foe = null;
                }
                break;
            case SlotChoice s:
                m.Sender.Pick = s.Slot;
                break;
            case TeamAnswer:
                m.Sender.State = ClientBattleState.SetTeam;
                break;
            case StartBattleStatement s:

                // Show start message and do battle start effects
                var owner = s.BattleOwner;
                var other = owner.Foe;
                var a = owner.BattleName;
                var b = other.BattleName;

                Main.NewText($"{a} started a battle against {b}!", Color.Aqua);

                // If we're in singleplayer, break early so this stuff only runs from BattleManager
                if (Main.netMode == NetmodeID.SinglePlayer)
                    break;

                // Set states
                owner.State = other.State = ClientBattleState.Ongoing;

                // Effects
                owner.StartBattleEffects(before: true);
                other.StartBattleEffects(before: false);
                owner.StartBattleEffects(before: false);
                break;
            case ForfeitStatement f:

                // Show forfeit message and do battle end effects
                var forfeiter = f.Forfeiter;
                var winner = forfeiter.Foe;
                a = forfeiter.BattleName;
                b = winner.BattleName;

                Main.NewText($"{a} forfeited their battle against {b}!", Color.Lime);

                forfeiter.BattleStopped();
                winner.BattleStopped();
                break;
            case WinStatement w:

                // Show win message and do battle end effects
                winner = w.Winner;
                var loser = winner.Foe;
                a = winner.BattleName;
                b = loser.BattleName;

                Main.NewText($"{a} defeated {b}!", Color.Lime);

                winner.BattleStopped();
                loser.BattleStopped();
                break;
            case TieQuestion:
                m.Sender.TieRequest = true;
                break;
            case TieTakeback:
                m.Sender.TieRequest = false;
                break;
            case TieStatement t:

                // Show tie messsage and do battle end effects
                var either = t.EitherParticipant;
                other = either.Foe;
                a = either.BattleName;
                b = other.BattleName;

                switch (t.Type)
                {
                    case TieStatement.TieType.Regular:
                        Main.NewText($"{a} and {b}'s battle ended in a tie!", Color.Yellow);
                        break;
                    case TieStatement.TieType.Agreed:
                        Main.NewText($"{a} and {b} agreed to a tie!", Color.Yellow);
                        break;
                    case TieStatement.TieType.Forced:
                        Main.NewText($"{a} and {b}'s battle was forcibly ended!", Color.Red);
                        break;
                }

                either.BattleStopped();
                other.BattleStopped();
                break;
        }
    }
    public void SetActiveSlot(byte newSlot)
    {
        ActiveSlot = newSlot;

        var loc = BattleClient.LocalClient;

        if (loc == BattleClient)
        {
            TestBattleUI.PlayerPanel.CurrentMon = GetActivePokemon();
        }
        else if (loc.Foe == this)
        {
            TestBattleUI.FoePanel.CurrentMon = GetActivePokemon();
        }
    }

    #endregion

    public bool StartBattle(IBattleProvider other)
    {
        var c = other.BattleClient;
        if (c is null || c.State != ClientBattleState.None)
            return false;

        var question = new ChallengeQuestion
        {
            Sender = this
        };
        question.Send(other);
        return true;
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;
        IL_Main.DrawMouseOver += static (il) =>
        {
            var c = new ILCursor(il);

            var playerIndex = 0;
            c.GotoNext(
                i => i.MatchLdsfld<Main>(nameof(Main.player)),
                i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
                i => i.MatchLdelemRef(),
                i => i.MatchLdcI4(0),
                i => i.MatchStfld<Player>(nameof(Player.cursorItemIconEnabled)),
                i => i.MatchLdsfld<Main>(nameof(Main.player)),
                i => i.MatchLdloc(out playerIndex));

            c.EmitLdloc(playerIndex);
            c.EmitStsfld(typeof(TerramonPlayer).GetField(nameof(HoveredPlayer)));
        };
    }
    
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

    public override void SetControls()
    {
        if (TestBattleUI.Instance.Visible && Player.controlInv && Player.releaseInventory)
        {
            TestBattleUI.HandleExit();
            Player.releaseInventory = false;
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        ProcessActiveMonTriggers();

        if (HasChosenStarter && KeybindSystem.HubKeybind.JustPressed)
            HubUI.ToggleActive();

        if (_battleClient != null && HoveredPlayer != -1 && _battleClient.State == ClientBattleState.None)
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                StartBattle(Main.player[HoveredPlayer].Terramon());
            HoveredPlayer = -1;
        }

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
        ExpShareOn = false;
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
            Main.NewText(Language.GetTextValue(
                $"Mods.Terramon.GUI.NPCShop.PremierBonus{(premierBonus != 1 ? "Plural" : string.Empty)}",
                premierBonus));

            Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(),
                premierBonus);
        }

        _premierBonusCount = 0;
    }

    private bool _locallyRequestedClient;

    public override void PostUpdate()
    {
        if (BattleClient.LocalBattleOngoing)
            BattleTicks++;
        if (_battleClient is null)
        {
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    _battleClient = new(this);
                    break;
                case NetmodeID.MultiplayerClient:
                    if (Player.whoAmI == Main.myPlayer)
                        goto case NetmodeID.SinglePlayer;
                    if (_locallyRequestedClient)
                        break;
                    Mod.SendPacket(new RequestClientRpc(), Player.whoAmI, Main.myPlayer, true);
                    _locallyRequestedClient = true;
                    break;
                case NetmodeID.Server:
                    if (_locallyRequestedClient)
                        break;
                    Mod.SendPacket(new RequestClientRpc(), Player.whoAmI);
                    _locallyRequestedClient = true;
                    break;
            }
        }
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
            var hasShinyEntry = _shinyDex.Entries.TryGetValue((ushort)id, out var shinyEntry);
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
        for (int i = 0; i < Party.Length; i++)
        {
            var p = Party[i];
            if (p == null) break;
            sb.Append($"{p.GetPacked(i)}]");
        }

        return sb.ToString().TrimEnd(']');
    }

    public void DefeatedPokemon(PokemonData defeated, bool battleLogs)
    {
        if (!ExpShareOn)
        {
            var active = GetActivePokemon();
            if (active.Status == NonVolatileStatus.Fnt)
                return;
            var expGain = active.ExperienceFromDefeat(defeated, 1f, this);
            var evGain = active.EVsFromDefeat(defeated, false);
            active.GainExperience(expGain, out var levelsGained, out _);
            active.TrainEVs(evGain, out _);
            if (battleLogs)
                LogYield(active, expGain, levelsGained, evGain);
            return;
        }

        var p = Party;
        ParticipantSettings.Recalculate(p, true);
        NonParticipantSettings.Recalculate(p, false);
        for (int i = 0; i < Party.Length; i++)
        {
            var poke = Party[i];
            if (poke is null)
                continue;
            ref ExpShareSettings settings =
                ref (poke.Participated ? ref ParticipantSettings : ref NonParticipantSettings);
            float myMult = settings[i];
            if (myMult == 0f)
                continue;
            var expGain = poke.ExperienceFromDefeat(defeated, myMult, this);
            var evGain = poke.EVsFromDefeat(defeated, settings.Disabled[i]);
            poke.GainExperience(expGain, out var levelsGained, out _);
            poke.TrainEVs(evGain, out _);
            if (battleLogs)
                LogYield(poke, expGain, levelsGained, evGain);
        }
    }

    private static void LogYield(PokemonData recipient, int expGain, int levelsGained,
        IEnumerable<(StatID Stat, byte EffortIncrease)> gains)
    {
        BattleInstance.Log($"{recipient.DisplayName} gained {expGain} EXP!", BattleInstance.BattleReceiveFollowup);
        if (levelsGained != 0)
            BattleInstance.Log($"{recipient.DisplayName} is now level {recipient.Level}!", BattleInstance.BattleReceiveFollowup);
        if (gains is null)
            return;
        foreach (var (stat, increase) in gains)
            BattleInstance.Log($"{recipient.DisplayName}'s {stat} EV increased by {increase}!", BattleInstance.MetaFollowup);
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
        if (HasChosenStarter != clone.HasChosenStarter)
            Mod.SendPacket(new PlayerFlagsRpc((byte)Player.whoAmI, HasChosenStarter), -1, Main.myPlayer, true);
    }

    #endregion
}
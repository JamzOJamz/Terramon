using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using EasyPacketsLib;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terramon.Content.Buffs;
using Terramon.Content.Items.Materials;
using Terramon.Content.Packets;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class TerramonWorld : ModSystem
{
    /// <summary>
    ///     The denominator (1/x) for the probability of apricorns falling from a tree when it is shaken.
    ///     Constant value of 8, meaning a 1/8 or approximately 12.5% chance.
    /// </summary>
    /*private const int
        ApricornDropChanceDenominator = 8; // TODO: Make this configurable through a gameplay config option*/
    private static SlotId _currentSlotId;

    private static float _originalMusicVolume;
    private static bool _soundEndedLastFrame;
    private static float _lastMusicVolume;
    private static PokedexService _worldDex;

    private static ApricornItem[] _apricornItems;

    private static bool _vanillaTreeShakeFailed;

    /// <summary>
    ///     Plays a sound while temporarily lowering the background music volume.
    ///     The background music volume is restored once the sound has finished playing.
    ///     This is useful for preventing dissonance when playing longer sounds that would overlap with background music.
    /// </summary>
    /// <param name="style">The sound style containing the parameters for the sound to be played.</param>
    /// <param name="volumeMultiplier">The multiplier for quieting the background music volume.</param>
    public static void PlaySoundOverBGM(in SoundStyle style, float volumeMultiplier = 0.45f)
    {
        var slotId = SoundEngine.PlaySound(style);
        if (Main.musicVolume <= 0) return;
        _originalMusicVolume = Main.musicVolume;
        _currentSlotId = slotId;
        Main.musicVolume = Main.soundVolume * volumeMultiplier;
        _lastMusicVolume = Main.musicVolume;
    }

    public static void UpdateWorldDex(int id, PokedexEntryStatus status, string lastUpdatedBy = null,
        bool force = false, bool netSend = true)
    {
        if (_worldDex == null) return;
        var hasEntry = _worldDex.Entries.TryGetValue(id, out var entry);
        if (!hasEntry) return;
        if (!force && entry.Status >= status) return;
        if (entry.Status != PokedexEntryStatus.Registered)
            entry.LastUpdatedBy = lastUpdatedBy;
        entry.Status = status;
        if (netSend && Main.netMode == NetmodeID.MultiplayerClient) // Sync the World Dex on all clients in multiplayer
            Terramon.Instance.SendPacket(new UpdateWorldDexRpc([((ushort)id, entry)]),
                ignoreClient: Main.myPlayer, forward: true);
    }

    public static PokedexService GetWorldDex()
    {
        return _worldDex;
    }

    public override void PreSaveAndQuit()
    {
        Terramon.ResetUI();
        Main.LocalPlayer.ClearBuff(ModContent.BuffType<PokemonCompanion>());
    }

    public override void ClearWorld()
    {
        _worldDex = new PokedexService();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        var entriesList = new List<int[]>();
        foreach (var entry in _worldDex.Entries)
        {
            if (entry.Value.Status == PokedexEntryStatus.Undiscovered) continue;
            var lastUpdatedBy = entry.Value.LastUpdatedBy;
            var entryDataLength = 2 + (entry.Value.LastUpdatedBy?.Length ?? 0);
            var entryData = new int[entryDataLength];
            entryData[0] = entry.Key;
            entryData[1] = (byte)entry.Value.Status;
            if (entryDataLength > 2) // If there is a lastUpdatedBy string
                for (var i = 0; i < lastUpdatedBy.Length; i++)
                    entryData[i + 2] = lastUpdatedBy[i];
            entriesList.Add(entryData);
        }

        tag["worldDex"] = entriesList;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (!tag.TryGet<List<int[]>>("worldDex", out var entries)) return;
        foreach (var entryData in entries)
        {
            var id = entryData[0];
            var status = (PokedexEntryStatus)entryData[1];
            string lastUpdatedBy = null;
            if (entryData.Length > 2)
            {
                var charArray = new char[entryData.Length - 2];
                for (var i = 2; i < entryData.Length; i++) charArray[i - 2] = (char)entryData[i];
                lastUpdatedBy = new string(charArray);
            }

            _worldDex.Entries[id] = new PokedexEntry(status, lastUpdatedBy);
        }
    }

    public override void PostSetupContent()
    {
        _apricornItems = ModContent.GetContent<ApricornItem>().Where(item => item.Obtainable).ToArray();
    }

    public override void Load()
    {
        On_Main.DoUpdate += MainDoUpdate_Detour;
        On_WorldGen.ShakeTree += WorldGenShakeTree_Detour;
        IL_WorldGen.ShakeTree += HookShakeTree;
    }

    private static void HookShakeTree(ILContext il)
    {
        try
        {
            // Start the Cursor at the start
            var c = new ILCursor(il);

            // Try to find where massive if else chain ends to inject code
            c.GotoNext(i => i.MatchLdcI4(12), i => i.MatchCallvirt(typeof(UnifiedRandom), "Next"),
                i => i.MatchBrtrue(out _), i => i.MatchLdloc3(), i => i.MatchLdcI4(12));

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the result of the random number before the delegate consumes it
            c.EmitDup();

            // Check result of the random number
            c.EmitDelegate<Action<int>>(result =>
            {
                if (result == 0) return;
                _vanillaTreeShakeFailed = true;
            });

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the tree type before the delegate consumes it
            c.EmitDup();

            // Check tree type
            c.EmitDelegate<Action<TreeTypes>>(type =>
            {
                if (type == TreeTypes.Ash) return;
                _vanillaTreeShakeFailed = true;
            });
        }
        catch
        {
            MonoModHooks.DumpIL(Terramon.Instance, il);
        }
    }

    public override void Unload()
    {
        _apricornItems = null;
    }

    private static void MainDoUpdate_Detour(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        // Set GameTime and MousePosition for UILoader
        UILoader.GameTime = gameTime;

        orig(self, ref gameTime);

        // Update all active tweens
        Tween.DoUpdate();

        if (Main.musicVolume != _lastMusicVolume)
        {
            // Volume is changed by something else, abort the fade
            _currentSlotId = default;
            _soundEndedLastFrame = false;
        }

        if (!SoundEngine.TryGetActiveSound(_currentSlotId, out var activeSound))
        {
            if (!_soundEndedLastFrame) return;
            _soundEndedLastFrame = false;
            _currentSlotId = default;

            // Fade back in the music volume
            Tween.To(() => Main.musicVolume, x => { Main.musicVolume = x; }, _originalMusicVolume, 0.75f);

            return;
        }

        if (!activeSound.IsPlaying) return;
        _soundEndedLastFrame = true;
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "numTreeShakes")]
    private static extern ref int GetNumTreeShakes(WorldGen instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "treeShakeX")]
    private static extern ref int[] GetTreeShakeX(WorldGen instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "treeShakeY")]
    private static extern ref int[] GetTreeShakeY(WorldGen instance);

    private static void WorldGenShakeTree_Detour(On_WorldGen.orig_ShakeTree orig, int i, int j)
    {
        if (_apricornItems.Length == 0)
        {
            orig(i, j);
            return;
        }

        WorldGen.GetTreeBottom(i, j, out var x, out var y);
        var numTreeShakes = GetNumTreeShakes(null);
        var treeShakeX = GetTreeShakeX(null);
        var treeShakeY = GetTreeShakeY(null);
        for (var k = 0; k < numTreeShakes; k++)
        {
            if (treeShakeX[k] != x || treeShakeY[k] != y) continue;
            orig(i, j);
            return;
        }

        _vanillaTreeShakeFailed = false;
        orig(i, j); // Call the original method to shake the tree
        if (!_vanillaTreeShakeFailed) return;

        var treeType = WorldGen.GetTreeType(Main.tile[x, y].TileType);
        if (!WorldGen.genRand.NextBool(3) ||
            treeType is not (TreeTypes.Forest or TreeTypes.Snow or TreeTypes.Hallowed)) return;
        y--;
        while (y > 10 && Main.tile[x, y].HasTile && TileID.Sets.IsShakeable[Main.tile[x, y].TileType]) y--;
        y++;
        if (!WorldGen.IsTileALeafyTreeTop(x, y) || Collision.SolidTiles(x - 2, x + 2, y - 2, y + 2))
            return;
        var randomApricorn = _apricornItems[WorldGen.genRand.Next(_apricornItems.Length)];
        Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Rectangle(x * 16, y * 16, 16, 16),
            randomApricorn.Type, WorldGen.genRand.Next(1, 3));
    }
}
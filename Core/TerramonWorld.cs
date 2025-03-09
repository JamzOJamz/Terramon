using EasyPacketsLib;
using ReLogic.Utilities;
using Terramon.Content.GUI;
using Terramon.Content.Packets;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.ModLoader.IO;
using Terraria.Enums;

namespace Terramon.Core;

public partial class TerramonWorld : ModSystem
{
    private static SlotId _currentSlotId;
    private static float _originalMusicVolume;
    private static bool _soundEndedLastFrame;
    private static float _lastMusicVolume;
    private static PokedexService _worldDex;

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
        if (Main.soundVolume <= 0) return;
        var reducedVolume = Main.soundVolume * volumeMultiplier;
        if (Main.musicVolume <= reducedVolume) return;
        _originalMusicVolume = Main.musicVolume;
        _currentSlotId = slotId;
        Main.musicVolume = reducedVolume;
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
        if (TooltipOverlay.IsHoldingPokemon())
            TooltipOverlay.ClearHeldPokemon(true);
        
        Terramon.ResetUI();
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

    public override void Load()
    {
        if (Main.dedServ) return;
        On_Main.DoUpdate += MainDoUpdate_Detour;
        On_Main.DoDraw += MainDoDraw_Detour;
    }

    private static void MainDoUpdate_Detour(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        // Set GameTime and MousePosition for UILoader
        UILoader.GameTime = gameTime;

        orig(self, ref gameTime);

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
    
    private static void MainDoDraw_Detour(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        // FrameSkip subtle does very weird stuff with GameTime that causes tweens to randomly go super slow if we don't do this
        double elapsedTime;

        if (Main.FrameSkipMode == FrameSkipMode.Subtle)
            elapsedTime = Tween.tweenStep;
        else
            elapsedTime = gameTime.ElapsedGameTime.TotalSeconds;

        // Update all active tweens
        Tween.DoUpdate(elapsedTime);
    }
}
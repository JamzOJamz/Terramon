using System.Collections.Generic;
using Terramon.Content.Buffs;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonWorld : ModSystem
{
    private PokedexService _worldDex;

    public void UpdateWorldDex(int id, PokedexEntryStatus status, string lastUpdatedBy = null, bool force = false)
    {
        if (_worldDex == null) return;
        var hasEntry = _worldDex.Entries.TryGetValue(id, out var entry);
        if (!hasEntry) return;
        if (!force && entry.Status >= status) return;
        if (entry.Status != PokedexEntryStatus.Registered)
            entry.LastUpdatedBy = lastUpdatedBy;
        entry.Status = status;
    }

    public override void PreSaveAndQuit()
    {
        UILoader.GetUIState<PartyDisplay>().ClearAllSlots();
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
            var lastUpdatedBy = entry.Value.LastUpdatedBy;
            var entryDataLength = 2 + (entry.Value.LastUpdatedBy?.Length ?? 0);
            var entryData = new int[entryDataLength];
            entryData[0] = entry.Key;
            entryData[1] = (byte)entry.Value.Status;
            if (entryDataLength > 2 && lastUpdatedBy != null)
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
}
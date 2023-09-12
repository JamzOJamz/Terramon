using System.Collections.Generic;
using Terramon.Content.GUI;
using Terramon.Content.Items.Mechanical;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer
{
    private readonly PokemonData[] Party = new PokemonData[6];
    public bool HasChosenStarter;
    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<TerramonPlayer>();

    public override void OnEnterWorld()
    {
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(Party);
        Main.NewText(Terramon.Database.GetPokemon(NationalDexID.Bulbasaur).Stats.HP);
        if (!HasChosenStarter) return;
        Main.NewText(Party[0].ID);
    }

    /// <summary>
    ///     Adds a Pokémon to the player's party. Returns false if their party is full; otherwise returns true.
    /// </summary>
    public bool AddPartyPokemon(PokemonData data)
    {
        var nextIndex = NextFreePartyIndex();
        if (nextIndex == -1) return false;
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

        return -1;
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
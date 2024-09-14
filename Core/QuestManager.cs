using System;
using System.Collections.Generic;
using System.Linq;
using EasyPacketsLib;
using Microsoft.Extensions.Primitives;
using Terramon.Content.Buffs;
using Terramon.Content.GUI;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Packets;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonQuest()
{
    public string[] Requirements;
    
    public string Key;
    public string Message;
    public string Author;

    public QuestCondition Conditions;
    public int Amount;
    
    public int RewardItemId;
    public int RewardItemAmount;
}

public struct QuestCondition
{
    public int? AffectedPokemonId;
    public int? AffectedPokemonType;

    public int? ItemToUse;
    public int? ItemToGather;
}

public class QuestManager
{
    public List<TerramonQuest> Quests = new List<TerramonQuest>();
    public List<Tuple<string, int>> ActiveQuests = new List<Tuple<string, int>>();

    public TerramonQuest GetQuest(string key) => Quests.First(q => q.Key == key);

    public void AddQuest(string key) => ActiveQuests.Add(new Tuple<string, int>(key, 0));

    public void TrackProgress(QuestCondition conditions)
    {
        for (int i = 0; i < ActiveQuests.Count; i++)
        {
            var quest = GetQuest(ActiveQuests[i].Item1);

            //TODO: checks n stuff
        }
    }
    
    //TODO: add CompleteQuest method which checks new quests to be added
    //TODO: add random quests
    //TODO: whatever quest loading stuff and that
}
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
using Terraria;
using Terramon.Content.Items.Vitamins;
using Humanizer;
using Terramon.Content.Items.Evolutionary;
using Terramon.Content.Items.Vanity;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace Terramon.Core;

public class TerramonQuest()
{
    public List<string> Requirements;
    
    public string Key;
    public string Name;
    public string Message;
    public string Author;

    public QuestCondition Conditions;
    public int Amount = 1;

    public int RewardItemId;
    public int RewardItemAmount = 1;
}

public struct QuestCondition
{
    public int? PokemonId;
    public int? PokemonType;

    public int? UseItem;
    public int? GatherItem;
}

//TODO: add TerramonRandQuest class to define possible randomized quests

public class QuestManager
{
    //TODO: store quests somewhere they can be registered on mod load
    public List<TerramonQuest> Quests = 
    [
        new TerramonQuest{
            Key = "TestQuest01",
            Name = "Use 3 Rare Candies",
            Conditions = new QuestCondition{
                UseItem = ModContent.ItemType<RareCandy>()
            },
            Amount = 3,
            RewardItemId = ModContent.ItemType<TrainerCap>(),
            RewardItemAmount = 1
        },
        new TerramonQuest{
            Key = "TestQuest02",
            Name = "Catch 2 Fire Type Pokemon",
            Conditions = new QuestCondition{
                PokemonType = TypeID.Fire
            },
            Amount = 2,
            RewardItemId = ModContent.ItemType<FireStone>(),
        },
        new TerramonQuest{
            Key = "TestQuest03",
            Name = "Catch a Growlithe in an Ultra Ball",
            Conditions = new QuestCondition{
                UseItem = ModContent.ItemType<UltraBallItem>(),
                PokemonId = 58
            },
            Amount = 1,
            RewardItemId = ItemID.GoldCoin,
            RewardItemAmount = 4
        },
        new TerramonQuest{
            Key = "TestQuest04",
            Name = "Mine 12 Copper Ores",
            Conditions = new QuestCondition{
                GatherItem = ItemID.CopperOre
            },
            Amount = 12,
            RewardItemId = ModContent.ItemType<RareCandy>(),
            RewardItemAmount = 3
        },
        new TerramonQuest{
            Key = "TestQuest05",
            Name = "Cut down 1 Boreal Tree",
            Conditions = new QuestCondition{
                GatherItem = ItemID.BorealWood
            },
            RewardItemId = ModContent.ItemType<MasterBallItem>(),
            Requirements = ["TestQuest04"]
        }
    ];
    public List<Tuple<string, float>> ActiveQuests =
    [
        new Tuple<string, float>("TestQuest01", 0),
        new Tuple<string, float>("TestQuest02", 0),
        new Tuple<string, float>("TestQuest03", 0),
        new Tuple<string, float>("TestQuest04", 0)
    ];
    
    public List<Tuple<TerramonQuest, float>> ActiveRandQuests = new List<Tuple<TerramonQuest, float>>(4);
    
    public List<string> CompletedQuests = new List<string>();
    public int CompletedRandQuests;
    public bool[] AcceptedRandQuests = new bool[4];

    public TerramonQuest GetQuest(string key) => Quests.First(q => q.Key == key);
    public void RegisterQuest(TerramonQuest quest) => Quests.Add(quest);
    void AddQuest(string key) => ActiveQuests.Add(new Tuple<string, float>(key, 0));

    public void TrackProgress(QuestCondition conditions)
    {
        for (int i = 0; i < ActiveQuests.Count; i++)
        {
            //-1 means quest has been completed (progress checks don't need to occur)
            if (ActiveQuests[i].Item2 == -1) return;
            
            var quest = GetQuest(ActiveQuests[i].Item1);
            var progress = GetProgressAmount(quest.Conditions, conditions);
        
            if (progress != -1)
                IncreaseQuestProgress(i, progress);
        }

        for (int i = 0; i < ActiveRandQuests.Count; i++)
        {
            if (ActiveRandQuests[i].Item2 == -1) return;
            
            var progress = GetProgressAmount(ActiveRandQuests[i].Item1.Conditions, conditions);
            if (progress != -1)
                IncreaseQuestProgress(i, progress, true);
        }

        AutoCompleteQuests();
    }

    float GetProgressAmount(QuestCondition quest, QuestCondition conditions)
    {
        //skip gather quests since those use a separate method
        if (quest.GatherItem.HasValue) return -1;
        
        //if the condition is to catch a Pokemon (a pokemon is involved in the conditions, and the item is either not required or is a Poke Ball)
        if ((quest.PokemonId.HasValue || quest.PokemonType.HasValue) && (!quest.UseItem.HasValue || ModContent.GetModItem(quest.UseItem.Value) is BasePkballItem))
        {
            //check if pokemon and type are correct
            if ((!quest.PokemonId.HasValue || quest.PokemonId == conditions.PokemonId) &&
                (!quest.PokemonType.HasValue || quest.PokemonType == conditions.PokemonType) &&
                (!quest.UseItem.HasValue || quest.UseItem == conditions.UseItem))
                return 1;
        }
        //if the condition is to use another item
        else if (quest.UseItem.HasValue && quest.UseItem == conditions.UseItem)
        {
            //check if pokemon or type are required
            if ((!quest.PokemonId.HasValue || quest.PokemonId == conditions.PokemonId) &&
                (!quest.PokemonType.HasValue || quest.PokemonType == conditions.PokemonType))
                return 1;
        }

        return -1;
    }
    
    void IncreaseQuestProgress(int index, float amount = 1, bool isRandom = false) => SetQuestProgress(index, ActiveQuests[index].Item2 + amount, isRandom);

    void SetQuestProgress(int index, float amount, bool isRandom = false, bool announceCompletion = true)
    {
        var quest = isRandom ? ActiveRandQuests[index].Item1 : GetQuest(ActiveQuests[index].Item1);
        var previousAmount = isRandom ? ActiveRandQuests[index].Item2 : ActiveQuests[index].Item2;

        if (amount >= quest.Amount && previousAmount != -1) //don't do whole fanfare if it's already been activated previously
        {
            //set total to -1 (marks it as completed)
            amount = -1;

            //don't notify about unaccepted randoms since player might not know about them
            if (announceCompletion &&
                (!isRandom || AcceptedRandQuests [index]))
            {
                Main.NewText($"Quest Complete! \"{quest.Name}\"", Color.GreenYellow);
                TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/ls_catch_fanfare"));
            }
        }
        
        if (isRandom)
            ActiveRandQuests[index] = new Tuple<TerramonQuest, float>(ActiveRandQuests[index].Item1, amount);
        else
            ActiveQuests[index] = new Tuple<string, float>(ActiveQuests[index].Item1, amount);
    }
    
    //separate method for if the condition is to "get x amount of an item" since it checks player inventory
    public void TrackGatherProgress(bool announceCompletion = true)
    {
        for (int i = 0; i < ActiveQuests.Count; i++)
        {
            var quest = GetQuest(ActiveQuests[i].Item1);
            if (!quest.Conditions.GatherItem.HasValue) continue;

            var amount = GetGatherProgressAmount(quest.Conditions.GatherItem.Value, ActiveQuests[i].Item2);
            if (amount != -1)
                SetQuestProgress(i, amount, false, announceCompletion);
        }
        
        for (int i = 0; i < ActiveRandQuests.Count; i++)
        {
            if (!ActiveRandQuests[i].Item1.Conditions.GatherItem.HasValue) continue;

            var amount = GetGatherProgressAmount(ActiveRandQuests[i].Item1.Conditions.GatherItem.Value, ActiveRandQuests[i].Item2);
            if (amount != -1)
                SetQuestProgress(i, amount, true, announceCompletion);
        }

        AutoCompleteQuests();
    }

    int GetGatherProgressAmount(int itemId, float previousAmount)
    {
        var inventorySlot = Main.LocalPlayer.inventory.ToList().FindIndex(item => item.type == itemId);
        if (inventorySlot == -1)
            if (previousAmount == -1)
                return 0;
            else
                return -1;
        return Main.LocalPlayer.inventory[inventorySlot].stack;
    }

    void AutoCompleteQuests()
    {
        //automatic quest completion until this is done via gui
        //todo: remove this once gui is available
        for (int i = ActiveQuests.Count - 1; i >= 0; i--)
        {
            if (ActiveQuests[i].Item2 == -1)
                CompleteQuest(i);
        }
    }

    void CompleteQuest(int index)
    {
        var quest = GetQuest(ActiveQuests[index].Item1);
        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward("TerramonQuestComplete"), quest.RewardItemId, quest.RewardItemAmount);
        CompletedQuests.Add(quest.Key);

        //if the quest was to find items, take the items
        if (quest.Conditions.GatherItem.HasValue)
            for (int i = 0; i < quest.Amount; i++)
                Main.LocalPlayer.ConsumeItem(quest.Conditions.GatherItem.Value);

        bool newQuests = false;
        foreach (var q in Quests)
        {
            if (q.Requirements != null && q.Requirements.Contains(quest.Key))
                if (q.Requirements.All(r => CompletedQuests.Contains(r)))
                {
                    AddQuest(q.Key);
                    newQuests = true;
                }
        }
        if (newQuests)
            Main.NewText("New quests available.", Color.Goldenrod);

        ActiveQuests.RemoveAt(index);
    }

    void CompleteRandQuest(int index)
    {
        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward("TerramonQuestComplete"),
            ActiveRandQuests[index].Item1.RewardItemId, ActiveRandQuests[index].Item1.RewardItemAmount);
        CompletedRandQuests++;
        AcceptedRandQuests[index] = false;

        CycleRandQuest(index);
    }

    void CycleRandQuest(int index)
    {
        //TODO: add code for exchanging quest in this slot for another one
    }
    
    //TODO: whatever quest loading stuff and that
}
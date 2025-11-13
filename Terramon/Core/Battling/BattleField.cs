using Showdown.NET.Definitions;
using Terramon.Core.Battling.BattlePackets;
using Terramon.ID;

namespace Terramon.Core.Battling;
public sealed class BattleField
{
    public BattleSide A;
    public BattleSide B;
    public BattleWeather Weather;
    public FieldCondition Condition;

    public void ModifyBoosts(BoostModifierAction action)
    {
        for (int i = 0; i < 6; i++)
        {
            A[i].ModifyBoosts(action);
            B[i].ModifyBoosts(action);
        }
    }

    /// <summary>
    ///     Receives and handles <see cref="BattleAction"/>s, a basic structure containing information that must be replicated across all clients.
    ///     This method is called on all clients, but should only operate on this <see cref="BattleClient"/> instance.
    /// </summary>
    /// <param name="a">The received action.</param>
    /// <param name="r">The reader, used to read extra information depending on the <see cref="BattleAction.ID"/> of the action.</param>
    public void Receive(in BattleAction a, BinaryReader r)
    {
        switch (a.ID)
        {
            case BattleActionID.SetPokemonDetails:
                GetMon(r).Details = r.ReadPokemonDetails();
                break;
            case BattleActionID.SetPokemonSpecies:
                GetMon(r).Species = r.ReadUInt16();
                break;
            case BattleActionID.SetPokemonHP:
                GetMon(r).HPData = r.ReadPokemonHP();
                break;
            case BattleActionID.MoveAnimation:
                GetMon(r).PlayMoveAnimation(r.ReadUInt16());
                break;
            case BattleActionID.SwitchPokemon:
                GetMon(r).SetAsActive();
                break;
            case BattleActionID.ActionFail:
                break;
            case BattleActionID.SetPokemonStatus:
                ref var mon = ref GetMon(r);
                if (a.Flags[0]) // is cure status
                    mon.CureStatus();
                else
                    mon.Status = (NonVolatileStatus)r.ReadByte();
                break;
            case BattleActionID.SetPokemonBoost: // all three possibilities are so disparate that they should probably be their own separate things tbh :sob:
                if (a.Flags[1]) // is swap
                {
                    var pair = r.ReadPokemonIDs();
                    ref var src = ref GetMon(pair.Source);
                    ref var tgt = ref GetMon(pair.Target);
                    StatID stat = (StatID)r.ReadByte();
                    var srcValue = src.StatStages[stat];
                    var tgtValue = tgt.StatStages[stat];
                    src.SetStat(stat, tgtValue, true);
                    tgt.SetStat(stat, srcValue, true);
                }
                else if (a.Flags[2]) // is copy
                {
                    var pair = r.ReadPokemonIDs();
                    ref var src = ref GetMon(pair.Source);
                    ref var tgt = ref GetMon(pair.Target);
                    tgt.PackedStats = src.PackedStats;
                }
                else
                    GetMon(r).SetStat(r.ReadByte(), r.ReadSByte(), a.Flags[0]);
                break;
            case BattleActionID.AllPokemonBoost:
                if (a.Flags[0]) // is clear-all
                    ModifyBoosts(BoostModifierAction.ClearAll);
                else
                    GetMon(r).ModifyBoosts((BoostModifierAction)r.ReadByte());
                break;
            case BattleActionID.SetWeather:
                Weather = (BattleWeather)r.ReadByte();
                break;
            case BattleActionID.SetFieldCondition:
                Condition = (FieldCondition)r.ReadByte();
                break;
            case BattleActionID.SetSideCondition:
                if (a.Flags[0])
                {
                    (A.Condition, B.Condition) = (B.Condition, A.Condition);
                }
                else
                    this[r.ReadByte()].Condition = (SideCondition)r.ReadByte();
                break;
            case BattleActionID.SetPokemonVolatile:
                mon = ref GetMon(r);
                var vol = (VolatileEffect)r.ReadByte();
                if (a.Flags[0]) // is start
                    mon.Volatiles.Add(vol);
                else
                    mon.Volatiles.Remove(vol);
                // todo: certain volatiles have more effects than just being set as flags on the mon
                break;
            case BattleActionID.PokemonItem:
                mon = ref GetMon(r);
                if (a.Flags[0]) // is reveal
                    mon.HeldItem = r.ReadUInt16();
                else
                    mon.HeldItem = 0;
                break;
            case BattleActionID.PokemonAbility:
                mon = ref GetMon(r);
                if (a.Flags[0]) // is reveal
                    mon.Ability = (AbilityID)r.ReadUInt16();
                else
                    mon.AbilitySuppressed = true;
                break;
            case BattleActionID.PokemonTransformDetails:
                break;
        }
    }
    public void Receive(BinaryReader r)
    {
        while (true)
        {
            var actionID = (BattleActionID)r.ReadByte();
            if (actionID == BattleActionID.None)
                break;
            var flags = (BitsByte)r.ReadByte();
            var action = new BattleAction(actionID, flags);
            Receive(in action, r);
        }
    }

    public ref BattlePokemon GetMon(BinaryReader r) => ref GetMon(r.ReadPokemonID());
    public ref BattlePokemon GetMon(SimpleMon ID) => ref this[ID.Side][ID.Slot];

    public BattleSide this[int side]
    {
        get
        {
            if (side == 1)
                return A;
            else if (side == 2)
                return B;
            else
                return null;
        }
    }
}

public enum BattleWeather : byte
{
    None,
    RainDance,
    DesolateLand,
    PrimordialSea,
    SunnyDay,
    Sandstorm,
    Hail,
    Snowscape,
    DeltaStream,
    StormSurge,
    DesertedDunes,
}

public enum FieldCondition : byte
{
    None,
    ElectricTerrain,
    PsychicTerrain,
    GrassyTerrain,
    MistyTerrain,
    MudSport,
    WaterSport,
    Gravity,
    TrickRoom,
    AnfieldAtmosphere,
}

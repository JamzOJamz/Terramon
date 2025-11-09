namespace Terramon.Core.Battling;
public sealed class BattleField
{
    public BattleObserver Observer { get; }
    public BattleSide A;
    public BattleSide B;
    public BattleWeather Weather;
    public FieldCondition Condition;
    public BattleField()
    {
        Observer = new(this);
    }
    public void ModifyBoosts(BoostModifierAction action)
    {
        for (int i = 0; i < 6; i++)
        {
            A[i].ModifyBoosts(action);
            B[i].ModifyBoosts(action);
        }
    }
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

using System.Collections;

namespace Terramon.Core.Battling;
public sealed class BattleSide : IEnumerable<PokemonData>
{
    public BattleSide Opposite;
    private byte _activeSlot;
    public int TeamCount;
    public BattlePokemon[] Team = new BattlePokemon[6];
    public IBattleProvider Provider;
    public ShowdownRequest CurrentRequest;
    public SideCondition Condition;
    public ref BattlePokemon ActivePokemon => ref Team[_activeSlot];
    public void SetActivePokemon(byte slot)
    {
        _activeSlot = slot;
        Provider.SetActiveSlot(slot);
    }

    public BattleSide(IBattleProvider baseOn)
    {
        Provider = baseOn;
        var team = baseOn.GetBattleTeam();
        for (int i = 0; i < team.Length; i++)
        {
            Team[i] = new()
            {
                Side = this,
                Slot = (byte)i
            };

            var mon = team[i];
            if (mon is null)
                continue;
            TeamCount++;
            Team[i].Data = mon;
        }
    }

    public IEnumerator<PokemonData> GetEnumerator()
    {
        for (int i = 0; i < Team.Length; i++)
            yield return Team[i].Data;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public ref BattlePokemon this[int slot] => ref Team[slot];

    public static BattleSide LocalSide
    {
        get
        {
            var loc = BattleClient.LocalClient;
            var sd = loc.SideIndex;
            if (sd == 0)
                return null;
            return loc.Battle[sd];
        }
    }
}

public enum SideCondition : byte
{
    None,
    Mist,
    Spikes,
    ToxicSpikes,
    StealthRock,
    FirePledge,
    StickyWeb,
    LightScreen,
    Reflect,
    Tailwind,
    Safeguard,
    LuckyChant,
    GMaxCannonade,
    GMaxSteelsurge,
    GMaxVineLash,
    GMaxVolcalith,
    GMaxWildfire,
}

public enum ShowdownRequest : byte
{
    None,
    Any,
    Wait,
    ForcedSwitch,
}

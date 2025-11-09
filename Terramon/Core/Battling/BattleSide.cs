using Showdown.NET.Definitions;
using System.Collections;
using Terramon.Content.NPCs;
using Terramon.ID;

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
        if (Provider is TerramonPlayer plr)
            plr.ActiveSlot = _activeSlot;
    }

    public BattleSide(IBattleProvider baseOn)
    {
        var team = baseOn.GetBattleTeam();
        for (int i = 0; i < Team.Length; i++)
        {
            Team[i].Side = this;

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
    ForcedMove,
    ForcedSwitch,
}

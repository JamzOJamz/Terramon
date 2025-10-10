using Showdown.NET.Simulator;
using Terramon.Content.NPCs;

namespace Terramon.Core.Battling;

public class BattleInstance
{
    /// <summary>
    ///     In wild battles, holds the index of the <see cref="PokemonNPC" /> currently being battled.
    ///     For trainer or other battle types, this value is null.
    /// </summary>
    public int? WildNPCIndex { get; init; }

    /// <summary>
    ///     The index of the player who initiated the battle or the host.
    ///     In wild battles, this is always the local player.
    ///     In trainer battles, this is the host player, and the other player is Player2.
    /// </summary>
    public int Player1Index { get; init; }

    /// <summary>
    ///     The index of other player in the battle (only applicable in trainer battles).
    ///     Null in wild battles.
    /// </summary>
    public int? Player2Index { get; init; }
    
    public int TickCount { get; set; }

    public BattleStream BattleStream { get; set; }

    public bool ShouldStop { get; private set; }

    public void Update()
    {
        TickCount++;
    }

    public void Stop()
    {
        ShouldStop = true;
    }
}
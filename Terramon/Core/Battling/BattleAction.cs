using Showdown.NET.Protocol;
using Terramon.ID;

namespace Terramon.Core.Battling;

/// <summary>
/// Think <see cref="ProtocolElement"/> but smaller, only giving out what's necessary for local replication.
/// </summary>
public readonly record struct BattleAction(BattleActionID ID, BitsByte Flags);
public enum BattleActionID : byte
{
    None,
    /// <summary>
    ///     <para>
    ///         If the species change is only apparent (i. e. caused by Illusion), <c>Header[0]</c> is <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         <b>Structure:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 1. <see cref="SimpleMon"/>: ID of the target Pokémon.
    ///             </item>
    ///             <item>
    ///                 2. <see langword="ushort"/>: <see cref="NationalDexID"/> of the new Pokémon species.
    ///             </item>
    ///             <item>
    ///                 (If apparent change) 3. <see langword="byte"/>: Shinyness (first bit) and gender (next bits, directly castable to <see cref="Gender"/>). 
    ///             </item>
    ///             <item>
    ///                 (If real change) 3. <see langword="uint"/>: Eight bits for max health, eight bits for current health.
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <see cref="DetailsChangeElement"/> and <see cref="ReplaceElement"/>
    /// </summary>
    SetPokemonDetails,
    /// <summary>
    ///     <see cref="FormeChangeElement"/>
    /// </summary>
    SetPokemonSpecies,
    /// <summary>
    ///     <see cref="DamageElement"/>, <see cref="HealElement"/>, <see cref="SetHPElement"/> and <see cref="FaintElement"/> (when second flag is set)
    /// </summary>
    SetPokemonHP,
    /// <summary>
    ///     <see cref="MoveElement"/>
    /// </summary>
    MoveAnimation,
    /// <summary>
    ///     <see cref="SwitchElement"/> and <see cref="DragElement"/>
    /// </summary>
    SwitchPokemon,
    /// <summary>
    ///     <see cref="CantElement"/>, <see cref="FailElement"/>, <see cref="BlockElement"/>, <see cref="NoTargetElement"/>, <see cref="MissElement"/> and <see cref="ImmuneElement"/>
    /// </summary>
    ActionFail,
    /// <summary>
    ///     <see cref="PrepareElement"/> and <see cref="MustRechargeElement"/>
    /// </summary>
    PokemonWait,
    /// <summary>
    ///     <see cref="StatusElement"/>, <see cref="CureStatusElement"/> and <see cref="CureTeamElement"/>
    /// </summary>
    SetPokemonStatus,
    /// <summary>
    ///     <see cref="BoostElement"/>, <see cref="UnboostElement"/>, <see cref="SetBoostElement"/>, <see cref="SwapBoostElement"/>, and <see cref="CopyBoostElement"/>
    /// </summary>
    SetPokemonBoost,
    /// <summary>
    ///     <see cref="InvertBoostElement"/>, <see cref="ClearBoostElement"/>, <see cref="ClearAllBoostElement"/>, <see cref="ClearPositiveBoostElement"/>, and <see cref="ClearNegativeBoostElement"/>
    /// </summary>
    AllPokemonBoost,
    /// <summary>
    ///     <see cref="TurnElement"/>
    /// </summary>
    AdvanceTurn,
    /// <summary>
    ///     <see cref="WeatherElement"/>
    /// </summary>
    SetWeather,
    /// <summary>
    ///     <see cref="FieldStartElement"/> and <see cref="FieldEndElement"/>
    /// </summary>
    SetFieldCondition,
    /// <summary>
    ///     <see cref="SideStartElement"/>, <see cref="SideEndElement"/> and <see cref="SwapSideConditionsElement"/>
    /// </summary>
    SetSideCondition,
    /// <summary>
    ///     <see cref="StartVolatileElement"/> and <see cref="EndVolatileElement"/>
    /// </summary>
    SetPokemonVolatile,
    /// <summary>
    ///     <see cref="ItemElement"/> and <see cref="EndItemElement"/>
    /// </summary>
    PokemonItem,
    /// <summary>
    ///     <see cref="AbilityElement"/> and <see cref="EndAbilityElement"/>
    /// </summary>
    PokemonAbility,
    /// <summary>
    ///     <see cref="TransformElement"/>, <see cref="MegaElement"/>, <see cref="PrimalElement"/> and <see cref="BurstElement"/>
    /// </summary>
    PokemonTransformDetails
}

namespace Terramon.Core.Battling;
public enum ErrorSubtype : byte
{
    None,
    /// <summary>
    ///     <para>
    ///         Tried to move but current request state isn't <see cref="ShowdownRequest.Any"/>
    ///     </para>
    ///     `Can't move: You need a ${this.requestState} response`
    /// </summary>
    BadStateMove,
    /// <summary>
    ///     `Can't move: You sent more choices than unfainted Pokémon.`
    ///     `Can't switch: You sent more choices than unfainted Pokémon`
    /// </summary>
    ChoiceOverflow,
    /// <summary>
    ///     `Can't move: Your ${pokemon.name} doesn't have a move ${moveIndex + 1}`
    /// </summary>
    NoMove,
    /// <summary>
    ///     `Can't move: Your ${pokemon.name} doesn't have a move matching ${moveid}`
    /// </summary>
    MoveMustBe,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't use ${move.name} as a Z-move`
    /// </summary>
    BadZMove,
    /// <summary>
    ///     `Can't move: You can't Z-move more than once per battle`
    /// </summary>
    AlreadyZMoved,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't use ${move.name} as a Max Move`
    /// </summary>
    BadMaxMove,
    /// <summary>
    ///     `Can't move: ${move.name} needs a target`
    /// </summary>
    TargetNotSet,
    /// <summary>
    ///     `Can't move: Invalid target for ${move.name}`
    /// </summary>
    BadTarget,
    /// <summary>
    ///     `Can't move: You can't choose a target for ${move.name}`
    /// </summary>
    TargetSet,
    /// <summary>
    ///     `Can't move: ${pokemon.name}'s Fight button is known to be safe`
    /// </summary>
    FailFightTest,
    /// <summary>
    ///     `Can't move: ${pokemon.name}'s ${maxMove.name} is disabled`
    /// </summary>
    MoveDisabled,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't mega evolve`
    /// </summary>
    BadMega,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't mega evolve X`
    /// </summary>
    BadMegaX,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't mega evolve Y`
    /// </summary>
    BadMegaY,
    /// <summary>
    ///     `Can't move: You can only mega-evolve once per battle`
    /// </summary>
    AlreadyMega,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't ultra burst`
    /// </summary>
    BadUltraBurst,
    /// <summary>
    ///     `Can't move: You can only ultra burst once per battle`
    /// </summary>
    AlreadyUltraBurst,
    /// <summary>
    ///     `Can't move: Dynamaxing doesn't outside of Gen 8.`
    /// </summary>
    BadDynamaxGen,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't Dynamax now.`
    /// </summary>
    BadDynamax,
    /// <summary>
    ///     `Can't move: It's your partner's turn to Dynamax.`
    /// </summary>
    WaitDynamaxPartner,
    /// <summary>
    ///     `Can't move: You can only Dynamax once per battle.`
    /// </summary>
    AlreadyDynamaxed,
    /// <summary>
    ///     `Can't move: ${pokemon.name} can't Terastallize.`
    /// </summary>
    BadTerastallization,
    /// <summary>
    ///     `Can't move: You can only Terastallize once per battle.`
    /// </summary>
    AlreadyTerastallized,
    /// <summary>
    ///     `Can't move: You can only Terastallize in Gen 9.`
    /// </summary>
    BadTerastallizationGen,
    /// <summary>
    ///     <para>
    ///         Tried to switch but current request state isn't <see cref="ShowdownRequest.Any"/> or <see cref="ShowdownRequest.ForcedSwitch"/>
    ///     </para>
    ///     `Can't switch: You need a ${this.requestState} response`
    /// </summary>
    BadStateSwitch,
    /// <summary>
    ///     `Can't switch: You sent more switches than Pokémon that need to switch`
    /// </summary>
    SwitchOverflow,
    /// <summary>
    ///     `Can't switch: You need to select a Pokémon to switch in`
    /// </summary>
    NoSwitch,
    /// <summary>
    ///     `Can't switch: You do not have a Pokémon named "${slotText}" to switch to`
    /// </summary>
    BadSwitchName,
    /// <summary>
    ///     `Can't switch: You do not have a Pokémon in slot ${slot + 1} to switch to`
    /// </summary>
    BadSwitchSlot,
    /// <summary>
    ///     `Can't switch: You can't switch to an active Pokémon`
    /// </summary>
    AlreadyActive,
    /// <summary>
    ///     `Can't switch: The Pokémon in slot ${slot + 1} can only switch in once`
    ///     `Can't choose for Team Preview: The Pokémon in slot ${pos + 1} can only switch in once`
    /// </summary>
    AlreadySwitched,
    /// <summary>
    ///     `Can't switch: You have to pass to a fainted Pokémon`
    /// </summary>
    SwitchNotFainted,
    /// <summary>
    ///     `Can't switch: You can't switch to a fainted Pokémon`
    /// </summary>
    SwitchFainted,
    /// <summary>
    ///     `Can't switch: The active Pokémon is trapped`
    /// </summary>
    SwitchTrapped,
    /// <summary>
    ///     `Can't choose for Team Preview: You're not in a Team Preview phase`
    /// </summary>
    NotInTeamPreview,
    /// <summary>
    ///     `Can't choose for Team Preview: You do not have a Pokémon in slot ${pos + 1}`
    /// </summary>
    BadTeamPreviewSlot,
    /// <summary>
    ///     `Can't choose for Team Preview: ${result}`
    /// </summary>
    TeamPreviewViolatesRule,
    /// <summary>
    ///     `Can't shift: You do not have a Pokémon in slot ${index + 1}`
    /// </summary>
    BadShiftSlot,
    /// <summary>
    ///     `Can't shift: You can only shift during a move phase`
    /// </summary>
    BadStateShift,
    /// <summary>
    ///     `Can't shift: You can only shift to the center in triples`
    /// </summary>
    BadShiftFormat,
    /// <summary>
    ///     `Can't shift: You can only shift from the edge to the center`
    /// </summary>
    BadShift,
    /// <summary>
    ///     `Can't do anything: The game is over`
    /// </summary>
    GameOver,
    /// <summary>
    ///     `Can't do anything: It's not your turn`
    /// </summary>
    BadTurn,
    /// <summary>
    ///     `Can't undo: A trapping/disabling effect would cause undo to leak information`
    /// </summary>
    BadUndo,
    /// <summary>
    ///     `Can't make choices: You sent choices for ${choiceStrings.length} Pokémon, but this is a ${this.battle.gameType} game!`
    /// </summary>
    BadChoiceAmount,
    /// <summary>
    ///     `Conflicting arguments for "move": ${original}`
    /// </summary>
    BadMoveCommand,
    /// <summary>
    ///     `Unrecognized data after "shift": ${data}`
    /// </summary>
    BadShiftCommand,
    /// <summary>
    ///     `Unrecognized data after "pass": ${data}`
    /// </summary>
    BadPassCommand,
    /// <summary>
    ///     `Unrecognized choice: ${choiceString}`
    /// </summary>
    BadCommand,
    /// <summary>
    ///     `Can't pass: You need to switch in a Pokémon to replace ${pokemon.name}`
    /// </summary>
    PassNeedsSwitch,
    /// <summary>
    ///     `Can't pass: Your ${pokemon.name} must make a move (or switch)`
    /// </summary>
    PassNeedsAction,
    /// <summary>
    ///     `Can't pass: Not a move or switch request`
    /// </summary>
    BadStatePass,
}

public static class BattleErrorParser
{
    public static ErrorSubtype Parse(string message)
    {
        var parts = message.Split(' ', 4);
        var id = parts[1];
        var leading = parts[2];
        var body = parts[3];

        switch (id)
        {
            // Move-related errors
            case "move:":
                break;
            // Switch-related errors
            case "switch:":
                break;
            // Team Preview-related errors
            case "choose":
                break;
            // Shift-related errors
            case "shift:":
                break;
            // Meta errors
            case "do":
                break;
            // Undo leak error
            case "undo:":
                break;
            // Wrong game format error
            case "make":
                break;
            // Bad move command error
            case "arguments":
                break;
            // Bad shift and pass command errors
            case "data":
                break;
            // Bad command error
            case "choice:":
                break;
            // Pass-related errors
            case "pass:":
                break;
        }

        // throw new Exception("Weird error message: " + message);
        return ErrorSubtype.None;
    }
}

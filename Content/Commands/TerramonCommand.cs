namespace Terramon.Content.Commands;

public abstract class TerramonCommand : ModCommand
{
    protected bool Allowed;

    /// <summary>
    ///     Shorthand for #FF1919 (255, 25, 25)
    /// </summary>
    protected static Color ChatColorRed => new(255, 25, 25);

    /// <summary>
    ///     Shorthand for #FFF014 (255, 240, 20)
    /// </summary>
    public static Color ChatColorYellow => new(255, 240, 20);

    /// <summary>
    ///     Minimum argument count for the command to be valid
    /// </summary>
    protected virtual int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Allowed = true;
        if (args.Length >= MinimumArgumentCount) return;
        Allowed = false;
        caller.Reply($"Usage: {Usage}", ChatColorRed);
    }
}
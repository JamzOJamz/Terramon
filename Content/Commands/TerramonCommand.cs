namespace Terramon.Content.Commands;

public abstract class TerramonCommand : ModCommand
{
    protected bool Allowed;

    protected virtual int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Allowed = true;
        if (args.Length >= MinimumArgumentCount) return;
        Allowed = false;
        caller.Reply($"Usage: {Usage}", Color.Red);
    }
}
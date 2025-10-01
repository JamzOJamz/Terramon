using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class BattleCommand : DebugCommand
{
    private static BattleInstance _currentBattle;
    public override CommandType Type => CommandType.Chat;

    public override string Command => "battle";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Battle.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Battle.Usage");

    protected override int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var subcommand = args[0];
        switch (subcommand)
        {
            case "start":
                StartBattle(caller);
                break;
            case "end":
                EndBattle(caller);
                break;
            default:
                caller.Reply("""
                             Invalid subcommand. Use "start" or "end"
                             """, ChatColorRed);
                return;
        }
    }

    private static void StartBattle(CommandCaller caller)
    {
        // Check if there's already an active battle
        if (_currentBattle != null)
        {
            caller.Reply("""A battle is already active, use "/battle end" to stop it first""", ChatColorRed);
            return;
        }

        // Create new battle instance
        _currentBattle = new BattleInstance();

        // Start the battle on a separate thread
        Task.Run(async () => await RunBattleAsync(_currentBattle));

        caller.Reply("Battle started and running in background!", ChatColorYellow);
    }

    private static void EndBattle(CommandCaller caller)
    {
        if (_currentBattle == null)
        {
            caller.Reply("No active battle to end", ChatColorRed);
            return;
        }

        _currentBattle.Stop();
        _currentBattle = null;
        caller.Reply("Battle stopped!", ChatColorYellow);
    }

    private static async Task RunBattleAsync(BattleInstance battleInstance)
    {
        try
        {
            var battleStream = new BattleStream();
            battleInstance.BattleStream = battleStream;
            
            var player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<TerramonPlayer>();
            var packedTeam = modPlayer.GetPackedTeam();

            // Initialize battle
            battleStream.Write(ProtocolCodec.EncodeStartCommand(string.Empty)); // No format needed
            battleStream.Write(ProtocolCodec.EncodeSetPlayerCommand("p1", Main.LocalPlayer.name, packedTeam));
            battleStream.Write(ProtocolCodec.EncodeSetPlayerCommand("p2", "Green"));

            // Process battle outputs
            await foreach (var output in battleStream.ReadOutputsAsync())
            {
                if (battleInstance.ShouldStop)
                    break;
                
                var frame = ProtocolCodec.Parse(output);
                if (frame == null || frame.Elements == null) continue;
                Main.NewText($"Received message of type {frame.GetType()} from simulator");
                foreach (var element in frame.Elements)
                {
                    Main.NewText(element);
                }
            }
        }
        catch (Exception ex)
        {
            Main.NewText($"Battle encountered an error: {ex.Message}", ChatColorRed);
            battleInstance.Stop();
        }
        finally
        {
            battleInstance.BattleStream?.Dispose();
        }
    }
}

public class BattleInstance
{
    public BattleStream BattleStream { get; set; }
    public bool ShouldStop { get; private set; }

    public void Stop()
    {
        ShouldStop = true;
    }
}
using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using Terramon.Core.Battling;
using Terramon.ID;
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
            case "move":
                Move(caller, args);
                break;
            case "switch":
                Switch(caller, args);
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

    private static void Move(CommandCaller caller, string[] args)
    {
        if (_currentBattle is null)
        {
            caller.Reply("""There is no active battle. Use "/battle start" to start one first""", ChatColorRed);
            return;
        }

        if (args.Length == 1)
        {
            caller.Reply("""Must provide a valid MOVESPEC (move index or move name)""", ChatColorRed);
            return;
        }

        string moveSpec;

        if (int.TryParse(args[1], out int n))
        {
            if (n < 1 || n > 4)
            {
                caller.Reply("""You provided a move index, but it was out of bounds""", ChatColorRed);
                return;
            }
            moveSpec = n.ToString();
        }
        else
        {
            string possibleName = args[1].Replace(" ", "").Trim();
            if (!Enum.TryParse<MoveID>(possibleName, true, out _))
            {
                caller.Reply("""You provided a move name, but it wasn't recognized""", ChatColorRed);
                return;
            }
            moveSpec = possibleName;
        }

        BattleStream stream = _currentBattle.BattleStream;

        stream.Write(ProtocolCodec.EncodePlayerChoiceCommand(1, $"move {moveSpec}"));
        stream.Write(ProtocolCodec.EncodePlayerChoiceCommand(2, "default"));
    }

    private static void Switch(CommandCaller caller, string[] args)
    {
        if (_currentBattle is null)
        {
            caller.Reply("""There is no active battle. Use "/battle start" to start one first""", ChatColorRed);
            return;
        }

        if (args.Length == 1)
        {
            caller.Reply("""Must provide a valid SWITCHSPEC (Pokémon's party index, nickname or species)""", ChatColorRed);
            return;
        }

        string switchSpec = null;

        if (int.TryParse(args[1], out int n))
        {
            if (n < 1 || n > 6)
            {
                caller.Reply("""You provided a party index, but it was out of bounds""", ChatColorRed);
                return;
            }
            switchSpec = n.ToString();
        }
        else
        {
            string possibleName = args[1];
            foreach (var mon in Main.LocalPlayer.GetModPlayer<TerramonPlayer>().Party)
            {
                if (mon.Nickname == possibleName || mon.Schema.Identifier == possibleName)
                {
                    switchSpec = possibleName;
                    break;
                }
            }
            if (switchSpec is null)
            {
                caller.Reply("""You provided a Pokémon nickname or species name, but it wasn't found in your party""", ChatColorRed);
                return;
            }
        }

        BattleStream stream = _currentBattle.BattleStream;

        stream.Write(ProtocolCodec.EncodePlayerChoiceCommand(1, $"switch {switchSpec}"));
        stream.Write(ProtocolCodec.EncodePlayerChoiceCommand(2, "default"));
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
            battleStream.Write(ProtocolCodec.EncodeStartCommand(FormatID.Gen9CustomGame));
            battleStream.Write(ProtocolCodec.EncodeSetPlayerCommand(1, Main.LocalPlayer.name, packedTeam));
            battleStream.Write(ProtocolCodec.EncodeSetPlayerCommand(2, "Green"));
            battleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand(1, "team 123456"));
            battleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand(2, "team 123456"));

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
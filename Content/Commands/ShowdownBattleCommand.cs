using Terramon.Core.Battling.TurnBased;

namespace Terramon.Content.Commands
{
    public class ShowdownBattleCommand : DebugCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "battle";

        protected override int MinimumArgumentCount => 1;

        private static ShowdownBattle _battle;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            base.Action(caller, input, args);
            if (!Allowed) return;
            
            var action = args[0].ToLower();
            switch (action)
            {
                case "start":
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var service = await NodeShowdownService.StartNewAsync(true);
                            _battle = new ShowdownBattle(service);
                            var options = new BattleOptions
                            {
                                FormatId = FormatID.Gen1CustomBattle
                            };
                            _battle.Start(options);
                            caller.Reply("Battle started!", ChatColorYellow);
                        }
                        catch (Exception e)
                        {
                            caller.Reply("Failed to start service: " + e.Message, ChatColorRed);
                        }
                    });
                    break;
                }
                case "end":
                    _battle?.Dispose();
                    _battle = null;
                    caller.Reply("Battle ended!", ChatColorYellow);
                    break;
            }
        }

        public override void Unload()
        {
            if (_battle == null) return;
            _battle.Dispose();
            _battle = null;
        }
    }
}
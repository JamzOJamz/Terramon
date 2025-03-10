using Terramon.Content.Tiles.Banners;
using Terraria.Localization;

namespace Terramon.Content.Commands
{
    public class BannerDataCommand : DebugCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "bannerset";

        public override string Description => Language.GetTextValue("Mods.Terramon.Commands.BannerData.Description");

        public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.BannerData.Usage");

        protected override int MinimumArgumentCount => 1;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            base.Action(caller, input, args);
            if (!Allowed) return;

            var player = caller.Player;
            if (player.HeldItem.ModItem is not PokeBannerItem bannerItem)
            {
                caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.BannerData.NotHoldingBanner"), ChatColorRed);
                return;
            }

            if (!Enum.TryParse(args[0], true, out BannerTier result))
            {
                caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.BannerData.ParseErrorBannerTier"), ChatColorRed);
                return;
            }

            bannerItem.tier = result;
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.BannerData.Success", result), ChatColorYellow);
        }
    }
}

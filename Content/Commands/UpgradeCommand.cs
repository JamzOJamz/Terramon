using System.Reflection;
using Terramon.ID;
using Terraria.Localization;

namespace Terramon.Content.Commands;

/// <summary>
///     A <see cref="ModCommand" /> that transfers legacy Pokémon from the pre-release version of Terramon to the new
///     system.
/// </summary>
public class UpgradeCommand : TerramonCommand
{
    private FieldInfo _basePkballDataField;
    private Type _basePkballType;
    private FieldInfo _pkmnDataLevelField;
    private FieldInfo _pkmnDataNameField;
    private FieldInfo _pkmnDataNicknameField;
    private FieldInfo _pkmnDataShinyField;
    private Type _pkmnDataType;

    public override CommandType Type => CommandType.Chat;

    public override string Command => "upgrade";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Upgrade.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Upgrade.Usage");

    public override bool IsLoadingEnabled(Mod mod)
    {
        // Load only if legacy pre-release version of Terramon is also enabled.
        return ModLoader.HasMod("TerramonMod");
    }

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);

        // Makes sure the legacy types were found during the Load phase
        if (_basePkballType == null || _pkmnDataType == null)
        {
            caller.Reply(
                Language.GetTextValue("Mods.Terramon.Commands.Upgrade.TypeResolveError"),
                ChatColorRed);
            return;
        }

        // Iterates the inventory of the player for legacy Poké Ball items and upgrades them to the new system
        var player = caller.Player;
        var modPlayer = player.GetModPlayer<TerramonPlayer>();
        var transferCount = 0;
        for (var i = 0; i < 50; i++)
        {
            var item = player.inventory[i];
            var modItemType = item.ModItem?.GetType();
            if (modItemType == null || modItemType.BaseType != _basePkballType) continue;
            var pkball = (object)item.ModItem;
            var data = _basePkballDataField.GetValue(pkball);
            if (data == null) continue;
            var name = _pkmnDataNameField.GetValue(data)?.ToString()?[..^3];
            if (name == null) continue;
            var id = Terramon.DatabaseV2.Pokemon.Where(p => p.Value.Identifier == name).Select(p => p.Key)
                .FirstOrDefault();
            if (id == 0) continue;
            var nickname = _pkmnDataNicknameField.GetValue(data)?.ToString();
            var isShiny = (bool)_pkmnDataShinyField.GetValue(data)!;
            var level = (int)_pkmnDataLevelField.GetValue(data)!;
            var newData = PokemonData.Create(player, id, (byte)level);
            newData.Nickname = nickname;
            newData.IsShiny = isShiny;
            newData.Ball = GetBallIDFromItemName(modItemType.Name);
            if (modPlayer.TransferPokemonToPC(newData) == null) break;

            transferCount++;
            item.TurnToAir();
        }
        
        if (transferCount == 0)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Upgrade.NopUpgrade"), ChatColorRed);
            return;
        }
        
        caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Upgrade.Success", transferCount, player.name),
            ChatColorYellow);
    }

    private static BallID GetBallIDFromItemName(string name)
    {
        return name switch
        {
            "MasterBallItem" => BallID.MasterBall,
            "UltraBallItem" => BallID.UltraBall,
            "GreatBallItem" => BallID.GreatBall,
            "PokeBallItem" => BallID.PokeBall,
            "PremierBallItem" => BallID.PremierBall,
            _ => BallID.PokeBall
        };
    }

    public override void Load()
    {
        var oldMod = ModLoader.GetMod("TerramonMod");
        var oldModAssembly = oldMod.Code;
        _basePkballType = oldModAssembly.GetType("TerramonMod.Items.BasePkballItem");
        _basePkballDataField = _basePkballType!.GetField("data", BindingFlags.Public | BindingFlags.Instance);
        _pkmnDataType = oldModAssembly.GetType("TerramonMod.Pokemon.PkmnData");
        _pkmnDataNameField = _pkmnDataType!.GetField("pkmn", BindingFlags.Public | BindingFlags.Instance);
        _pkmnDataNicknameField = _pkmnDataType!.GetField("Nickname", BindingFlags.Public | BindingFlags.Instance);
        _pkmnDataShinyField = _pkmnDataType!.GetField("isShiny", BindingFlags.Public | BindingFlags.Instance);
        _pkmnDataLevelField = _pkmnDataType!.GetField("level", BindingFlags.Public | BindingFlags.Instance);
    }

    public override void Unload()
    {
        _basePkballType = null;
        _basePkballDataField = null;
        _pkmnDataType = null;
        _pkmnDataNameField = null;
        _pkmnDataNicknameField = null;
        _pkmnDataShinyField = null;
        _pkmnDataLevelField = null;
    }
}
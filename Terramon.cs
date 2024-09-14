using System.IO;
using EasyPacketsLib;
using Terramon.Content.GUI;

namespace Terramon;

public class Terramon : Mod
{
    /*
     * TODO:
     * This will be removed at a later date.
     * It exists because there are Pokémon in the DB that shouldn't be loaded as mod content (yet).
     */
    public const ushort MaxPokemonID = 151;

    /// <summary>
    ///     The maximum level a Pokémon can reach.
    /// </summary>
    public const ushort MaxPokemonLevel = 100;

    public static Terramon Instance => ModContent.GetInstance<Terramon>();

    public static DatabaseV2 DatabaseV2 { get; private set; }

    public static void ResetPartyUI(bool fullClear = false)
    {
        if (fullClear)
        {
            PartyDisplay.ClearAllSlots();
            InventoryParty.ClearAllSlots();
        }
        else
        {
            var partyData = TerramonPlayer.LocalPlayer.Party;
            PartyDisplay.UpdateAllSlots(partyData); // Update the party sidebar display
            InventoryParty.UpdateAllSlots(partyData); // Update the inventory party display
        }
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        EasyPacketDLL.HandlePacket(reader, whoAmI);
    }

    private void SetupCrossModCompatibility()
    {
        if (Main.dedServ) return;

        // Wikithis compatibility
        if (!ModLoader.TryGetMod("Wikithis", out var wikiThis)) return;
        wikiThis.Call(0, this, "https://terrariamods.wiki.gg/wiki/Terramon_Mod/{}");
        wikiThis.Call(3, this, ModContent.Request<Texture2D>("Terramon/icon_small"));
    }

    public override void Load()
    {
        // Load the database
        var dbStream = GetFileStream("Assets/Data/PokemonDB-min.json");
        DatabaseV2 = DatabaseV2.Parse(dbStream);

        // Register the mod in EasyPacketsLib
        EasyPacketDLL.RegisterMod(this);

        // Setup cross-mod compatibility
        SetupCrossModCompatibility();
    }

    public override void Unload()
    {
        DatabaseV2 = null;
        EasyPacketDLL.Unload();
    }
}
using EasyPacketsLib;
using Terramon.Content.GUI;
using Terramon.Content.Menus;
using Terramon.Core.Loaders.UILoading;

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

    static Terramon()
    {
        if (!Main.dedServ) MenuSocialWidget.Setup();
    }

    /// <summary>
    ///     The amount of Pokémon that have actually been loaded into the game.
    ///     This is the minimum of the amount of Pokémon in the database and <see cref="MaxPokemonID" />.
    /// </summary>
    public static int LoadedPokemonCount => Math.Min(MaxPokemonID, DatabaseV2.Pokemon.Count);

    public static Terramon Instance => ModContent.GetInstance<Terramon>();

    public static DatabaseV2 DatabaseV2 { get; private set; }

    /// <summary>
    ///     The amount of times the mod has been loaded by the player.
    ///     The only way for one to change or reset this is to edit or delete the file <c>TerramonLoadCount.dat</c> in the save
    ///     directory.
    /// </summary>
    private static uint TimesLoaded { get; set; }

    /// <summary>
    ///     Forces a full refresh of the party UI (<see cref="PartyDisplay" /> and <see cref="InventoryParty" />), updating all
    ///     slots.
    /// </summary>
    public static void RefreshPartyUI()
    {
        var partyData = TerramonPlayer.LocalPlayer.Party;
        PartyDisplay.UpdateAllSlots(partyData); // Update the party sidebar display
        InventoryParty.UpdateAllSlots(partyData); // Update the inventory party display
    }

    /// <summary>
    ///     Resets UI states for reuse with other players. Called when the player leaves the world in
    ///     <see cref="TerramonWorld.PreSaveAndQuit" />.
    /// </summary>
    public static void ResetUI()
    {
        PartyDisplay.ClearAllSlots();
        InventoryParty.ClearAllSlots();
        PCInterface.ResetToDefault();
        UILoader.GetUIState<HubUI>().ResetPokedex();
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

    private uint CheckLoadCount()
    {
        var datFilePath = Path.Combine(Main.SavePath, "TerramonLoadCount.dat");
    
        if (!File.Exists(datFilePath))
        {
            using var writer = new BinaryWriter(File.Open(datFilePath, FileMode.Create));
            writer.Write(1u);
            return 1;
        }

        uint result = 1;

        try
        {
            using var reader = new BinaryReader(File.Open(datFilePath, FileMode.Open));
            result = reader.ReadUInt32();
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException or ArgumentException or FormatException)
        {
            Logger.Warn($"Failed to read load count from file. Resetting to 1. Error: {ex.Message}");
            result = 1;
        }

        result++;
        using (var writer = new BinaryWriter(File.Open(datFilePath, FileMode.Create)))
        {
            writer.Write(result);
        }

        return result;
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

        // Don't run the rest of the method on servers
        if (Main.dedServ) return;

        // Check how many times the mod has been loaded
        TimesLoaded = CheckLoadCount();

        // Check if first ever time loading the mod, and if so, force switch to Terramon's mod menu
        if (TimesLoaded == 1)
            ModContent.GetInstance<TerramonMenu>().ForceSwitchToThis();
    }

    public override void Unload()
    {
        DatabaseV2 = null;
        EasyPacketDLL.Unload();
    }
}
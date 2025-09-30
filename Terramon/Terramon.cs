using EasyPacketsLib;
using Terramon.Content.GUI;
using Terramon.Content.Menus;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;

namespace Terramon;

public class Terramon : Mod
{
    /*
     * TODO:
     * This will be removed at a later date.
     * It exists because there are Pokémon in the DB that shouldn't be loaded as mod content (yet).
     */
    public const ushort MaxPokemonIDToLoad = 491;

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
    ///     This is calculated once after loading the database.
    /// </summary>
    public static int LoadedPokemonCount { get; private set; }

    /// <summary>
    ///     The highest Pokémon ID loaded into the game.
    ///     This is calculated once after loading the database.
    /// </summary>
    public static int HighestPokemonID { get; private set; }

    public static Terramon Instance => ModContent.GetInstance<Terramon>();

    public static DatabaseV2 DatabaseV2 { get; private set; }

    /// <summary>
    ///     The amount of times the mod has been loaded by the player.
    ///     The only way for one to change or reset this is to edit or delete the file <c>TerramonLoadCount.dat</c> in the save
    ///     directory.
    /// </summary>
    public static uint TimesLoaded { get; private set; }

    /// <summary>
    ///     Calculates and caches the loaded Pokémon count and highest ID.
    ///     Called once after the database is loaded.
    /// </summary>
    private static void CalculatePokemonMetrics()
    {
        if (DatabaseV2?.Pokemon == null)
        {
            LoadedPokemonCount = 0;
            HighestPokemonID = 0;
            return;
        }

        // Calculate loaded count
        LoadedPokemonCount = Math.Min(MaxPokemonIDToLoad, DatabaseV2.Pokemon.Count);

        // Calculate the highest ID (highest ID that is <= MaxPokemonIDToLoad)
        HighestPokemonID = DatabaseV2.Pokemon.Keys!
            .Where(id => id <= MaxPokemonIDToLoad)
            .Max();
    }

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
            Logger.Warn($"Failed to read count from file! Error: {ex.Message}");
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
        // Load items, then entities
        AddContent<TerramonItemLoader>();
        AddContent<PokemonEntityLoader>();

        // Load the database
        var dbStream = GetFileStream("Assets/Data/PokemonDB-min.json");
        DatabaseV2 = DatabaseV2.Parse(dbStream);
        
        Logger.Debug($"PKMN LOADED COUNT DEBUGGGG {DatabaseV2.Pokemon.Count}");

        // Calculate and cache Pokémon metrics after loading the database
        CalculatePokemonMetrics();

        // Register the mod in EasyPacketsLib
        EasyPacketDLL.RegisterMod(this);

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
        LoadedPokemonCount = 0;
        HighestPokemonID = 0;
        EasyPacketDLL.Unload();
    }
}
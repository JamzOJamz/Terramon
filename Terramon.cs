using EasyPacketsLib;
using Terramon.Content.GUI;
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
    ///     Whether this is the first time the mod has been loaded on the player's system, ever.
    ///     The only way for one to reset this is to delete the file <c>TerramonHasLoadedBefore.dat</c> in the save directory.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static bool IsFirstTimeLoad { get; private set; }

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

    private static bool CheckFirstTimeLoad()
    {
        var datFilePath = Path.Combine(Main.SavePath, "TerramonHasLoadedBefore.dat");
        if (File.Exists(datFilePath)) return false;

        File.Create(datFilePath).Close();
        return true;
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

        // Check if first ever time loading the mod
        IsFirstTimeLoad = CheckFirstTimeLoad();
    }

    public override void Unload()
    {
        DatabaseV2 = null;
        EasyPacketDLL.Unload();
    }
}
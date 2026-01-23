using Terraria.Audio;

namespace Terramon.Helpers;

public static class TerramonSoundID
{
    private const string CryPath = "Terramon/Sounds/Cries/";
    public static readonly SoundStyle PkballConsume = new("Terramon/Sounds/pkball_consume") { Volume = 0.35f };
    public static readonly SoundStyle PkballThrow = new("Terramon/Sounds/pkball_throw") { Volume = 0.8f };
    public static readonly SoundStyle PkballBounce = new("Terramon/Sounds/pkball_bounce") { Volume = 0.75f };
    public static readonly SoundStyle PkballCatchPla = new("Terramon/Sounds/pkball_catch_pla") { Volume = 0.75f };
    public static readonly SoundStyle PkmnRecall = new("Terramon/Sounds/pkmn_recall") { Volume = 0.35f };
    public static readonly SoundStyle ButtonSmm = new("Terramon/Sounds/button_smm") { Pitch = 0.6f, Volume = 0.2925f };
    public static readonly SoundStyle ButtonLocked = new("Terramon/Sounds/button_locked") { Volume = 0.25f };
    public static readonly SoundStyle CatchClick = new("Terramon/Sounds/ls_catch_click");
    public static readonly SoundStyle CatchFail = new("Terramon/Sounds/ls_catch_fail");
    public static readonly SnVariants CatchWobble = new("Terramon/Sounds/ls_catch_wobble", numVariants: 3);
    public static readonly SoundStyle PCOn = new("Terramon/Sounds/ls_pc_on") { Volume = 0.54f };
    public static readonly SoundStyle PCOff = new("Terramon/Sounds/ls_pc_off") { Volume = 0.54f };
    public static readonly SoundStyle DexOpen = new("Terramon/Sounds/dex_open") { Volume = 0.48f };
    public static readonly SoundStyle DexClose = new("Terramon/Sounds/dex_close") { Volume = 0.48f };
    public static readonly SoundStyle DexPageUp = new("Terramon/Sounds/dex_pageup") { Volume = 0.325f };

    public static readonly SoundStyle BattlePing = new("Terramon/Sounds/battle_tb_ping")
        { Volume = 0.3f, MaxInstances = 0 };

    public static readonly SoundStyle BattlePingEmpty = new("Terramon/Sounds/battle_tb_empty")
        { Volume = 0.3f, MaxInstances = 0 };

    public static readonly SoundStyle BattleStart = new("Terramon/Sounds/battle_tb_start") { Volume = 0.3f };
    public static readonly SoundStyle BattleDecide = new("Terramon/Sounds/battle_decide") { Volume = 0.3f };
    public static readonly SoundStyle BattleCancel = new("Terramon/Sounds/battle_cancel") { Volume = 0.3f };
    public static readonly SoundStyle BattleRun = new("Terramon/Sounds/battle_run") { Volume = 0.3f };

    public static readonly SoundStyle HitNormalDamage = new("Terramon/Sounds/hit_normal_damage")
        { Volume = 0.165f, PitchVariance = 0.12f };

    public static readonly SoundStyle RealtimeEXPGain = new("Terramon/Sounds/realtime_exp_gain")
        { Volume = 0.5f, PitchRange = (-0.1f, 0.1f) };

    public static SoundStyle GetCry(ushort id, float volume = 0.15f) =>
        Terramon.DatabaseV2.GetPokemon(id).GetCry(volume);

    public static SoundStyle GetCry(this DatabaseV2.PokemonSchema schema, float volume = 0.15f) =>
        new(CryPath + schema.Identifier) { Volume = volume };

    public static SoundStyle GetCry(this PokemonData data, float volume = 0.15f) =>
        new(CryPath + data.InternalName) { Volume = volume };
}

public readonly record struct SnVariants(SoundStyle Child)
{
    public SnVariants(string path, int numVariants) : this(Child: new SoundStyle(path, numVariants))
    {
    }

    public SoundStyle this[int i] => Child with { SelectedVariant = i };
}
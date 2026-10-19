using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using Terramon.Content.Configs;
using Terramon.Core.Loaders;
using Terramon.Core.NPCComponents;
using Terramon.Core.Systems;
using Terramon.Core.Systems.FancySpawns;
using Terramon.ID;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to adjust the spawn location and rate of an NPC.
/// </summary>
public class NPCSpawnController : NPCComponent
{
    /// <summary>
    ///     A constant multiplier for balancing the spawn rates of Pokémon to vanilla NPCs.
    ///     Best value determined through testing, may need to be adjusted.
    /// </summary>
    private const float ConstantSpawnMultiplier = 1.25f; // Seems to work well
    public static JObject Macros { get; private set; }

    // See https://terrariamods.wiki.gg/wiki/Terramon_Mod/Pok%C3%A9mon#Spawning
    static NPCSpawnController()
    {
        MonoModHooks.Add(typeof(NPCLoader).GetMethod(nameof(NPCLoader.FinishSetup), BindingFlags.Static | BindingFlags.NonPublic),
            HookFinishSetup);
    }

    public override void Load()
    {
        base.Load();
        using var stream = Mod.GetFileStream("Assets/Data/Macros.json");
        using var reader = new StreamReader(stream);
        var tempMacros = JObject.Parse(reader.ReadToEnd());
        Macros = [];
        foreach (var kind in tempMacros)
        {
            if (kind.Key == "Consts")
            {
                Macros.Add(kind.Key, kind.Value);
                continue;
            }
            foreach (var macro in (JObject)kind.Value)
            {
                Flatten(macro.Value, GetFlattener(kind.Key));
                Macros.Add(macro.Key, macro.Value);
            }
        }
    }

    public override void SetDefaults(NPC npc)
    {
        if (!CacheInstances || !Enabled || Instances.ContainsKey(npc.type)) return;
        if (Rules == null || Rules.Count == 0) return;
        for (int i = Rules.Count - 1; i >= 0; i--)
        {
            var rule = (JObject)Rules[i];
            var export = false;
            if (rule.ContainsKey("FromTreeShake"))
            {
                TreeDropsGlobalTile.TreeShakeRules.Add(rule);
                export = true;
            }
            if (rule.ContainsKey("FromProjectile"))
            {
                ProjectileSpawns.ProjectileKillRules.Add(rule);
                export = true;
            }
            if (rule.ContainsKey("FromTile"))
            {

            }
            if (export)
            {
                rule.Add("Type", npc.type);
                Rules.RemoveAt(i);
            }
            foreach (var condition in rule)
            {
                Flatten(condition.Value, GetFlattener(condition.Key));
            }
        }
        Instances.Add(npc.type, this);
    }
    private static Func<string, double> GetFlattener(string cond)
    {
        return cond switch
        {
            "GameMode" => (s) => ((double?)typeof(GameModeID).GetField(s)?.GetRawConstantValue()) ?? throw new InvalidDataException(),
            "Time" => (s) => TimeOnly.Parse(s).Ticks / (double)TimeSpan.TicksPerHour,
            "Invasion" => (s) => ((double?)typeof(InvasionID).GetField(s)?.GetRawConstantValue()) ?? throw new InvalidDataException(),
            "MoonStyle" => (s) => s switch
            {
                "Normal" => 0,
                "Yellow" => 1,
                "Ringed" => 2,
                "Mythril" => 3,
                "BrightBlue" => 4,
                "Green" => 5,
                "Pink" => 6,
                "Orange" => 7,
                "Purple" => 8,
                _ => throw new InvalidDataException(s)
            },
            "MoonPhase" => (s) => (int)Enum.Parse<MoonPhase>(s),
            "WeekDay" => (s) => (int)Enum.Parse<DayOfWeek>(s),
            "Month" => (s) => Array.IndexOf(DateTimeFormatInfo.InvariantInfo.MonthNames, s),
            "FromTreeShake" => (s) => (int)Enum.Parse<TreeTypes>(s),
            "FromProjectile" => (s) => ProjectileID.Search.GetId(s),
            _ => null
        };
    }
    private static void Flatten(JToken j, Func<string, double> flattener = null)
    {
        if (j.Type == JTokenType.String)
        {
            var v = (JValue)j;
            var s = v.Value<string>();
            if (Macros.TryGetValue("Consts", out var consts))
            {
                if (((JObject)consts).TryGetValue(s, out var num))
                    v.Value = ((JValue)num).Value;
            }
            if (flattener != null)
                v.Value = flattener(s);
        }
        else if (j.Type == JTokenType.Object)
        {
            var o = (JObject)j;
            if (!o.TryGetValue("Args", out var args) || args.Type != JTokenType.Array)
                return;
            foreach (var item in args)
                Flatten(item, flattener);
        }
    }

    /// <summary>
    ///     Natural spawn rules for the NPC. They will all be checked, and will add to its spawn weight if its conditions are met.
    /// </summary>
    public JArray Rules;

    protected override bool CacheInstances => true;

    private static void HookFinishSetup(Action orig)
    {
        orig();

        var hookList = NPCLoader.HookEditSpawnPool;
        var hookGlobals = hookList.hookGlobals;

        // Finds this type (NPCSpawnController) in the array and move it to the end to ensure it runs last
        for (var i = 0; i < hookGlobals.Length; i++)
        {
            if (hookGlobals[i].GetType() != typeof(NPCSpawnController)) continue;
            MoveToEnd(hookGlobals, i);
            break;
        }

        return;

        static void MoveToEnd<T>(IList<T> array, int index)
        {
            if (index < 0 || index >= array.Count) return;

            var temp = array[index];
            for (var i = index; i < array.Count - 1; i++) array[i] = array[i + 1];
            array[^1] = temp;
        }
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        var gameplayConfig = GameplayConfig.Instance;

        var regularSpawnRateMultiplier = gameplayConfig.NonPokemonSpawnRateMultiplier;
        if (regularSpawnRateMultiplier != 1f)
            // Iterate through all the NPCs in the pool and apply the spawn rate multiplier directly
            foreach (var key in pool.Keys.ToList())
                pool[key] *= regularSpawnRateMultiplier;

        var spawnRateMultiplier = gameplayConfig.PokemonSpawnRateMultiplier;
        if (spawnRateMultiplier == 0) return;

        var rate = NPC.spawnRate;

        var typesAdded = new HashSet<int>();
        foreach (var (type, component) in Instances)
        {
            if (!component.Enabled) continue;
            var rules = ((NPCSpawnController)component).Rules;
            foreach (var ruleToken in rules)
            {
                var rule = (JObject)ruleToken;
                if (Evaluate(rule, in spawnInfo))
                {
                    if (typesAdded.Add(type))
                        pool[type] = default;
                    pool[type] += (float)rule.GetValue("Chance");
                }
            }
        }

        // Normalize the spawn pool
        var totalTypesAdded = typesAdded.Count;
        foreach (var type in typesAdded)
            pool[type] = pool[type] / totalTypesAdded * spawnRateMultiplier;
    }

    private static bool Evaluate(JObject rule, in NPCSpawnInfo spawnInfo)
    {
        var p = spawnInfo.Player;
        var c = p.Center;
        var isBool = false;
        foreach (var condition in rule)
        {
            var key = condition.Key;
            // Boolean
            switch (key)
            {
                // World
                case "Event":
                    isBool = true;
                    if (!EvaluateBoolean((j) =>
                    {
                        if (j.Type != JTokenType.String)
                            throw new InvalidDataException();
                        var ev = (string)j;
                        return ev switch
                        {
                            "Party" => BirthdayParty.PartyIsUp,
                            "LanternNight" => LanternNight.LanternsUp,
                            "Rain" => Main.raining,
                            "Sandstorm" => Sandstorm.Happening,
                            "WindyDay" => Main.IsItAHappyWindyDay,
                            "Thunderstorm" => Main.IsItStorming,
                            "BloodMoon" => Main.bloodMoon,
                            "SlimeRain" => Main.slimeRain,
                            "OldOnesArmy" => DD2Event.Ongoing,
                            "SolarEclipse" => Main.eclipse,
                            "PumpkinMoon" => Main.pumpkinMoon,
                            "FrostMoon" => Main.snowMoon,
                            "LunarEvent" => NPC.LunarApocalypseIsUp,
                            "Christmas" => Main.xMas,
                            "Halloween" => Main.halloween,
                            _ => throw new UnreachableException(ev)
                        };
                    }, condition.Value))
                        return false;
                    break;
                case "Downed":
                    isBool = true;
                    if (!EvaluateBoolean((j) =>
                    {
                        int npc;
                        if (j.Type == JTokenType.Integer)
                            npc = (int)j;
                        else if (j.Type != JTokenType.String)
                            throw new InvalidDataException();
                        else if (!NPCID.Search.TryGetId((string)j, out var np))
                            return false;
                        else
                            npc = np;
                        if (npc >= NPCID.Count) // add a way for other mods to plug their own downed impls somehow
                            throw new InvalidDataException("Modded downed flags are currently not supported!");
                        return npc switch
                        {
                            NPCID.LunarTowerStardust => NPC.downedTowerStardust,
                            NPCID.LunarTowerNebula => NPC.downedTowerNebula,
                            NPCID.LunarTowerVortex => NPC.downedTowerVortex,
                            NPCID.LunarTowerSolar => NPC.downedTowerSolar,
                            NPCID.MoonLordCore => NPC.downedMoonlord,
                            NPCID.CultistBoss => NPC.downedAncientCultist,
                            NPCID.SantaNK1 => NPC.downedChristmasSantank,
                            NPCID.Everscream => NPC.downedChristmasTree,
                            NPCID.IceQueen => NPC.downedChristmasIceQueen,
                            NPCID.Pumpking => NPC.downedHalloweenKing,
                            NPCID.MourningWood => NPC.downedHalloweenTree,
                            NPCID.DukeFishron => NPC.downedFishron,
                            NPCID.Deerclops => NPC.downedDeerclops,
                            NPCID.QueenSlimeBoss => NPC.downedQueenSlime,
                            NPCID.Spazmatism or NPCID.Retinazer => NPC.downedMechBoss2,
                            NPCID.TheDestroyer => NPC.downedMechBoss1,
                            NPCID.HallowBoss => NPC.downedEmpressOfLight,
                            NPCID.Golem => NPC.downedGolemBoss,
                            NPCID.Clown => NPC.downedClown,
                            NPCID.EyeofCthulhu => NPC.downedBoss2,
                            NPCID.BrainofCthulhu or NPCID.EaterofWorldsHead => NPC.downedBoss2,
                            NPCID.Plantera => NPC.downedPlantBoss,
                            NPCID.KingSlime => NPC.downedSlimeKing,
                            NPCID.SkeletronPrime => NPC.downedMechBoss3,
                            NPCID.SkeletronHead => NPC.downedBoss3,
                            NPCID.QueenBee => NPC.downedQueenBee,
                            NPCID.WallofFlesh => Main.hardMode,
                            _ => throw new UnreachableException(j.ToString())
                        };
                    }, condition.Value))
                        return false;
                    break;
                // Player
                case "Zone":
                    isBool = true;
                    if (!EvaluateBoolean((j) =>
                    {
                        if (j.Type != JTokenType.String)
                            throw new InvalidDataException();
                        var zone = (string)j;
                        var idx = zone.IndexOf('/');
                        if (idx != -1)
                        {
                            var modName = zone.AsSpan(0, idx);
                            var biomeName = zone.AsSpan(idx + 1);
                            if (!ModLoader.TryGetMod(modName.ToString(), out var mod))
                                return false;
                            if (!mod.TryFind<ModBiome>(biomeName.ToString(), out var biome))
                                throw new InvalidDataException($"Unable to find biome '{biomeName}' inside '{modName}'!");
                            return p.InModBiome(biome);
                        }
                        var prop = typeof(Player).GetProperty("Zone" + zone);
                        if (prop is null || prop.PropertyType != typeof(bool))
                            throw new InvalidDataException();
                        return (bool)prop.GetValue(p, null);
                    }, condition.Value))
                        return false;
                    break;
            }
            if (isBool)
                continue;
            // Numerical
            // argument to compare condition.Value against
            double a = key switch
            {
                // World
                "GameMode" => Main.GameModeInfo.Id,
                "Time" => Utils.GetDayTimeAs24FloatStartingFromMidnight(),
                "Invasion" => Main.invasionType,
                "WindSpeed" => Main.windSpeedCurrent,
                "MoonStyle" => Main.moonType,
                "MoonPhase" => Main.moonPhase,
                "Starfall" => Star.starfallBoost,
                "WeekDay" => (double)DateTime.Today.DayOfWeek,
                "MonthDay" => DateTime.Today.Day,
                "YearDay" => DateTime.Today.DayOfYear,
                "Month" => DateTime.Today.Month,
                "Year" => DateTime.Today.Year,
                // Player
                "AbsoluteX" => c.X,
                "AbsoluteY" => c.Y,
                "RelativeX" => Utils.GetLerpValue(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles, c.X / 16f, true),
                "RelativeY" => Utils.GetLerpValue(Main.offLimitBorderTiles, Main.maxTilesY - Main.offLimitBorderTiles, c.Y / 16f, true),
                "Money" => Utils.CoinsCount(out _, p.inventory),
                "Luck" => p.luck,
                _ => throw new UnreachableException(key)
            };
            if (!EvaluateNumeric(a, condition.Value, out _))
                return false;
        }
        return true;
    }
    private static bool EvaluateBoolean(Func<JToken, bool> deserializer, JToken j)
    {
        if (j.Type is JTokenType.String)
            return deserializer(j);
        else if (j.Type is JTokenType.Object)
        {
            var jo = (JObject)j;
            if (!jo.TryGetValue("Op", out var opToken) || opToken.Type != JTokenType.String ||
                !jo.TryGetValue("Args", out var args) || args.Type != JTokenType.Array)
                throw new InvalidDataException();
            switch ((string)opToken)
            {
                case "Or":
                    foreach (var subCondition in args)
                        if (EvaluateBoolean(deserializer, subCondition))
                            return true;
                    return false;
                case "And":
                    foreach (var subCondition in args)
                        if (!EvaluateBoolean(deserializer, subCondition))
                            return false;
                    return true;
                case "Not":
                    if (args.Count() != 1)
                        throw new InvalidDataException();
                    return !EvaluateBoolean(deserializer, args[0]);
            }
        }
        throw new UnreachableException(j.ToString());
    }
    internal static bool EvaluateNumeric<TSelf>(TSelf n, JToken j, out TSelf result) where TSelf : INumber<TSelf>
    {
        result = default;
        if (j.Type is JTokenType.Integer or JTokenType.Float)
            return n == j.Value<TSelf>();
        else if (j.Type is JTokenType.String)
        {
            var s = (string)j;
            if (Macros.TryGetValue(s, out var macro))
                return EvaluateNumeric(n, macro, out result);
        }
        else if (j.Type is JTokenType.Object)
        {
            var jo = (JObject)j;
            if (!jo.TryGetValue("Op", out var opToken) || opToken.Type != JTokenType.String ||
                !jo.TryGetValue("Args", out var args) || args.Type != JTokenType.Array)
                throw new InvalidDataException(jo.ToString());
            switch ((string)opToken)
            {
                case "Or":
                    foreach (var subCondition in args)
                        if (EvaluateNumeric(n, subCondition, out _))
                            return true;
                    return false;
                case "And":
                    foreach (var subCondition in args)
                        if (!EvaluateNumeric(n, subCondition, out _))
                            return false;
                    return true;
                case "Not":
                    if (args.Count() != 1)
                        throw new InvalidDataException();
                    return !EvaluateNumeric(n, args[0], out _);
                case "Between":
                    if (args.Count() != 2)
                        throw new InvalidDataException();
                    EvaluateNumeric(n, args[0], out var lower);
                    EvaluateNumeric(n, args[1], out var upper);
                    //                    wraparound case                   normal range
                    return lower > upper ? (n >= lower || n < upper) : (n >= lower && n < upper);
                case "GreaterThan":
                    if (args.Count() != 1)
                        throw new InvalidDataException();
                    EvaluateNumeric(n, args[0], out var gt);
                    return n > gt;
                case "LessThan":
                    if (args.Count() != 1)
                        throw new InvalidDataException();
                    EvaluateNumeric(n, args[0], out var lt);
                    return n < lt;
            }
        }
        throw new UnreachableException(j.ToString());
    }
}

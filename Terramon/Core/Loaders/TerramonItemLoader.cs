using System.Reflection;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Tiles.Interactive;
using Terramon.Content.Tiles.MusicBoxes;
using Terramon.Content.Tiles.Paintings;

namespace Terramon.Core.Loaders;

public enum TerramonItemGroup
{
    Apricorns,
    PokeBalls,
    Recovery,
    EvolutionaryItems,
    Vitamins,
    HeldItems,
    KeyItems,
    Interactive,
    MusicBoxes,
    PokeBallMinis,
    Vanity,
    Banners,
    Uncategorized
}

internal sealed class TerramonItemRegistration : ModSystem
{
    public override void Load()
    {
        // Add Apricorns
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Apricorns)
            .AddAllOfType<ApricornItem>();

        // Add Poké Balls
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.PokeBalls)
            .Add<PokeBallItem>()
            .Add<GreatBallItem>()
            .Add<UltraBallItem>()
            .Add<MasterBallItem>()
            .Add<PremierBallItem>()
            .Add<CherishBallItem>()
            .Add<AetherBallItem>();

        // Add recovery items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Recovery)
            // Potions
            .Add<Potion>()
            .Add<SuperPotion>()
            .Add<HyperPotion>()
            .Add<MaxPotion>()
            // Revives
            .Add<Revive>()
            .Add<MaxRevive>();

        // Add evolutionary items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.EvolutionaryItems)
            // Evolution stones
            .Add<FireStone>()
            .Add<WaterStone>()
            .Add<ThunderStone>()
            .Add<LeafStone>()
            .Add<MoonStone>()
            .Add<DuskStone>()
            .Add<IceStone>()
            // Misc evolutionary items
            .Add<LinkingCord>();

        // Add vitamins
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Vitamins)
            .Add<RareCandy>()
            .AddAllOfType<ExpCandy>();

        // Add held items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.HeldItems)
            .AddAllOfType<HeldItem>();

        // Add key items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.KeyItems)
            .AddAllOfType<KeyItem>();

        // Add interactive items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Interactive)
            .AddAllOfType<PCItem>();

        // Add music boxes
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.MusicBoxes)
            .AddAllOfType<MusicItem>();

        // Add Poké Ball minis
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.PokeBallMinis)
            .Add<PokeBallMiniItem>()
            .Add<GreatBallMiniItem>()
            .Add<UltraBallMiniItem>()
            .Add<MasterBallMiniItem>()
            .Add<PremierBallMiniItem>()
            .Add<CherishBallMiniItem>()
            .Add<AetherBallMiniItem>();

        // Add vanity items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Vanity)
            // Trainer vanity
            .Add<TrainerCap>()
            .Add<TrainerTorso>()
            .Add<TrainerLegs>();

        // Register banner group (populated in PokemonEntityLoader)
        TerramonItemRegistry.RegisterGroup(TerramonItemGroup.Banners);

        // Add uncategorized items
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.Uncategorized)
            .Add<ErikaPaintingItem>()
            .Add<ShimmerStateDrive>();
    }
}

[Autoload(false)]
public class TerramonItemLoader : ModSystem
{
    public override void OnModLoad()
    {
        foreach (var entry in TerramonItemRegistry.GetSortedItems())
        {
            try
            {
                var item = entry.IsInstance ? entry.Instance! : (ModItem)Activator.CreateInstance(entry.ItemType!)!;
                Mod.AddContent(item);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Failed to load item '{entry.ItemType?.FullName}': {ex}");
            }
        }
    }
}

public static class TerramonItemRegistry
{
    private static readonly Dictionary<string, GroupData> Groups = new();

    public static GroupBuilder Group(TerramonItemGroup group) => Group(group.ToString());

    public static GroupBuilder Group(string groupName)
    {
        if (!Groups.TryGetValue(groupName, out var group))
            throw new Exception($"Group '{groupName}' has not been registered.");

        return new GroupBuilder(group);
    }

    internal static GroupBuilder RegisterGroup(TerramonItemGroup group, int? explicitOrder = null) =>
        RegisterGroup(group.ToString(), explicitOrder);

    public static GroupBuilder RegisterGroup(string groupName, int? explicitOrder = null)
    {
        if (!Groups.TryGetValue(groupName, out var group))
        {
            group = new GroupData();
            Groups[groupName] = group;
        }

        if (explicitOrder.HasValue)
        {
            group.Order = explicitOrder.Value;
        }
        else
        {
            // Default behaviour is to auto-assign to end of list
            var max = Groups.Values.Count > 0 ? Groups.Values.Max(g => g.Order) : -1;
            group.Order = max + 1;
        }

        return new GroupBuilder(group);
    }

    public static void RegisterItem(Type itemType, string groupName, int? order = null)
    {
        if (!Groups.TryGetValue(groupName, out var group))
            throw new Exception($"Group '{groupName}' has not been registered.");

        var itemOrder = order ?? group.Items.Count;

        var entry = new ItemEntry(itemType);

        group.Items.Add(entry);
        group.ItemOrders[entry] = itemOrder;

        Groups[groupName] = group;
    }

    public static void RegisterItem(Type itemType, TerramonItemGroup group, int? order = null)
    {
        RegisterItem(itemType, group.ToString(), order);
    }

    public static IEnumerable<ItemEntry> GetSortedItems()
    {
        return Groups
            .OrderBy(g => g.Value.Order)
            .SelectMany(g =>
                g.Value.Items
                    .OrderBy(t => g.Value.ItemOrders[t])
                    .ThenBy(t => t.ItemType!.FullName));
    }

    public class GroupData
    {
        public readonly Dictionary<ItemEntry, int> ItemOrders = new();
        public readonly List<ItemEntry> Items = [];
        public int Order;
    }

    public sealed class ItemEntry
    {
        public ItemEntry(Type type)
        {
            ItemType = type;
        }

        public ItemEntry(ModItem instance)
        {
            Instance = instance;
            ItemType = instance.GetType();
        }

        public Type ItemType { get; }
        public ModItem Instance { get; }

        public bool IsInstance => Instance != null;
    }

    public sealed class GroupBuilder
    {
        private readonly GroupData _group;

        internal GroupBuilder(GroupData group)
        {
            _group = group;
        }

        public GroupBuilder Add<T>() where T : ModItem => Add(typeof(T));

        public GroupBuilder Add(Type itemType) => AddEntry(new ItemEntry(itemType));

        public GroupBuilder Add(ModItem instance) => AddEntry(new ItemEntry(instance));

        private GroupBuilder AddEntry(ItemEntry entry)
        {
            var order = _group.Items.Count;

            _group.Items.Add(entry);
            _group.ItemOrders[entry] = order;

            return this;
        }

        public GroupBuilder AddAllOfType<TBase>() where TBase : ModItem
        {
            var baseType = typeof(TBase);

            // Search all assemblies that might contain mod item classes
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray()!;
                }

                foreach (var t in types)
                {
                    if (t == null)
                        continue;

                    // Must be a subclass of the base type AND not abstract
                    if (t.IsSubclassOf(baseType) && !t.IsAbstract)
                    {
                        Add(t);
                    }
                }
            }

            return this;
        }
    }
}
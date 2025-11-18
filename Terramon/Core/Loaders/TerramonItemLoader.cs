using Terramon.Content.Items;
using Terramon.Content.Items.HeldItems;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Tiles.Interactive;
using Terramon.Content.Tiles.MusicBoxes;
using Terramon.Content.Tiles.Paintings;
using Terraria.ModLoader.Core;

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
    MegaStones,
    Interactive,
    MusicBoxes,
    PokeBallMinis,
    Vanity,
    Banners,
    Uncategorized
}

internal sealed class TerramonItemRegistration : ModSystem
{
    public TerramonItemRegistration()
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

        // Register mega stone group (populated in MegaStone)
        TerramonItemRegistry
            .RegisterGroup(TerramonItemGroup.MegaStones);

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
    public override void Load()
    {
        foreach (var item in TerramonItemRegistry.GetSortedItems())
        {
            Mod.AddContent(item);
        }
    }
}

public static class TerramonItemRegistry
{
    private static readonly Dictionary<string, GroupData> Groups = [];

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
        var item = (ModItem)Activator.CreateInstance(itemType);

        group.Items.Add(item);
        group.ItemOrders[item] = itemOrder;

        Groups[groupName] = group;
    }

    public static void RegisterItem(Type itemType, TerramonItemGroup group, int? order = null)
    {
        RegisterItem(itemType, group.ToString(), order);
    }

    public static IEnumerable<ModItem> GetSortedItems()
    {
        return Groups
            .OrderBy(g => g.Value.Order)
            .SelectMany(g =>
                g.Value.Items
                    .OrderBy(t => g.Value.ItemOrders[t])
                    .ThenBy(t => t.Name));
    }

    public class GroupData
    {
        public readonly Dictionary<ModItem, int> ItemOrders = new();
        public readonly List<ModItem> Items = [];
        public int Order;
    }

    public sealed class GroupBuilder
    {
        private readonly GroupData _group;

        internal GroupBuilder(GroupData group)
        {
            _group = group;
        }

        public GroupBuilder Add<T>() where T : ModItem
            => Add(typeof(T));

        public GroupBuilder Add(Type itemType)
        {
            try
            {
                var item = (ModItem)Activator.CreateInstance(itemType);

                Add(item);
            }
            catch (InvalidCastException)
            {
                Terramon.Instance.Logger.Error($"Failed to load item '{itemType.FullName}': Given type is not a {nameof(ModItem)}");
            }

            return this;
        }

        public GroupBuilder Add(ModItem item)
        {
            var order = _group.Items.Count;

            _group.Items.Add(item);
            _group.ItemOrders[item] = order;

            return this;
        }

        public GroupBuilder AddAllOfType<TBase>() where TBase : ModItem
        {
            var baseType = typeof(TBase);

            // Search all assemblies that might contain mod item classes
            var assemblies = ModLoader.Mods.Select(m => m.Code);

            foreach (var asm in assemblies)
            {
                var types = AssemblyManager.GetLoadableTypes(asm);

                foreach (var t in types)
                {
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
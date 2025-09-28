namespace Terramon.Core;

public static class TerramonItemAPI
{
    public static class Sets
    {
        /// <summary>
        ///     Indicates whether an item is legitimately obtainable in-game. If false, the item will have a tooltip
        ///     indicating it is unobtainable.
        /// </summary>
        public static HashSet<int> Unobtainable { get; private set; } = [];
    }
}
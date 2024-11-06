namespace Terramon.Content.Items;

[Autoload(false)]
public abstract class TerramonItem : ModItem
{
    /// <summary>
    ///     The rarity of the item. Defaults to White.
    /// </summary>
    protected virtual int UseRarity => ItemRarityID.White;

    public override string Texture => "ModLoader/UnloadedItem"; // Default texture

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
        Item.rare = UseRarity;
        Item.maxStack = Item.CommonMaxStack;
    }
}
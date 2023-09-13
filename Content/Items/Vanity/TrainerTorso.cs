using Terramon.Content.Items;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terramon.Content.Items.Vanity
{
	// The AutoloadEquip attribute automatically attaches an equip texture to this item.
	[AutoloadEquip(EquipType.Body)]
	public class TrainerTorso : TerramonItem
	{
        public override string Texture => "Terramon/Assets/Items/Vanity/" + GetType().Name;
        public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 18;
			Item.height = 18;
			Item.value = 3000;
			Item.rare = ItemRarityID.White;
			Item.vanity = true;
		}
	}
}

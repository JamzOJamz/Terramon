using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;

namespace Terramon.Content.Items.Mechanical
{
	public abstract class BasePkballItem : TerramonItem
	{
		public virtual int pokeballThrow => ModContent.ProjectileType<BasePkballProjectile>();
		public virtual int igPrice => -1; //ingame price (from pokemon games) so price scaling matches

		public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault($"BasePkball");
			// Tooltip.SetDefault("Throw it to catch a Pokemon!");
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 99; //Amount of Pokeballs needed to duplicate them in Journey Mode
		}

		public override void SetDefaults()
		{
			Item.CloneDefaults(ItemID.Shuriken);
			Item.shoot = pokeballThrow;
			Item.shootSpeed = 6.5f;
			Item.UseSound = new SoundStyle("Terramon/Content/Audio/Sounds/pkball_throw");
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 99;
			Item.damage = 0;
			Item.autoReuse = false;
			Item.useStyle = ItemUseStyleID.Thrust;
			Item.rare = ItemRarityID.Blue;
			Item.value = igPrice * 3;
			Item.useTime = 15;
			Item.consumable = true;
		}

        /*public override bool? UseItem(Player player) //Manage what happens when the player uses the item
        {
            Item.consumable = true;
            SoundEngine.PlaySound(new SoundStyle("TerramonMod/Sounds/pkball_throw"), player.position);
            return true;
        }*/
    }
}
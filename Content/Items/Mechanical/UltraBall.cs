using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;


namespace Terramon.Content.Items.Mechanical
{
    class UltraBallProjectile : BasePkballProjectile
    {
        public override int pokeballCapture => ModContent.ItemType<UltraBallItem>();
        public override float catchModifier => 2f;
    }

    class UltraBallItem : BasePkballItem
    {
        protected override int UseRarity => ModContent.RarityType<UltraBallRarity>();
        public override int pokeballThrow => ModContent.ProjectileType<UltraBallProjectile>();
        public override int igPrice => 800;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = igPrice / 2; //Amount needed to duplicate them in Journey Mode
        }
    }
    public class UltraBallRarity : ModRarity
    {
        public override Color RarityColor => new(249, 163, 27);
    }
}

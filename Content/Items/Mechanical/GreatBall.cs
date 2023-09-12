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
    class GreatBallProjectile : BasePkballProjectile
    {
        public override int pokeballCapture => ModContent.ItemType<GreatBallItem>();
        public override float catchModifier => 1.5f;
    }

    class GreatBallItem : BasePkballItem
    {
        protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
        public override int pokeballThrow => ModContent.ProjectileType<GreatBallProjectile>();
        public override int igPrice => 600;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = igPrice / 2; //Amount needed to duplicate them in Journey Mode
        }
    }
    public class GreatBallRarity : ModRarity
    {
        public override Color RarityColor => new(47, 155, 224);
    }
}

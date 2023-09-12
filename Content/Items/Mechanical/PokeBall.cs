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
    class PokeBallProjectile : BasePkballProjectile
    {
        public override int pokeballCapture => ModContent.ItemType<PokeBallItem>();
        public override float catchModifier => 1;
    }

    class PokeBallItem : BasePkballItem
    {
        protected override int UseRarity => ModContent.RarityType<PokeBallRarity>();
        public override int pokeballThrow => ModContent.ProjectileType<PokeBallProjectile>();
        public override int igPrice => 200;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = igPrice / 2; //Amount needed to duplicate them in Journey Mode
        }
    }
    public class PokeBallRarity : ModRarity
    {
        public override Color RarityColor => new(214, 74, 86);
    }
}

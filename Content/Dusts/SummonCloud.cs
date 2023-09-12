using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Terramon.Content.Dusts
{
	public class SummonCloud : ModDust
	{
        public override string Texture => "Terramon/Assets/Dusts/" + GetType().Name;
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = false;
            dust.noLight = false;
            dust.scale *= 2;
            dust.frame = new Rectangle(0, Main.rand.Next(3) * 16, 16, 16);
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity * 0.5f;
            dust.rotation += 0.01f;
            dust.scale -= 0.05f;

            if (dust.scale < 0.5f)
            {
                dust.active = false;
            }
            return false;
        }
    }
}

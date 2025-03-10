﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.DataStructures;

namespace Terramon.Content.Tiles
{
    public abstract class CustomPreviewTile : ModTile
    {
        private static bool _hasHooked = false;
        public override void Load()
        {
            if (!_hasHooked)
            {
                IL_TileObject.DrawPreview += ILCustomPreviewDrawing;
                _hasHooked = true;
            }
        }

        private static void ILCustomPreviewDrawing(ILContext il)
        {
            try
            {
                var localPosition = new VariableDefinition(il.Import(typeof(Vector2)));

                il.Body.Variables.Add(localPosition);

                var c = new ILCursor(il);

                // i'm so glad this is only done once in the whole code thank you terraria people
                if (!c.TryGotoNext(i => i.MatchLdarg0()))
                {
                    Terramon.Instance.Logger.Warn("ILCustomPreviewDrawing: Couldn't find draw call arguments load");
                    return;
                }

                // find when the position actually gets made

                if (!c.TryGotoNext(MoveType.After, i => i.MatchNewobj<Vector2>()))
                {
                    Terramon.Instance.Logger.Warn("ILCustomPreviewDrawing: Couldn't find construction of position");
                    return;
                }
                // store it
                c.EmitStloc(localPosition.Index);

                // find when the rectangle and color get loaded

                int rectangleIndex = 0;
                int colorIndex = 0;

                if (!c.TryFindNext(out _, i => i.MatchLdloc(out rectangleIndex), i => i.MatchLdloc(out colorIndex)))
                {
                    Terramon.Instance.Logger.Warn("ILCustomPreviewDrawing: Couldn't find start of useless arguments load");
                    return;
                }

                // right now, the cursor is between the position variable creation and the sourceRect load instructions.
                // currently, the spritebatch and texture2d are the only things that are loaded on the stack and consumable, because we consumed the position value earlier when storing it.
                // for easier branching, we can simply use the spritebatch and texture2d values from the stack and then emit them again later.

                // load all the necessary values for our method:

                // position
                c.EmitLdloc(localPosition.Index);
                // sourceRect
                c.EmitLdloc(rectangleIndex);
                // color
                c.EmitLdloc(colorIndex);
                // tileobjectpreviewdata
                c.EmitLdarg1();

                c.EmitDelegate<Func<SpriteBatch, Texture2D, Vector2, Rectangle?, Color, TileObjectPreviewData, bool>>((sb, t, p, r, c, top) =>
                {
                    ModTile modTile = TileLoader.GetTile(top.Type);
                    if (modTile != null && modTile is CustomPreviewTile previewTile)
                    {
                        return previewTile.PreDrawPlacementPreview(sb, top, t, p, r, c);
                    }
                    return true;
                });

                // currently, absolutely nothing is loaded on the stack (except for the delegate return bool), as we consumed all of it with our delegate.

                // let's get ready for branchin'
                var skipLabel = il.DefineLabel();
                c.EmitBrfalse(skipLabel); // this consumes the bool, so stack is once again balanced

                // let's load the values we consumed

                // spritebatch
                c.EmitLdarg0();
                // texture
                c.EmitLdloc1();
                // position
                c.EmitLdloc(localPosition.Index);

                // find the draw call
                if (!c.TryGotoNext(MoveType.After, i => i.MatchCallvirt<SpriteBatch>("Draw")))
                {
                    Terramon.Instance.Logger.Warn("ILCustomPreviewDrawing: Couldn't find main draw call");
                    return;
                }

                // mark the label to skip the draw call
                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(Terramon.Instance, il);
            }
        }
        /// <summary>
        /// Runs for each section of the drawn tile inside the placement preview drawing code.
        /// <para>Return false to stop regular drawing.</para>
        /// </summary>
        /// <param name="sb">The SpriteBatch.</param>
        /// <param name="data">The drawn preview data.</param>
        /// <param name="texture">The drawn texture.</param>
        /// <param name="position">The draw position.</param>
        /// <param name="sourceRect">The frame of the draw.</param>
        /// <param name="color">The color of the draw.</param>
        /// <returns>Whether or not regular drawing should run for this section.</returns>
        public virtual bool PreDrawPlacementPreview(SpriteBatch sb, TileObjectPreviewData data, Texture2D texture, Vector2 position, Rectangle? sourceRect, Color color)
        {
            return true;
        }
    }
}

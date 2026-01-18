using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent;

namespace Terramon.Helpers;

public class TerramonCursorOverrideID : ILoadable
{
    private const int CustomCursorCount = 4;

    public static short WithdrawPCRed { get; private set; } = -1;
    public static short WithdrawPCWhite { get; private set; } = -1;
    public static short DepositPCRed { get; private set; } = -1;
    public static short DepositPCWhite { get; private set; } = -1;

    public void Load(Mod mod)
    {
        var originalLength = TextureAssets.Cursors.Length;
        var newLength = originalLength + CustomCursorCount;

        Array.Resize(ref TextureAssets.Cursors, newLength);

        // Load custom cursor textures
        TextureAssets.Cursors[originalLength] =
            mod.Assets.Request<Texture2D>("Assets/GUI/Miscellaneous/CursorWithdrawPCRed");
        TextureAssets.Cursors[originalLength + 1] =
            mod.Assets.Request<Texture2D>("Assets/GUI/Miscellaneous/CursorWithdrawPCWhite");
        TextureAssets.Cursors[originalLength + 2] =
            mod.Assets.Request<Texture2D>("Assets/GUI/Miscellaneous/CursorDepositPCRed");
        TextureAssets.Cursors[originalLength + 3] =
            mod.Assets.Request<Texture2D>("Assets/GUI/Miscellaneous/CursorDepositPCWhite");

        // Assign cursor IDs
        WithdrawPCRed = (short)originalLength;
        WithdrawPCWhite = (short)(originalLength + 1);
        DepositPCRed = (short)(originalLength + 2);
        DepositPCWhite = (short)(originalLength + 3);

        // IL edit cursor drawing to draw custom cursors without shadow
        IL_Main.DrawInterface_36_Cursor += IL_DisableShadowForCustomCursors;
    }

    public void Unload()
    {
        var originalLength = TextureAssets.Cursors.Length - CustomCursorCount;
        Array.Resize(ref TextureAssets.Cursors, originalLength);

        WithdrawPCRed = -1;
        WithdrawPCWhite = -1;
        DepositPCRed = -1;
        DepositPCWhite = -1;
    }

    private static void IL_DisableShadowForCustomCursors(ILContext il)
    {
        try
        {
            // Start the Cursor at the start
            var c = new ILCursor(il);

            // if (flag) // Right before cursor shadow is drawn
            c.GotoNext(MoveType.After, i => i.MatchLdloc2());

            // Nop out the original flag local variable loading
            var prev = c.Prev;
            prev.OpCode = OpCodes.Nop;
            prev.Operand = null;

            // Get the addresses of the local variables used for the flag and the color
            c.EmitLdloca(2);
            c.EmitLdloca(1);

            c.EmitDelegate((ref bool flag, ref Color white) =>
            {
                // If the cursorOverride is one of Terramon's special cursors, we skip drawing the shadow
                var cursorOverride = Main.cursorOverride;
                if (cursorOverride != WithdrawPCRed &&
                    cursorOverride != WithdrawPCWhite &&
                    cursorOverride != DepositPCRed &&
                    cursorOverride != DepositPCWhite) return;
                flag = false;
                white = Color.White;
            });

            // Loads the flag value back onto the stack for the original code to use
            c.EmitLdloc2();
        }
        catch
        {
            MonoModHooks.DumpIL(Terramon.Instance, il);
        }
    }
}
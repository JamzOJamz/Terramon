using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;

namespace Terramon.Core;

public class TerramonSystem : ModSystem
{
    public override void PreSaveAndQuit()
    {
        UILoader.GetUIState<PartyDisplay>().ClearAllSlots();
    }
}
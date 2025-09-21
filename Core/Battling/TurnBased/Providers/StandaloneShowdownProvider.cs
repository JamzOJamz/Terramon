namespace Terramon.Core.Battling.TurnBased;

public sealed class StandaloneShowdownProvider : IShowdownProvider
{
    public void Dispose()
    {
        // No resources to clean up. Implementation required by interface.
    }

    public void WriteCommand(string type, object[] data)
    {
        throw new NotImplementedException();
    }

    public event Action<string, string[]> ReceiveMessage;
}
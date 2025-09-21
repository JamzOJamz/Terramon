namespace Terramon.Core.Battling.TurnBased;

public class ShowdownBattle : IDisposable
{
    private readonly IShowdownService _service;

    public ShowdownBattle(IShowdownService service)
    {
        _service = service;
        _service.ReceiveMessage += OnReceiveMessage;
    }

    private static void OnReceiveMessage(string type, string[] data)
    {
        switch (type)
        {
            case "update":
                // Handle update messages
                Main.NewText("Received update: " + string.Join(", ", data));
                break;
        }
    }

    public void Start(BattleOptions options)
    {
        ExecuteCommand("start", [options]);
    }

    private void ExecuteCommand(string type, object[] data)
    {
        _service.WriteCommand(type, data);
    }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }
}
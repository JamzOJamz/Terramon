using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace Terramon.Core.Systems;

public class DiscordInviteBeamer : ModSystem
{
    private const string TerramonInviteCode = "qDn5eW27c4";
    private const int RpcVersion = 1;
    private const int ChanceToBeamDenominator = 1;
    private static readonly Tuple<int, int> RpcPortRange = new(6463, 6472);

    public override void OnModLoad()
    {
        if (!Terramon.IsFirstTimeLoad || !Main.rand.NextBool(ChanceToBeamDenominator) || !IsClientRunning())
            return;

        // Try beaming the invite code to the Discord client
        Task.Run(() => Send(TerramonInviteCode));
    }

    private static bool IsClientRunning()
    {
        return Process.GetProcessesByName("Discord").Length > 0;
    }

    private static async Task Send(string inviteCode)
    {
        foreach (var port in Enumerable.Range(RpcPortRange.Item1, RpcPortRange.Item2 - RpcPortRange.Item1 + 1))
        {
            var url = $"ws://127.0.0.1:{port}/?v={RpcVersion}";
            using var client = new ClientWebSocket();
            client.Options.SetRequestHeader("Origin", "https://discord.com"); // Required to work properly

            try
            {
                // Attempt to connect to the Discord RPC server
                await client.ConnectAsync(new Uri(url), CancellationToken.None);

                if (client.State != WebSocketState.Open) continue;

                Terramon.Instance.Logger.Debug($"Connected to {url}");

                // Send the invite code or payload to the Discord RPC server
                var payload =
                    $$"""{"cmd":"INVITE_BROWSER","args":{"code":"{{inviteCode}}"},"nonce":"{{Guid.NewGuid()}}"}""";
                var buffer = Encoding.UTF8.GetBytes(payload);
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);

                Terramon.Instance.Logger.Debug($"Discord invite {inviteCode} sent to client!");

                // Wait for 5 seconds before closing the connection
                await Task.Delay(5000);

                Terramon.Instance.Logger.Debug("Closing connection...");

                return; // Exit after successful connection, sending, and logging messages
            }
            catch (WebSocketException ex)
            {
                Terramon.Instance.Logger.Debug($"Failed to connect to {url}: {ex.Message}");
            }
        }

        Terramon.Instance.Logger.Debug("Failed to connect to any Discord RPC server.");
    }
}
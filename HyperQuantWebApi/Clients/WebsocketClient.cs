using System.Net.WebSockets;
using System.Text;

namespace HyperQuantWebApi.Clients
{
    public class WebsocketClient
    {
        private readonly Action<string> _onMessageReceived;

        public WebsocketClient(Action<string> onMessageReceived)
        {
            _onMessageReceived = onMessageReceived;
        }

        public async Task StartClientAsync(string url, string message)
        {
            using (var webSocket = new ClientWebSocket())
            {
                await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);

                if (!string.IsNullOrEmpty(message))
                {
                    await SendMessageAsync(webSocket, message);
                }

                await ReceiveMessageAsync(webSocket);
            }
        }

        private async Task SendMessageAsync(ClientWebSocket webSocket, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        private async Task ReceiveMessageAsync(ClientWebSocket webSocket)
        {
            var buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                        CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                _onMessageReceived?.Invoke(message);
            }
        }
    }
}

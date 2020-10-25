using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Garage48.DeepFakeDetection.Server.Extensions;
using Newtonsoft.Json;

namespace Garage48.DeepFakeDetection.Server.Services
{
    public sealed class ClientHandler
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _rooms = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>>();

        private readonly VideoService _videoService;

        public ClientHandler(VideoService videoService)
        {
            _videoService = videoService;
        }

        public async Task HandleAsync(WebSocket client)
        {
            var clientId = Guid.NewGuid();
            var receivedClose = false;
            var roomId = string.Empty;

            while (!receivedClose)
            {
                try
                {
                    var (result, bytes) = await client.ReceiveFullMessageAsync(CancellationToken.None);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var msgText = Encoding.UTF8.GetString(bytes);
                            var msg = JsonConvert.DeserializeObject<RoomMessage>(msgText);
                            _rooms.AddOrUpdate(msg.RoomId,
                                _ => new ConcurrentDictionary<Guid, WebSocket>(new Dictionary<Guid, WebSocket>
                                    {{clientId, client}}),
                                (_, __) => new ConcurrentDictionary<Guid, WebSocket>(new Dictionary<Guid, WebSocket>
                                    {{clientId, client}}));
                            roomId = msg.RoomId;
                            break;
                        case WebSocketMessageType.Binary:
                            if (!_rooms.TryGetValue(roomId, out var clients))
                            {
                                break;
                            }

                            try
                            {
                                var (processedBytes, maybeEvalResult) = await _videoService.HandleSegmentAsync(clientId, bytes);

                                if (maybeEvalResult != null)
                                {
                                    await Task.WhenAll(clients.ToArray().Select(pair =>
                                        pair.Value.SendAsync(Encoding.UTF8.GetBytes(maybeEvalResult),
                                            WebSocketMessageType.Text,
                                            true,
                                            CancellationToken.None)));
                                }

                                //.Where(pair => pair.Key != clientId)
                                await Task.WhenAll(clients.ToArray().Select(pair =>
                                    pair.Value.SendAsync(processedBytes.ToArray(),
                                        WebSocketMessageType.Binary,
                                        true,
                                        CancellationToken.None)));
                            }
                            catch (WebSocketException e)
                            {
                                foreach (var (savedClientId, _) in clients.ToArray().Where(pair => pair.Value.State != WebSocketState.Open))
                                {
                                    if(clients.TryRemove(savedClientId, out var socket)) socket.Abort();
                                }
                            }
                            break;
                        case WebSocketMessageType.Close:
                            receivedClose = true;
                            if (_rooms.TryGetValue(roomId, out var roomClients))
                            {
                                roomClients.TryRemove(clientId, out _);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (WebSocketException e)
                {
                    return;
                }
            }
        }

        public sealed class RoomMessage
        {
            public string RoomId { get; set; }
        }
    }
}
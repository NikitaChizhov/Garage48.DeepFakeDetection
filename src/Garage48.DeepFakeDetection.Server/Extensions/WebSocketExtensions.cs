using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Garage48.DeepFakeDetection.Server.Extensions
{
    internal static class WebSocketExtensions
    {
        private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create();

        public static async Task<(WebSocketReceiveResult, ArraySegment<byte>)> ReceiveFullMessageAsync(this WebSocket socket, CancellationToken cancellationToken)
        {
            WebSocketReceiveResult response;
            var message = new List<byte>();

            var buffer = new ArraySegment<byte>(ArrayPool.Rent(1024 * 32));
            do
            {
                response = await socket.ReceiveAsync(buffer, cancellationToken);
                message.AddRange(buffer.Slice(0, response.Count));
            } while (!response.EndOfMessage);

            return (response, message.ToArray());
        }
    }
}
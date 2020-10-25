using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Garage48.DeepFakeDetection.Server.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Garage48.DeepFakeDetection.Server
{
    public sealed class WebsocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ClientHandler _clientHandler;
        
        public WebsocketHandlerMiddleware(RequestDelegate next, ClientHandler clientHandler)
        {
            _next = next;
            _clientHandler = clientHandler;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await _clientHandler.HandleAsync(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await _next(context);
            }
        }

    }
}
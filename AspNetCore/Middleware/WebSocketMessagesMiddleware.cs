using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Middleware
{
	public class WebSocketMessagesMiddleware
	{
		private readonly RequestDelegate _next;

		public WebSocketMessagesMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest)
			{
				await _next.Invoke(context);
			}
			else
			{
				var ct = context.RequestAborted;
				using (var socket = await context.WebSockets.AcceptWebSocketAsync())
				{
					var interval = int.Parse(await ReceiveStringAsync(socket, ct));
					for (var i = 0; ; i++)
					{
						await SendStringAsync(socket, i.ToString(), ct);
						await Task.Delay(interval);
					}
				}
			}
		}

		private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
		{
			var buffer = Encoding.UTF8.GetBytes(data);
			var segment = new ArraySegment<byte>(buffer);

			return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
		}

		private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
		{
			var buffer = new ArraySegment<byte>(new byte[8192]);
			using (var ms = new MemoryStream())
			{
				WebSocketReceiveResult result;
				do
				{
					ct.ThrowIfCancellationRequested();

					result = await socket.ReceiveAsync(buffer, ct);
					ms.Write(buffer.Array, buffer.Offset, result.Count);
				}
				while (!result.EndOfMessage);

				ms.Seek(0, SeekOrigin.Begin);
				if (result.MessageType != WebSocketMessageType.Text)
				{
					throw new Exception("Unexpected message");
				}

				using (var reader = new StreamReader(ms, Encoding.UTF8))
				{
					return await reader.ReadToEndAsync();
				}
			}
		}
	}

	public static class WebSocketMessageMiddlewarextension
	{
		public static IApplicationBuilder UseWebSocketMessage(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<WebSocketMessagesMiddleware>();
		}
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

public interface IWebSocketConnection : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken token);
    Task SendAsync(byte[] buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token);
    Task<WebSocketReceiveResult> ReceiveAsync(byte[] buffer, CancellationToken token);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken token);
} 
using System.Net.WebSockets;

/// <summary>
/// Класс для подключения к WebSocket API
/// </summary>
/// <remarks>
/// ClientWebSocket - класс нельзя мокать
/// Паттерн "Адаптер" - используем этот класс для тестирования
/// <remarks>
public class WebSocketConnection : IWebSocketConnection
{
    private readonly ClientWebSocket _ws;

    public WebSocketConnection()
    {
        _ws = new ClientWebSocket();
    }

    public WebSocketState State => _ws.State;

    public Task ConnectAsync(Uri uri, CancellationToken token) => _ws.ConnectAsync(uri, token);
    public Task SendAsync(byte[] buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken token) 
        => _ws.SendAsync(buffer, messageType, endOfMessage, token);
    public Task<WebSocketReceiveResult> ReceiveAsync(byte[] buffer, CancellationToken token) 
        => _ws.ReceiveAsync(buffer, token);
    public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken token) 
        => _ws.CloseAsync(closeStatus, statusDescription, token);
    public void Dispose() => _ws.Dispose();
} 
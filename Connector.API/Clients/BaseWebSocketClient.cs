namespace Connector.API.Clients;

using System.Net.WebSockets;
using System.Text;
using Connector.API.Interfaces;
using Connector.API.Models;

/// <summary>
/// Базовый класс для WebSocket клиентов
/// Реализует механизм переподключения и общую логику обработки соединения и ошибок
/// Управляет жизненным циклом соединения
/// </summary>
public abstract class BaseWebSocketClient : IWebSocketClient, IDisposable
{
    private IWebSocketConnection _ws;
    private CancellationTokenSource _cts;
    private readonly int _maxReconnectAttempts = 5;
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);
    private int _reconnectAttempts;
    private bool _isDisposed;
    private readonly Uri _wsUri;
    
    protected bool IsConnected => _ws.State == WebSocketState.Open;
    
    protected BaseWebSocketClient(Uri wsUri, IWebSocketConnection? ws = default)
    {
        _wsUri = wsUri;
        _ws = ws ?? new WebSocketConnection();
        _cts = new CancellationTokenSource();
    }

    private event Action<Trade> _onTradeReceived = delegate { };
    private event Action<Candle> _onCandleReceived = delegate { };

    public virtual event Action<Trade> OnTradeReceived
    {
        add => _onTradeReceived += value;
        remove => _onTradeReceived -= value;
    }

    public virtual event Action<Candle> OnCandleReceived
    {
        add => _onCandleReceived += value;
        remove => _onCandleReceived -= value;
    }

    public async Task ConnectAsync()
    {
        if (IsConnected) return;

        await Task.Run(() => 
        {
            _ws.Dispose();
            _cts.Dispose();
        });

        _ws = new WebSocketConnection();
        _cts = new CancellationTokenSource();

        try
        {
            // Сначала подключаемся
            await _ws.ConnectAsync(_wsUri, _cts.Token);
            
            // Затем выполняем дополнительную инициализацию
            await ConnectInternalAsync();
            
            _reconnectAttempts = 0;
            _ = StartListening();
        }
        catch (Exception ex)
        {
            await HandleConnectionErrorAsync(ex);
        }
    }

    public Task DisconnectAsync()
    {
        if (IsConnected)
        {
            return _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
        }

        _ws.Dispose();
        _cts.Cancel();
        return Task.CompletedTask;
    }

    protected abstract Task ConnectInternalAsync();
    public abstract Task SubscribeToTradesAsync(string pair);
    public abstract Task UnsubscribeFromTradesAsync(string pair);
    public abstract Task SubscribeToCandlesAsync(string pair, int periodInSec);
    public abstract Task UnsubscribeFromCandlesAsync(string pair);

    protected async Task SendMessageAsync(string message)
    {
        if (!IsConnected)
            throw new InvalidOperationException("WebSocket не подключен");

        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
    }

    private async Task StartListening()
    {
        var buffer = new byte[4096];
        
        while (IsConnected && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _ws.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var copy = string.Concat(message);
                    _ = Task.Run(() => ProcessMessageAsync(copy));
                }
            }
            catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
            {
                await HandleConnectionErrorAsync(ex);
            }
        }
    }

    public abstract Task ProcessMessageAsync(string message);

    private async Task HandleConnectionErrorAsync(Exception ex)
    {
        if (_reconnectAttempts >= _maxReconnectAttempts)
        {
            throw new Exception($"Превышено максимальное количество попыток подключения ({_maxReconnectAttempts})", ex);
        }

        _reconnectAttempts++;
        // Повторяем попытку подключения через увеличивающийся интервал
        var delay = TimeSpan.FromSeconds(Math.Pow(2, _reconnectAttempts));
        await Task.Delay(delay);

        await ConnectAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _cts.Cancel();
                DisconnectAsync().Wait();
                _ws.Dispose();
                _cts.Dispose();
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
} 
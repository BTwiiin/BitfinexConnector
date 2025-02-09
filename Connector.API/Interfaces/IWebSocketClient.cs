namespace Connector.API.Interfaces;

using Connector.API.Models;

public interface IWebSocketClient
{
    event Action<Trade> OnTradeReceived;
    event Action<Candle> OnCandleReceived;
    
    Task SubscribeToTradesAsync(string pair);
    Task UnsubscribeFromTradesAsync(string pair);
    
    Task SubscribeToCandlesAsync(string pair, int periodInSec);
    Task UnsubscribeFromCandlesAsync(string pair);
    
    Task ConnectAsync();
    Task DisconnectAsync();
} 
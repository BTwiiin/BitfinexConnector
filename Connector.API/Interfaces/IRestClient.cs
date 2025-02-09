namespace Connector.API.Interfaces;

using Connector.API.Models;

public interface IRestClient
{
    Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount);
    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = null);
    Task<Ticker> GetTickerAsync(string pair);
} 
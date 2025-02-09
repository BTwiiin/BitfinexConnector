namespace Connector.API.Clients;

using System.Text.Json;
using Connector.API.Models;

public class RestClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api-pub.bitfinex.com/v2";

    public RestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount)
    {
        // Bitfinex v2 trades endpoint: /trades/tSYMBOL/hist
        var response = await _httpClient.GetAsync($"/trades/t{pair}/hist?limit={maxCount}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        // Bitfinex returns trades as arrays: [ID, MTS, AMOUNT, PRICE]
        var rawTrades = JsonSerializer.Deserialize<decimal[][]>(content);
        
        return rawTrades?.Select(t => new Trade
        {
            Id = t[0].ToString(),
            Pair = pair,
            Time = DateTimeOffset.FromUnixTimeMilliseconds((long)t[1]),
            Amount = Math.Abs(t[2]),
            Price = t[3],
            Side = t[2] > 0 ? "BUY" : "SELL"
        }) ?? new List<Trade>();
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = null)
    {
        // Convert seconds to Bitfinex timeframe format
        var timeframe = periodInSec switch
        {
            60 => "1m",
            300 => "5m",
            900 => "15m",
            1800 => "30m",
            3600 => "1h",
            7200 => "2h",
            14400 => "4h",
            86400 => "1D",
            _ => throw new ArgumentException($"Unsupported period: {periodInSec}")
        };

        var queryParams = new List<string>();
        
        if (count.HasValue)
            queryParams.Add($"limit={count}");
        if (from.HasValue)
            queryParams.Add($"start={from.Value.ToUnixTimeMilliseconds()}");
        if (to.HasValue)
            queryParams.Add($"end={to.Value.ToUnixTimeMilliseconds()}");
            
        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        
        // Bitfinex v2 candles endpoint: /candles/trade:timeframe:tSYMBOL/hist
        var response = await _httpClient.GetAsync($"/candles/trade:{timeframe}:t{pair}/hist{queryString}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        // Bitfinex returns candles as arrays: [MTS, OPEN, HIGH, LOW, CLOSE, VOLUME, PRICE]
        var rawCandles = JsonSerializer.Deserialize<decimal[][]>(content);
        
        return rawCandles?.Select(c => new Candle
        {
            Pair = pair,
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)c[0]),
            OpenPrice = c[1],
            HighPrice = c[2],
            LowPrice = c[3],
            ClosePrice = c[4],
            TotalVolume = c[5],
            TotalPrice = c[6]
        }) ?? new List<Candle>();
    }

    public async Task<Ticker> GetTickerAsync(string pair)
    {
        // Bitfinex v2 ticker endpoint: /ticker/tSYMBOL
        var response = await _httpClient.GetAsync($"/ticker/t{pair}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        // Bitfinex returns ticker as array: [BID, BID_SIZE, ASK, ASK_SIZE, DAILY_CHANGE, ...]
        var rawTicker = JsonSerializer.Deserialize<decimal[][]>(content);
        
        if (rawTicker == null || !rawTicker.Any())
            throw new InvalidOperationException("Invalid ticker data received");
        
        var tickerData = rawTicker[0];
        return new Ticker
        {
            Bid = tickerData[0],
            BidSize = tickerData[1],
            Ask = tickerData[2],
            AskSize = tickerData[3],
            DailyChange = tickerData[4],
            DailyChangeRelative = tickerData[5],
            LastPrice = tickerData[6],
            Volume = tickerData[7],
            High = tickerData[8],
            Low = tickerData[9]
        };
    }
}


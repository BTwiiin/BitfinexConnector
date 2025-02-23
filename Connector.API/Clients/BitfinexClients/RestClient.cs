namespace Connector.API.Clients;

using System.Text.Json;
using Connector.API.Models;
using Connector.API.Interfaces;
using System.Net.Http;
using System.IO;
using System.Text.Json.Serialization;

/// <summary>
/// REST клиент для Bitfinex API v2
/// Документация: https://docs.bitfinex.com
/// 
/// Особенности реализации:
/// - Все ответы приходят в виде массивов
/// - Timestamp в миллисекундах
/// - Отрицательный объем означает продажу
/// </summary>
public class RestClient : BaseRestClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api-pub.bitfinex.com/v2/";
    private const string LogFile = "rest_client.log";

    private string GetTimeframe(int periodInSec)
    {
        return periodInSec switch
        {
            60 => "1m",
            300 => "5m",
            900 => "15m",
            1800 => "30m",
            3600 => "1h",
            7200 => "2h",
            14400 => "4h",
            21600 => "6h",
            43200 => "12h",
            86400 => "1D",
            _ => "1m"
        };
    }

    private void Log(string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
        File.AppendAllText(LogFile, logMessage + Environment.NewLine);
    }

    public RestClient(HttpClient httpClient) : base(httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var url = $"{BaseUrl}trades/t{pair}/hist?limit={maxCount}";
            var response = await _httpClient.GetAsync(url);
            
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
        });
    }

    public override async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = null)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var timeframe = GetTimeframe(periodInSec);
            var url = $"{BaseUrl}candles/trade:{timeframe}:t{pair}/hist";
            
            var queryParams = new List<string>();
            
            if (count.HasValue)
                queryParams.Add($"limit={count}");
            if (from.HasValue)
                queryParams.Add($"start={from.Value.ToUnixTimeMilliseconds()}");
            if (to.HasValue)
                queryParams.Add($"end={to.Value.ToUnixTimeMilliseconds()}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);
            
            var response = await _httpClient.GetAsync(url);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            // Bitfinex returns candles as arrays: [MTS, OPEN, HIGH, LOW, CLOSE, VOLUME, PRICE]
            var rawCandles = JsonSerializer.Deserialize<decimal[][]>(content);
            
            return rawCandles?.Select(c => new Candle
            {
                Pair = pair,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)c[0]),
                OpenPrice = c[1],
                ClosePrice = c[2],
                HighPrice = c[3],
                LowPrice = c[4],
                TotalVolume = c[5]
            }) ?? new List<Candle>();
        });
    }

    public override async Task<Ticker> GetTickerAsync(string pair)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var url = $"{BaseUrl}ticker/{pair}";
            Log($"GET Request: {url}");
            
            var response = await _httpClient.GetAsync(url);
            Log($"Response Status: {(int)response.StatusCode} {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            Log($"Response Content: {content}");
            
            response.EnsureSuccessStatusCode();
            
            // Handle nested array response
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            var tickerData = JsonSerializer.Deserialize<decimal[]>(content, options);

            if (tickerData == null || !tickerData.Any())
                throw new InvalidOperationException("Invalid ticker data received");
            
            Log($"Parsed ticker data for {pair}: LastPrice={tickerData[6]}");
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
        });
    }

    public async Task<Dictionary<string, decimal>> GetTickersAsync(string[] pairs)
    {
        var prices = new Dictionary<string, decimal>();
        Log($"Getting tickers for pairs: {string.Join(", ", pairs)}");
        foreach (var pair in pairs)
        {
            try 
            {
                var ticker = await GetTickerAsync(pair);
                prices[pair] = ticker.LastPrice;
                Log($"Got price for {pair}: {ticker.LastPrice}");
            }
            catch (Exception ex)
            {
                Log($"Error getting ticker for {pair}: {ex.Message}");
                throw;
            }
        }
        return prices;
    }
}


using System.Text.Json;
using System.Diagnostics;
using Connector.API.Models;
using System.Text.Json.Serialization;
using System.Text;

namespace Connector.API.Clients;

/// <summary>
/// WebSocket клиент для Bitfinex API v2
/// Документация: https://docs.bitfinex.com/reference/ws-public-trades
/// 
/// Особенности реализации:
/// - Подписки через каналы
/// - Обработка heartbeat сообщений
/// - Автоматическое переподключение
/// </summary>
public class WebSocketClient : BaseWebSocketClient
{
    private static readonly Uri _wsUri = new("wss://api-pub.bitfinex.com/ws/2");
    private readonly Dictionary<string, int> _tradeChannelIds = new();
    private readonly Dictionary<string, int> _candleChannelIds = new();
    private readonly Dictionary<int, string> _channelPairs = new();
    private static readonly string LogPath = "websocket_log.txt";
    private StringBuilder _messageBuffer = new StringBuilder();
    
    public WebSocketClient(IWebSocketConnection? ws = default) : base(_wsUri, ws) { }

    public override event Action<Trade> OnTradeReceived = delegate { };
    public override event Action<Candle> OnCandleReceived = delegate { };

    protected override async Task ConnectInternalAsync()
    {
        await base.SendMessageAsync(JsonSerializer.Serialize(new { @event = "conf", flags = 32768 }));
    }

    public override async Task SubscribeToTradesAsync(string pair)
    {
        var msg = new { 
            @event = "subscribe", 
            channel = "trades", 
            symbol = $"t{pair}" 
        };
        await SendMessageAsync(JsonSerializer.Serialize(msg));
    }

    public override async Task UnsubscribeFromTradesAsync(string pair)
    {
        if (_tradeChannelIds.TryGetValue(pair, out int chanId))
        {
            var msg = new { 
                @event = "unsubscribe", 
                chanId = chanId 
            };
            Log($"Unsubscribing from trades: {pair}, chanId: {chanId}");
            await SendMessageAsync(JsonSerializer.Serialize(msg));
            _tradeChannelIds.Remove(pair);
            _channelPairs.Remove(chanId);
        }
    }

    public override async Task SubscribeToCandlesAsync(string pair, int periodInSec)
    {
        var timeframe = periodInSec switch
        {
            60 => "1m",    // 1 минута
            300 => "5m",   // 5 минут
            900 => "15m",  // 15 минут
            1800 => "30m", // 30 минут
            3600 => "1h",  // 1 час
            7200 => "2h",  // 2 часа
            14400 => "4h", // 4 часа
            86400 => "1D", // 1 день
            _ => throw new ArgumentException($"Неподдерживаемый период: {periodInSec}")
        };

        Trace.WriteLine($"Subscribing to candles: {pair}, timeframe: {timeframe}");

        var msg = new { 
            @event = "subscribe", 
            channel = "candles", 
            key = $"trade:{timeframe}:t{pair}" 
        };
        var jsonMsg = JsonSerializer.Serialize(msg);
        Trace.WriteLine($"Sending subscription message: {jsonMsg}");
        await SendMessageAsync(jsonMsg);
    }

    public override async Task UnsubscribeFromCandlesAsync(string pair)
    {
        if (_candleChannelIds.TryGetValue(pair, out int chanId))
        {
            var msg = new { 
                @event = "unsubscribe", 
                chanId = chanId 
            };
            Log($"Unsubscribing from candles: {pair}, chanId: {chanId}");
            await SendMessageAsync(JsonSerializer.Serialize(msg));
            _candleChannelIds.Remove(pair);
            _channelPairs.Remove(chanId);
        }
    }

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n");
        }
        catch { }
    }

    public override async Task ProcessMessageAsync(string message)
    {
        try
        {
            Log($"Received WS message: {message}");
            await Task.Yield();

            // Skip processing split/incomplete messages
            if (message.StartsWith(",") || message.StartsWith(".") || 
                message.All(c => char.IsDigit(c) || c == ',' || c == '.'))
            {
                return;
            }

            // Try to parse as complete message
            try
            {
                using var doc = JsonDocument.Parse(message);
                await ProcessCompleteMessage(doc.RootElement);
            }
            catch (JsonException)
            {
                // Silently ignore split message parsing errors
                return;
            }
        }
        catch (Exception ex)
        {
            Log($"ProcessMessage Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    private async Task ProcessCompleteMessage(JsonElement root)
    {
        if (!IsValidMessage(root)) return;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var channelId = root[0].GetInt32();
            
            // Skip heartbeat messages
            if (root[1].ValueKind == JsonValueKind.String && root[1].GetString() == "hb")
                return;

            if (!_channelPairs.TryGetValue(channelId, out var pair))
            {
                Log($"Unknown channel ID: {channelId}");
                return;
            }

            // Handle trades
            if (root[1].ValueKind == JsonValueKind.String && root[1].GetString() == "tu")
            {
                if (!TryParseTradeData(root[2], pair, out var trade) || trade == null) return;
                OnTradeReceived?.Invoke(trade);
            }
            // Handle candles
            else if (root[1].ValueKind == JsonValueKind.Array)
            {
                // Handle both single candle updates and snapshot arrays
                var candlesArray = root[1];
                
                // If it's a snapshot (array of arrays)
                if (candlesArray[0].ValueKind == JsonValueKind.Array)
                {
                    foreach (var candleElement in candlesArray.EnumerateArray())
                    {
                        if (TryParseCandleData(candleElement, pair, out var candle) && candle != null)
                        {
                            OnCandleReceived?.Invoke(candle);
                        }
                    }
                }
                // If it's a single candle update
                else if (TryParseCandleData(candlesArray, pair, out var candle) && candle != null)
                {
                    OnCandleReceived?.Invoke(candle);
                }
            }
        }
        else if (root.TryGetProperty("event", out var eventProp))
        {
            Trace.WriteLine($"Received event: {eventProp.GetString()}");
            if (eventProp.GetString() == "subscribed")
            {
                var channelId = root.GetProperty("chanId").GetInt32();
                var channel = root.GetProperty("channel").GetString();
                Trace.WriteLine($"Subscription confirmed - Channel: {channel}, ID: {channelId}");
                if (channel == "trades" && root.TryGetProperty("symbol", out var symbolProp))
                {
                    var symbol = symbolProp.GetString();
                    var pair = symbol?.Substring(1); // Remove 't' prefix
                    if (pair != null)
                    {
                        _channelPairs[channelId] = pair;
                        _tradeChannelIds[pair] = channelId;
                    }
                }
                else if (channel == "candles" && root.TryGetProperty("key", out var keyProp))
                {
                    var key = keyProp.GetString();
                    var pair = key?.Split(':').Last().Substring(1); // Fix: Extract pair correctly from "trade:1m:tBTCUSD" format
                    Log($"Candles subscription - Key: {key}, Extracted pair: {pair}");
                    if (pair != null)
                    {
                        _channelPairs[channelId] = pair;
                        _candleChannelIds[pair] = channelId;
                        Log($"Added candle channel mapping - Pair: {pair}, ChannelId: {channelId}");
                    }
                }
            }
        }
    }

    private bool IsValidMessage(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array && root.ValueKind != JsonValueKind.Object)
            return false;

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0)
            return false;

        return true;
    }

    private bool TryParseTradeData(JsonElement tradeData, string pair, out Trade? trade)
    {
        trade = default;
        try
        {
            trade = new Trade
            {
                Id = tradeData[0].GetDecimal().ToString(),
                Pair = pair,
                Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeData[1].GetInt64()),
                Amount = Math.Abs(tradeData[2].GetDecimal()),
                Price = tradeData[3].GetDecimal(),
                Side = tradeData[2].GetDecimal() > 0 ? "BUY" : "SELL"
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseCandleData(JsonElement candleData, string pair, out Candle? candle)
    {
        candle = default;
        try
        {
            candle = new Candle
            {
                Pair = pair,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleData[0].GetInt64()),
                OpenPrice = candleData[1].GetDecimal(),
                HighPrice = candleData[2].GetDecimal(),
                LowPrice = candleData[3].GetDecimal(),
                ClosePrice = candleData[4].GetDecimal(),
                TotalVolume = candleData[5].GetDecimal()
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class SubscriptionResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    [JsonPropertyName("chanId")]
    public int ChanId { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }
}


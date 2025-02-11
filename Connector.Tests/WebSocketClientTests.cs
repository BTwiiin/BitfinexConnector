using System.Net.WebSockets;
using Moq;
using Xunit;
using Connector.API.Clients;
using Connector.API.Models;

namespace Connector.Tests;

public class WebSocketClientTests
{
    private readonly Mock<IWebSocketConnection> _mockWs;
    private readonly WebSocketClient _client;
    private readonly List<Trade> _receivedTrades;
    private readonly List<Candle> _receivedCandles;

    public WebSocketClientTests()
    {
        _mockWs = new Mock<IWebSocketConnection>();
        _client = new WebSocketClient(_mockWs.Object);
        _receivedTrades = new List<Trade>();
        _receivedCandles = new List<Candle>();

        _client.OnTradeReceived += trade => _receivedTrades.Add(trade);
        _client.OnCandleReceived += candle => _receivedCandles.Add(candle);

        // Setup mock connection state
        _mockWs.Setup(ws => ws.State).Returns(WebSocketState.Open);
    }

    [Theory]
    [InlineData("BTCUSD")]
    [InlineData("ETHUSD")]
    [InlineData("LTCUSD")]
    public async Task ProcessMessage_ShouldRaiseTradeEvent_WhenTradeMessageReceived(string pair)
    {
        // Arrange
        var tradeMessage = $@"[1,""tu"",[123456,1612137600000,0.5,50000]]";
        
        // First send subscription confirmation
        var subscriptionMessage = $@"{{""event"":""subscribed"",""channel"":""trades"",""chanId"":1,""symbol"":""t{pair}""}}";
        await _client.ProcessMessageAsync(subscriptionMessage);

        // Act
        await _client.ProcessMessageAsync(tradeMessage);

        // Assert
        Assert.Single(_receivedTrades);
        var trade = _receivedTrades[0];
        Assert.Equal("123456", trade.Id);
        Assert.Equal(pair, trade.Pair);
        Assert.Equal(0.5m, trade.Amount);
        Assert.Equal(50000m, trade.Price);
    }

    [Theory]
    [InlineData("BTCUSD")]
    [InlineData("ETHUSD")]
    [InlineData("LTCUSD")]
    public async Task ProcessMessage_ShouldRaiseCandleEvent_WhenCandleMessageReceived(string pair)
    {
        // Arrange
        var candleMessage = $@"[2,[1612137600000,50000,51000,49000,50500,100]]";
        
        // First send subscription confirmation
        var subscriptionMessage = $@"{{""event"":""subscribed"",""channel"":""candles"",""chanId"":2,""key"":""trade:1m:t{pair}""}}";
        await _client.ProcessMessageAsync(subscriptionMessage);

        // Act
        await _client.ProcessMessageAsync(candleMessage);

        // Assert
        Assert.Single(_receivedCandles);
        var candle = _receivedCandles[0];
        Assert.Equal(pair, candle.Pair);
        Assert.Equal(50000m, candle.OpenPrice);
        Assert.Equal(51000m, candle.HighPrice);
        Assert.Equal(49000m, candle.LowPrice);
        Assert.Equal(50500m, candle.ClosePrice);
        Assert.Equal(100m, candle.TotalVolume);
    }

    [Fact]
    public async Task SubscribeToTradesAsync_ShouldSendCorrectMessage()
    {
        // Arrange
        byte[]? sentMessage = null;
        _mockWs.Setup(ws => ws.SendAsync(It.IsAny<byte[]>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .Callback<byte[], WebSocketMessageType, bool, CancellationToken>((bytes, _, _, _) => sentMessage = bytes)
            .Returns(Task.CompletedTask);

        // Act
        await _client.SubscribeToTradesAsync("BTCUSD");

        // Assert
        Assert.NotNull(sentMessage!);
        var message = System.Text.Encoding.UTF8.GetString(sentMessage);
        Assert.Contains("subscribe", message.ToLower());
        Assert.Contains("trades", message.ToLower());
        Assert.Contains("btcusd", message.ToLower());
    }

    [Fact]
    public async Task SubscribeToCandlesAsync_ShouldSendCorrectMessage()
    {
        // Arrange
        byte[]? sentMessage = null;
        _mockWs.Setup(ws => ws.SendAsync(It.IsAny<byte[]>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
            .Callback<byte[], WebSocketMessageType, bool, CancellationToken>((bytes, _, _, _) => sentMessage = bytes)
            .Returns(Task.CompletedTask);

        // Act
        await _client.SubscribeToCandlesAsync("BTCUSD", 60);

        // Assert
        Assert.NotNull(sentMessage!);
        var message = System.Text.Encoding.UTF8.GetString(sentMessage);
        Assert.Contains("subscribe", message.ToLower());
        Assert.Contains("candles", message.ToLower());
        Assert.Contains("btcusd", message.ToLower());
    }

    [Fact]
    public async Task ConnectAsync_ShouldInitiateConnection()
    {
        // Arrange
        var connected = false;
        _mockWs.Setup(ws => ws.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback(() => connected = true)
            .Returns(Task.CompletedTask);
        _mockWs.SetupSequence(ws => ws.State)
            .Returns(WebSocketState.None)  // Initial state
            .Returns(WebSocketState.Open); // After connection

        // Act
        await _client.ConnectAsync();

        // Assert
        Assert.True(connected);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldCloseConnection()
    {
        // Arrange
        var closed = false;
        _mockWs.Setup(ws => ws.CloseAsync(WebSocketCloseStatus.NormalClosure, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => closed = true)
            .Returns(Task.CompletedTask);

        // Act
        await _client.DisconnectAsync();

        // Assert
        Assert.True(closed);
    }
}

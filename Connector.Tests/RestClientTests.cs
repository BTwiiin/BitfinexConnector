using System.Net;
using Connector.API.Clients;
using Moq;
using Moq.Protected;

namespace Connector.Tests;

public class RestClientTests
{
    [Fact]
    public async Task GetTradesAsync_ShouldReturnTrades_WhenApiReturnsValidData()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var expectedTradesArray = @"[
            [123, 1621436800000, 1.0, 100.0],
            [124, 1621436800000, -2.0, 101.0]
        ]";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedTradesArray)
            });

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));

        // Act
        var trades = await client.GetTradesAsync("BTCUSD", 2);

        // Assert
        var tradesList = trades.ToList();
        Assert.Equal(2, tradesList.Count);
        
        Assert.Equal("123", tradesList[0].Id);
        Assert.Equal("BTCUSD", tradesList[0].Pair);
        Assert.Equal(100.0m, tradesList[0].Price);
        Assert.Equal(1.0m, tradesList[0].Amount);
        Assert.Equal("BUY", tradesList[0].Side);

        Assert.Equal("124", tradesList[1].Id);
        Assert.Equal(101.0m, tradesList[1].Price);
        Assert.Equal(2.0m, tradesList[1].Amount);
        Assert.Equal("SELL", tradesList[1].Side);
    }

    [Fact]
    public async Task GetTradesAsync_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid symbol")
            });

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.GetTradesAsync("INVALID", 1));
    }

    [Fact]
    public async Task GetCandleSeriesAsync_ShouldReturnCandles_WhenApiReturnsValidData()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var expectedCandlesArray = @"[
            [1621436800000, 100, 101, 105, 95, 10.5],
            [1621437600000, 101, 102, 106, 96, 11.5]
        ]";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedCandlesArray)
            });

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));  // Initialize the client properly

        // Act
        var candles = await client.GetCandleSeriesAsync("BTCUSD", 60, DateTimeOffset.UtcNow.AddHours(-1));

        // Assert
        Assert.Equal(2, candles.Count());

        Assert.Equal(100.0m, candles.First().OpenPrice);
        Assert.Equal(101.0m, candles.First().ClosePrice);
        Assert.Equal(105.0m, candles.First().HighPrice);
        Assert.Equal(95.0m, candles.First().LowPrice);
        Assert.Equal(10.5m, candles.First().TotalVolume);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1621436800000), candles.First().OpenTime);
    }


    [Fact]
    public async Task GetCandleSeriesAsync_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid timeframe")
            });

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.GetCandleSeriesAsync("BTCUSD", 60, DateTimeOffset.UtcNow.AddHours(-1)));
    }

    [Fact]
    public async Task GetTickerAsync_ShouldReturnTickerInfo_WhenApiReturnsValidData()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // Реальные данные BTC/USD на момент написания теста (2025-02-09)
        // Получены через: curl https://api-pub.bitfinex.com/v2/ticker/tBTCUSD
        // BID, BID_SIZE, ASK, ASK_SIZE, DAILY_CHANGE, DAILY_CHANGE_RELATIVE, LAST_PRICE, VOLUME, HIGH, LOW
        var expectedTickerArray = @"[
                96067.0,
                5.24148622,
                96068.0,
                4.93945737,
                -69.0,
                -0.00071773,
                96067.0,
                420.33711804,
                97366.0,
                95748.0
        ]";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedTickerArray)
            });

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));

        // Act
        var ticker = await client.GetTickerAsync("BTCUSD");

        // Assert
        Assert.NotNull(ticker);
        Assert.Equal(96067.0m, ticker.Bid);
        Assert.Equal(5.24148622m, ticker.BidSize);
        Assert.Equal(96068.0m, ticker.Ask);
        Assert.Equal(4.93945737m, ticker.AskSize);
        Assert.Equal(-69.0m, ticker.DailyChange);
        Assert.Equal(-0.00071773m, ticker.DailyChangeRelative);
        Assert.Equal(96067.0m, ticker.LastPrice);
        Assert.Equal(420.33711804m, ticker.Volume);
        Assert.Equal(97366.0m, ticker.High);
        Assert.Equal(95748.0m, ticker.Low);
    }
} 
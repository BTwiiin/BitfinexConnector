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
            [1621436800000, 100.0, 105.0, 95.0, 101.0, 10.5, 1000.0],
            [1621437600000, 101.0, 106.0, 96.0, 102.0, 11.5, 1100.0]
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

        var client = new RestClient(new HttpClient(mockHttpMessageHandler.Object));

        // Act
        var candles = await client.GetCandleSeriesAsync("BTCUSD", 60, DateTimeOffset.UtcNow.AddHours(-1));

        // Assert
        var candlesList = candles.ToList();
        Assert.Equal(2, candlesList.Count);
        
        Assert.Equal("BTCUSD", candlesList[0].Pair);
        Assert.Equal(100.0m, candlesList[0].OpenPrice);
        Assert.Equal(105.0m, candlesList[0].HighPrice);
        Assert.Equal(95.0m, candlesList[0].LowPrice);
        Assert.Equal(101.0m, candlesList[0].ClosePrice);
        Assert.Equal(10.5m, candlesList[0].TotalVolume);
        Assert.Equal(1000.0m, candlesList[0].TotalPrice);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1621436800000), candlesList[0].OpenTime);
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
        var expectedTickerArray = @"[
            [
                43986.0,           // BID
                24.31637567,       // BID_SIZE
                43987.0,           // ASK
                28.66523066,       // ASK_SIZE
                -821.0,            // DAILY_CHANGE
                -0.0183,           // DAILY_CHANGE_RELATIVE
                43986.0,           // LAST_PRICE
                1247.05793468,     // VOLUME
                45039.0,           // HIGH
                43371.0            // LOW
            ]
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
        Assert.Equal(43986.0m, ticker.Bid);
        Assert.Equal(24.31637567m, ticker.BidSize);
        Assert.Equal(43987.0m, ticker.Ask);
        Assert.Equal(28.66523066m, ticker.AskSize);
        Assert.Equal(-821.0m, ticker.DailyChange);
        Assert.Equal(-0.0183m, ticker.DailyChangeRelative);
        Assert.Equal(43986.0m, ticker.LastPrice);
        Assert.Equal(1247.05793468m, ticker.Volume);
        Assert.Equal(45039.0m, ticker.High);
        Assert.Equal(43371.0m, ticker.Low);
    }
} 
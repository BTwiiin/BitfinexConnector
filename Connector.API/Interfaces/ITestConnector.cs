namespace Connector.API.Interfaces;

using Connector.API.Models;

/// <summary>
/// Интерфейс для работы с биржевыми данными через REST и WebSocket
/// </summary>
public interface ITestConnector
{
    #region Rest

    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);

    #endregion

    #region Socket

    // Примечание: Раздельные события для покупок и продаж избыточны,
    // так как направление сделки содержится в свойстве Side модели Trade
    event Action<Trade> NewBuyTrade;
    event Action<Trade> NewSellTrade;

    /***
    * Подключение к WebSocket и подписка на потоки данных - это асинхронные операции
    * Task позволяет дождаться подтверждения успешной подписки/отписки
    * Это защищает от race conditions, когда код продолжает выполняться до завершения подписки
    ***/
    

    /// <summary>
    /// Подписка на поток сделок
    /// </summary>
    Task SubscribeTrades(string pair, int maxCount = 100);
    Task UnsubscribeTrades(string pair);

    // Примечание: CandleSeriesProcessing вызывается при обновлении свечи
    event Action<Candle> CandleSeriesProcessing;

    // Примечание: Параметры from/to/count используются для начальной загрузки исторических данных
    // и уже реализованы в методе GetCandleSeriesAsync
    // Разделение ответственности: 
    // - история через REST
    // - текущие обновления через WebSocket

    /// <summary>
    /// Подписка на поток свечей
    /// </summary>

    Task SubscribeCandles(string pair, int periodInSec);
    Task UnsubscribeCandles(string pair);

    #endregion
}


namespace Connector.API.Clients;

using System.Net.Http;
using Connector.API.Interfaces;
using Connector.API.Models;

/**
 * Базовый класс для REST-клиента
 * Реализует повторные попытки запросов с задержкой
 * Определяет абстрактные методы для получения данных
 * Используется для реализации различных бирж без дублирования кода
 */

public abstract class BaseRestClient : IRestClient
{
    private readonly HttpClient _httpClient;
    private readonly int _maxRetryAttempts = 3;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);

    protected BaseRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        for (int i = 0; i < _maxRetryAttempts; i++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException)
            {
                if (i == _maxRetryAttempts - 1)
                    throw;

                await Task.Delay(_retryDelay);
            }
        }
        
        throw new InvalidOperationException("Should not reach here");
    }

    public abstract Task<IEnumerable<Trade>> GetTradesAsync(string pair, int maxCount);
    public abstract Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = null);
    public abstract Task<Ticker> GetTickerAsync(string pair);
} 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Connector.API.Clients;

public class PortfolioCalculator
{
    private readonly RestClient _restClient;
    private Dictionary<string, decimal> _prices = new();
    private const string LogFile = "portfolio_calculator.log";

    private void Log(string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
        File.AppendAllText(LogFile, logMessage + Environment.NewLine);
    }

    public PortfolioCalculator(RestClient restClient)
    {
        _restClient = restClient;
    }

    public async Task<Dictionary<string, decimal>> CalculateBalances()
    {
        try
        {
            Log("Starting portfolio calculation");
            // Get current prices
            var pairs = new[] { "tBTCUSD", "tXRPUSD", "tXMRUSD", "tDSHUSD" };
            Log($"Requesting prices for pairs: {string.Join(", ", pairs)}");
            _prices = await _restClient.GetTickersAsync(pairs);
            Log($"Received prices: {string.Join(", ", _prices.Select(p => $"{p.Key}: {p.Value}"))}");

            // Initial balances
            var portfolio = new Dictionary<string, decimal>
            {
                { "BTC", 1m },
                { "XRP", 15000m },
                { "XMR", 50m },
                { "DSH", 30m }
            };
            Log($"Portfolio balances: {string.Join(", ", portfolio.Select(p => $"{p.Key}: {p.Value}"))}");

            // Calculate total in USD
            decimal totalUSD = 0;
            foreach (var (currency, amount) in portfolio)
            {
                var price = _prices[$"t{currency}USD"];
                var value = amount * price;
                Log($"Calculating {currency}: {amount} * {price} = {value} USD");
                totalUSD += value;
            }
            Log($"Total portfolio value: {totalUSD} USD");

            // Convert total to each currency
            var result = new Dictionary<string, decimal>
            {
                { "USD", totalUSD },
                { "BTC", totalUSD / _prices["tBTCUSD"] },
                { "XRP", totalUSD / _prices["tXRPUSD"] },
                { "XMR", totalUSD / _prices["tXMRUSD"] },
                { "DSH", totalUSD / _prices["tDSHUSD"] }
            };
            Log($"Final results: {string.Join(", ", result.Select(r => $"{r.Key}: {r.Value}"))}");

            return result;
        }
        catch (Exception ex)
        {
            Log($"Error in portfolio calculation: {ex.Message}\nStackTrace: {ex.StackTrace}");
            throw;
        }
    }
} 
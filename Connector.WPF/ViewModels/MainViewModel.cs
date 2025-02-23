using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Connector.API.Models;
using Connector.API.Clients;
using System.Diagnostics;

namespace Connector.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WebSocketClient _wsClient;
        private readonly RestClient _restClient;
        private readonly PortfolioCalculator _portfolioCalculator;
        private Dictionary<string, decimal> _portfolioBalances = new();

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _selectedPair = "BTCUSD";

        private string? _previousPair;

        public ObservableCollection<Trade> Trades { get; } = new();
        public ObservableCollection<Candle> Candles { get; } = new();
        public ObservableCollection<string> AvailablePairs { get; } = new();

        public Dictionary<string, decimal> PortfolioBalances
        {
            get => _portfolioBalances;
            set
            {
                _portfolioBalances = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(WebSocketClient wsClient, RestClient restClient, PortfolioCalculator calculator)
        {
            _wsClient = wsClient;
            _restClient = restClient;
            _portfolioCalculator = calculator;

            // Initialize available pairs
            AvailablePairs = new ObservableCollection<string> 
            { 
                "BTCUSD", 
                "ETHUSD",
                "LTCUSD" 
            };

            // Make sure SelectedPair is set to a valid value
            SelectedPair = AvailablePairs.FirstOrDefault() ?? "BTCUSD";

            _wsClient.OnTradeReceived += trade => 
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    Trades.Insert(0, trade);
                    if (Trades.Count > 100) Trades.RemoveAt(Trades.Count - 1);
                });
            };

            _wsClient.OnCandleReceived += candle =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Candles.Insert(0, candle);
                    if (Candles.Count > 100) Candles.RemoveAt(Candles.Count - 1);
                });
            };
        }

        [RelayCommand]
        private async Task Connect()
        {
            try
            {
                await _wsClient.ConnectAsync();
                await _wsClient.SubscribeToTradesAsync(SelectedPair);
                await _wsClient.SubscribeToCandlesAsync(SelectedPair, 60);
                _previousPair = SelectedPair;
                IsConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}");
                Debug.WriteLine($"Connection error: {ex}");
            }
        }

        [RelayCommand]
        private async Task Disconnect()
        {
            try
            {
                await _wsClient.DisconnectAsync();
                IsConnected = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disconnection error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            try
            {
                var trades = await _restClient.GetTradesAsync(SelectedPair, 100);
                var candles = await _restClient.GetCandleSeriesAsync(SelectedPair, 60, null, null, 100);

                Trades.Clear();
                foreach (var trade in trades)
                {
                    Trades.Add(trade);
                }

                Candles.Clear();
                foreach (var candle in candles)
                {
                    Candles.Add(candle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Data refresh error: {ex.Message}");
            }
        }

        partial void OnSelectedPairChanged(string value)
        {
            Debug.WriteLine($"Selected pair changed to: {value}");
            if (IsConnected)
            {
                Application.Current.Dispatcher.BeginInvoke(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(_previousPair))
                        {
                            await _wsClient.UnsubscribeFromTradesAsync(_previousPair);
                            await _wsClient.UnsubscribeFromCandlesAsync(_previousPair);
                            _previousPair = SelectedPair;
                        }
                        
                        await _wsClient.SubscribeToTradesAsync(value);
                        await _wsClient.SubscribeToCandlesAsync(value, 60);
                        
                        await RefreshData();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error switching pair: {ex}");
                        MessageBox.Show($"Error switching pair: {ex.Message}");
                    }
                });
            }
            else
            {
                _ = RefreshData();
            }
        }

        [RelayCommand]
        private void CleanData()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Trades.Clear();
                    Candles.Clear();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clean error: {ex.Message}");
                Debug.WriteLine($"Clean error: {ex}");
            }
        }

        [RelayCommand]
        private async Task CalculatePortfolio()
        {
            try
            {
                PortfolioBalances = await _portfolioCalculator.CalculateBalances();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Portfolio calculation error: {ex.Message}");
            }
        }
    }
} 
using System.Configuration;
using System.Data;
using System.Windows;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Connector.API.Clients;
using Connector.WPF.ViewModels;
using Connector.WPF.Views;
using Connector.API.Interfaces;

namespace Connector.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<WebSocketClient>();
        services.AddSingleton<RestClient>();
        services.AddSingleton<PortfolioCalculator>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}


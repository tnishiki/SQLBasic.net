using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SQLBasic_net;
using SQLBasic_net.Services;
using SQLBasic_net.ViewModels;
using SQLBasic_net.Views;

namespace SQLBasic_net;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IWindowProvider, WindowProvider>();
                services.AddSingleton<ICoreService, CoreService>();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();

                services.AddTransient<SetSyntax>();
                services.AddTransient<SetSyntaxViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();

        //あらかじめテーブル一覧を取得する
        var viewmodel = _host.Services.GetRequiredService<MainWindowViewModel>();
        if (viewmodel != null)
        {
            await viewmodel.GetTableNames();
        }
        // MainWindow 起動
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}

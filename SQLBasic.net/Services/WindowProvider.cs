using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SQLBasic_net.Services;

public class WindowProvider : IWindowProvider
{
    private readonly IServiceProvider _serviceProvider;

    public WindowProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    public Window? GetMainWindow()
    {
        return Application.Current?.MainWindow;
    }

    public void ShowDialog<T>(Action<Window>? configure = null) where T : Window
    {
        var window = _serviceProvider.GetRequiredService<T>();
        window.ShowDialog();
    }
}


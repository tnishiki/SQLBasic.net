using System.Windows;

namespace SQLBasic_net.Services;

public interface IWindowProvider
{
    Window? GetMainWindow();
    void ShowDialog<T>(Action<Window>? configure = null) where T : Window;
}

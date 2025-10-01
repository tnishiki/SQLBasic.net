using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SQLBasic_net;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();

        DataContext = mainWindowViewModel;
        mainWindowViewModel.win = this;
    }

    public void BuildColumns(List<string?> headers)
    {
        QueryGrid.Columns.Clear();

        foreach (var header in headers)
        {
            QueryGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(header)
            });
        }
    }
}

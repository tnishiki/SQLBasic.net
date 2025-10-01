using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLBasic_net.Datas;

public partial class SyntaxItem : ObservableObject
{
    [ObservableProperty]
    private int _No = 0;
    [ObservableProperty]
    private string _Name = string.Empty;
    [ObservableProperty]
    private Brush _Color = Brushes.White;
    [ObservableProperty]
    private string _Sample = string.Empty;
}

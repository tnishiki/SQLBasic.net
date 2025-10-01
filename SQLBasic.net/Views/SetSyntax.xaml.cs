using ColorPicker;
using ColorPicker.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SQLBasic_net.ViewModels;
using SQLBasic_net.Datas;

namespace SQLBasic_net.Views;

/// <summary>
/// SetSyntax.xaml の相互作用ロジック
/// </summary>
public partial class SetSyntax : Window
{
    public SetSyntax(SetSyntaxViewModel setSyntaxViewModel)
    {
        InitializeComponent();

        DataContext = setSyntaxViewModel;

        setSyntaxViewModel.WindowClose = this.Close;
    }

    private void ColorPickerChanged(object sender, RoutedEventArgs e)
    {
        SetSyntaxViewModel vm = (SetSyntaxViewModel)DataContext;

        if (sender is ColorSliders)
        {
            ColorSliders cp = (ColorSliders)sender;

            if (cp != null && vm.SelectSyntaxItem != null)
            {
                double r = cp.ColorState.RGB_R * 255;
                double g = cp.ColorState.RGB_G * 255;
                double b = cp.ColorState.RGB_B * 255;

                vm.SelectSyntaxItem.Color = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b));
            }

            ((SetSyntaxViewModel)DataContext).ChangeColor();
        }
    }

    private void SyntaxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SetSyntaxViewModel vm = (SetSyntaxViewModel)DataContext;

        if (e.AddedItems != null && e.AddedItems[0] is SyntaxItem && e.AddedItems != null)
        {
            var item = (SyntaxItem)e.AddedItems[0]!;

            if (item.Color is SolidColorBrush solidBrush)
            {
                vm.SelectColorPicker = new ColorState()
                {
                    A = 1.0,
                    RGB_R = solidBrush.Color.R / 255.0,
                    RGB_G = solidBrush.Color.G / 255.0,
                    RGB_B = solidBrush.Color.B / 255.0,
                };
            }
        }
    }

}

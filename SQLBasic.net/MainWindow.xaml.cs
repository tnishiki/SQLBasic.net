using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SQLBasic_net.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SQLBasic_net;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private CompletionWindow? _completionWindow;
    
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();

        SqlEditor.TextArea.KeyDown += TextArea_KeyDown;

        DataContext = mainWindowViewModel;
        mainWindowViewModel.Win = this;

        mainWindowViewModel.SqlEditor = SqlEditor;;
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
    private void TextArea_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            // Ctrl + Space 押下を検知
            if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;                // ← イベント伝播を止める

                var model = (MainWindowViewModel)DataContext;
                int caretOffset = SqlEditor.TextArea.Caret.Offset;
                var (candidates, completionHeader) = model.GetCandidateDatabaseItem(SqlEditor.Document.Text, caretOffset);
                if (candidates == null)
                {
                    return;
                }
                if (_completionWindow != null)
                {
                    _completionWindow.Close();
                    _completionWindow = null;
                }
                _completionWindow = new CompletionWindow(SqlEditor.TextArea);

                // カーソル前の入力済みプレフィックスをcompletionSegmentに含めるため、単語の先頭をStartOffsetに設定
                string documentText = SqlEditor.Document.Text;
                int wordStart = caretOffset;
                while (wordStart > 0 && (char.IsLetterOrDigit(documentText[wordStart - 1]) || documentText[wordStart - 1] == '_'))
                {
                    wordStart--;
                }
                _completionWindow.StartOffset = wordStart;

                var data = _completionWindow.CompletionList.CompletionData;
                foreach (var item in candidates)
                {
                    data.Add(new MyCompletionData(item));
                }

                // ヘッダーラベルをCompletionListの上に配置する
                // Content を先にパネルへ差し替えて CompletionList の親を解放してから追加する
                var headerLabel = new TextBlock
                {
                    Text = completionHeader,
                    Padding = new Thickness(4, 2, 4, 2),
                };
                var panel = new DockPanel();
                DockPanel.SetDock(headerLabel, Dock.Top);
                panel.Children.Add(headerLabel);
                _completionWindow.Content = panel;           // CompletionList が Window の論理ツリーから切り離される
                panel.Children.Add(_completionWindow.CompletionList); // 親なし状態になったので追加可能

                _completionWindow.Show();
                _completionWindow.Closed += (_, _) => _completionWindow = null;
            }
        }
        catch
        {
            return;
        }
    }
}

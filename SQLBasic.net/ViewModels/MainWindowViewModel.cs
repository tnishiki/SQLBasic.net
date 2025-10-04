using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Windows.Media;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using SQLBasic_net.Services;
using SQLBasic_net.Views;
using System.Text;
using CsvHelper;
using System.Globalization;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using CsvHelper.Configuration;

namespace SQLBasic_net;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private double _FontSize = 16.0;

    [ObservableProperty]
    private TextDocument _SqlDocument = new TextDocument();

    public string SQLQuery
    {
        get
        {
            return SqlDocument.Text;
        }
    }

    [ObservableProperty]
    private System.Windows.GridLength _EditorRowHeight = new System.Windows.GridLength(5, System.Windows.GridUnitType.Star);
    [ObservableProperty]
    private System.Windows.GridLength _ResultRowHeight = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star);

    [ObservableProperty]
    private Brush _EditorBackground = new SolidColorBrush(Colors.Black);
    [ObservableProperty]
    private IHighlightingDefinition? _SyntaxSet;

    [ObservableProperty]
    private ObservableCollection<string> _TableNames = new ObservableCollection<string>();

    [ObservableProperty]
    private ObservableCollection<ExpandoObject> _GridItems = new ObservableCollection<ExpandoObject>();
    [ObservableProperty]
    private int _DataNum = 0;
    [ObservableProperty]
    private string _SQLMessage = string.Empty;

    private readonly IWindowProvider windowProvider;
    private readonly ICoreService coreService;

    public MainWindow? win = null;

    public MainWindowViewModel(IWindowProvider _windowProvider, ICoreService _coreService)
    {
        windowProvider = _windowProvider;
        coreService = _coreService;
    }

    public async Task GetTableNames()
    {
        TableNames.Clear();

        if (coreService != null)
        {

            var list = await coreService.GetTableNames();
            foreach (var tname in list)
            {
                TableNames.Add(tname);
            }

            LoadSyntaxColors();
        }
    }
    private void LoadSyntaxColors()
    {
        List<string> colorList = new List<string>();

        EditorBackground = coreService.GetSyntaxColor(0);

        for (int i = 1; i < 11; i++)
        {
            colorList.Add(coreService.GetStringColorCode(coreService.GetSyntaxColor(i)));
        }

        var xml = coreService.GetSytaxXml(colorList.ToArray());
        using (var stringReader = new System.IO.StringReader(xml))
        {
            using (var reader = XmlReader.Create(stringReader))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                SyntaxSet = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
            }
        }
    }


    [RelayCommand]
    private void FontToLarge()
    {
        FontSize += 2.0;

        if (28 < FontSize)
        {
            FontSize = 28;
        }
    }
    [RelayCommand]
    private void FontToSmall()
    {
        FontSize -= 2.0;

        if (FontSize < 14)
        {
            FontSize = 14;
        }
    }

    [RelayCommand]
    private async Task Execute()
    {
        if (coreService == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SQLQuery))
        {
            return;
        }

        var (p, token) = coreService.CheckSQL(SQLQuery);

        SQLMessage = "";
        if (p != "OK")
        {
            SQLMessage = p;
            return;
        }

        if (token == TokenKind.Select)
        {
            var (headers, rows, message) = await coreService.CallDBQuery(SQLQuery);
            if (message != "")
            {
                SQLMessage = message;
                return;
            }
            if (win != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {

                    win.BuildColumns(new List<string?>());
                    GridItems.Clear();

                    win.BuildColumns(headers);
                    foreach (var row in rows)
                    {
                        dynamic expando = new ExpandoObject();
                        var dict = (IDictionary<string, object>)expando;
                        for (int i = 0; i < headers.Count; i++)
                        {
                            dict[headers[i] ?? ""] = row[i];
                        }
                        GridItems.Add(expando);
                    }
                    DataNum = GridItems.Count;

                    //結果グリッドの列幅を計算
                    // 結果グリッドの高さを自動調整
                    if (DataNum > 0)
                    {
                        // 結果がある場合：結果グリッドを開く（例：エディタ3:結果2の比率）
                        EditorRowHeight = new System.Windows.GridLength(3, System.Windows.GridUnitType.Star);
                        ResultRowHeight = new System.Windows.GridLength(2, System.Windows.GridUnitType.Star);
                    }
                    else
                    {
                        // 結果がない場合：結果グリッドを閉じる
                        EditorRowHeight = new System.Windows.GridLength(5, System.Windows.GridUnitType.Star);
                        ResultRowHeight = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star);
                    }
                });
            }
        }
        else if (token == TokenKind.Update || token == TokenKind.Delete || token == TokenKind.Insert)
        {
            var mes = await coreService.CallDBExecute(SQLQuery);

            if (mes != "OK")
            {
                SQLMessage = mes;
            }
        }
        else if (token == TokenKind.Create || token == TokenKind.Drop)
        {
            var mes = await coreService.CallDBExecute(SQLQuery);

            if (mes != "OK")
            {
                SQLMessage = mes;
            }

            await GetTableNames();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            SqlDocument.Text = "";
        });

        if (win != null)
        {
            win.BuildColumns(new List<string?>());
            DataNum = 0;

            if (GridItems != null)
            {
                GridItems.Clear();
            }
        }
    }

    [RelayCommand]
    private async Task SetSytax()
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            windowProvider.ShowDialog<SetSyntax>(w =>
            {
                w.Topmost = true;
            });

            LoadSyntaxColors();
        });

    }

    [RelayCommand]
    private void FormatDocument()
    {
        try
        {
            var text = SqlDocument.Text;

            text = SimpleSqlFormatter.Format(text); // 整形したテキストを反映

            SqlDocument.Text = text;
        }
        catch (Exception ex)
        {
            // パースできなかったときは何もしない（あるいはメッセージ表示）
            System.Diagnostics.Debug.WriteLine("Format error: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task Open()
    {
        try
        {
            // ファイルダイアログを表示
            var dialog = new OpenFileDialog
            {
                Title = "SQL ファイルを開く",
                Filter = "SQL ファイル (*.sql)|*.sql|テキスト ファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
                DefaultExt = ".sql",
                AddExtension = true,
            };
            if (dialog.ShowDialog() == true)
            {
                var text = File.ReadAllText(dialog.FileName, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                // AvalonEdit に内容をセット取得（TextDocument）
                SqlDocument.Text = text;
            }
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SQLMessage = $"ファイルを開く際にエラーが発生しました。\n{ex.Message}";
            });
        }
    }
    [RelayCommand]
    private async Task Save()
    {
        try
        {
            // 保存ダイアログを表示
            var dialog = new SaveFileDialog
            {
                Title = "SQL ファイルの保存",
                Filter = "SQL ファイル (*.sql)|*.sql|テキスト ファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
                DefaultExt = ".sql",
                FileName = $"{DateTime.Now.ToString("yyyy-MM-dd-HHmm_")}query.sql",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() == true)
            {
                // AvalonEdit の内容を取得（TextDocument）
                string sqlText = SqlDocument?.Text ?? string.Empty;

                // UTF-8（BOMなし）で保存
                File.WriteAllText(dialog.FileName, sqlText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SQLMessage = $"保存しました: {dialog.FileName}";
                });
            }
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SQLMessage = $"ファイル保存中にエラーが発生しました。\n{ex.Message}";
            });
        }
    }
    [RelayCommand]
    private async Task CsvExport()
    {
        try
        {
            if (GridItems == null || GridItems.Count == 0)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SQLMessage = "エクスポートできるデータがありません。";
                });
                return;
            }

            // ファイル保存ダイアログ
            var dialog = new SaveFileDialog
            {
                Title = "CSVファイルの保存",
                Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"{DateTime.Now.ToString("yyyy-MM-dd-HHmm_")}query.csv",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
                return;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,              // ヘッダー行を出力
                ShouldQuote = (field) => true, // 常にダブルクォーテーションで囲む
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            };
            // UTF-8（BOM付き）で保存（Excel互換性のため）
            using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            using var csv = new CsvWriter(writer, config);
            bool headerWritten = false;

            // GridItems は ExpandoObject のコレクション
            foreach (IDictionary<string, object?> record in GridItems)
            {
                if (!headerWritten)
                {
                    foreach (var header in record.Keys)
                    {
                        csv.WriteField(header);
                    }
                    csv.NextRecord();
                    headerWritten = true;
                }

                foreach (var kv in record)
                {
                    csv.WriteField(kv.Value);
                }
                csv.NextRecord();
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SQLMessage = $"CSVとしてエクスポートしました: {dialog.FileName}";
            });
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SQLMessage = $"CSVエクスポート中にエラーが発生しました。\n{ex.Message}";
            });
        }
    }

    public IEnumerable<string>? GetCandicateDatabaseItem(string documentText, int caretOffset)
    {
        if (coreService == null || string.IsNullOrEmpty(documentText))
        {
            return null;
        }
        var candicate = coreService.GetCandicateDatabaseItem(documentText, caretOffset);

        return candicate;
    }
}

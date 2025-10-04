using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows.Media;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using SQLBasic_net.Services;
using SQLBasic_net.Views;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Principal;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.RegularExpressions;

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

    public IEnumerable<string>? OnTextChanged(string documentText, int caretOffset)
    {
        if (coreService == null)
        {
            return null;
        }

        var context = GetContextAroundCaret(documentText, caretOffset);

        // "." が入力されたらカラム補完
        // "." 直後ならカラム補完
        if (context.EndsWith("."))
        {
            var tableName = GetTableNameBeforeDot(documentText, caretOffset);
            return coreService.GetColumnNamesOnEditor(tableName);
        }
        // FROM 直後にテーブル補完
        else if (Regex.IsMatch(documentText, @"\bFROM\s+[a-zA-Z_]*$", RegexOptions.IgnoreCase))
        {
            return coreService.GetTableNamesOnEditor();
        }
        return null;
    }
    // カーソル前後の一部文字列を取得（文脈判定用）
    private string GetContextAroundCaret(string text, int caretOffset, int contextLength = 6)
    {
        if (caretOffset <= 0 || string.IsNullOrEmpty(text))
            return string.Empty;

        int start = Math.Max(0, caretOffset - contextLength);
        int length = Math.Min(contextLength, text.Length - start);
        return text.Substring(start, length);
    }

    private string GetTableNameBeforeDot(string text,int caretOffset)
    {
        // caret の直前 50 文字くらいからテーブル名候補を探す
        int start = Math.Max(0, caretOffset - 50);
        string context = text.Substring(start, caretOffset - start);

        // 例: "FROM student." → student
        var match = Regex.Match(context, @"([a-zA-Z0-9_]+)\s*\.$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
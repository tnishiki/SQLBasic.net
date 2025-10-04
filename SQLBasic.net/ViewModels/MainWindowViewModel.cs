using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
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
    #region 補完候補用のキーワード
    private static readonly HashSet<string> SqlClauseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "on",
        "using",
        "where",
        "group",
        "order",
        "inner",
        "left",
        "right",
        "full",
        "outer",
        "cross",
        "join"
    };
    #endregion

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
        if (coreService == null || string.IsNullOrEmpty(documentText))
        {
            return null;
        }

        caretOffset = Math.Clamp(caretOffset, 0, documentText.Length);
        string textBeforeCaret = documentText.Substring(0, caretOffset);

        var tableAliases = ParseTableAliases(documentText);

        // ピリオドで区切られたカラム補完
        var dotMatch = Regex.Match(textBeforeCaret, @"([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]*)$", RegexOptions.IgnoreCase);
        if (dotMatch.Success)
        {
            var alias = dotMatch.Groups[1].Value;
            var columnPrefix = dotMatch.Groups[2].Value;

            var tableName = ResolveTableName(tableAliases, alias);
            if (!string.IsNullOrEmpty(tableName))
            {
                var filteredColumns = FilterByPrefix(coreService.GetColumnNamesOnEditor(tableName), columnPrefix);
                return filteredColumns.Count > 0 ? filteredColumns : null;
            }

            return null;
        }

        // FROM 直後はテーブル候補
        var fromMatch = Regex.Match(textBeforeCaret, @"\bfrom\s+([a-zA-Z0-9_]*)$", RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var tablePrefix = fromMatch.Groups[1].Value;
            var filteredTables = FilterByPrefix(coreService.GetTableNamesOnEditor(), tablePrefix);
            return filteredTables.Count > 0 ? filteredTables : null;
        }

        // SELECT ～ FROM の間ではカラム候補
        var selectMatch = Regex.Match(textBeforeCaret, @"\bselect\b", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        if (selectMatch.Success)
        {
            var afterSelect = textBeforeCaret.Substring(selectMatch.Index + selectMatch.Length);
            if (!Regex.IsMatch(afterSelect, @"\bfrom\b", RegexOptions.IgnoreCase))
            {
                var prefixMatch = Regex.Match(textBeforeCaret, @"([a-zA-Z0-9_]*)$");
                var columnPrefix = prefixMatch.Success ? prefixMatch.Groups[1].Value : string.Empty;

                var tablesForColumns = tableAliases.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (tablesForColumns.Count == 0)
                {
                    return null;
                }

                var allColumns = new List<string>();
                foreach (var table in tablesForColumns)
                {
                    allColumns.AddRange(coreService.GetColumnNamesOnEditor(table));
                }

                var filteredColumns = FilterByPrefix(allColumns, columnPrefix);
                return filteredColumns.Count > 0 ? filteredColumns : null;
            }
        }

        return null;
    }

    private Dictionary<string, string> ParseTableAliases(string text)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
        {
            return aliases;
        }

        foreach (Match match in Regex.Matches(text, @"\b(from|join)\s+([a-zA-Z0-9_]+)(?:\s+(?:as\s+)?([a-zA-Z0-9_]+))?", RegexOptions.IgnoreCase))
        {
            AddAlias(aliases, match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : null);
        }

        foreach (Match match in Regex.Matches(text, @",\s*([a-zA-Z0-9_]+)(?:\s+(?:as\s+)?([a-zA-Z0-9_]+))?", RegexOptions.IgnoreCase))
        {
            AddAlias(aliases, match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null);
        }

        return aliases;
    }

    private void AddAlias(Dictionary<string, string> aliases, string tableName, string? alias)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return;
        }

        tableName = tableName.Trim();
        if (!aliases.ContainsKey(tableName))
        {
            aliases[tableName] = tableName;
        }

        if (string.IsNullOrWhiteSpace(alias))
        {
            return;
        }

        alias = alias.Trim();
        if (!SqlClauseKeywords.Contains(alias) && !aliases.ContainsKey(alias))
        {
            aliases[alias] = tableName;
        }
    }

    private string? ResolveTableName(Dictionary<string, string> aliases, string aliasOrTable)
    {
        if (string.IsNullOrWhiteSpace(aliasOrTable))
        {
            return null;
        }

        if (aliases.TryGetValue(aliasOrTable, out var tableName))
        {
            return tableName;
        }

        return aliasOrTable;
    }

    private List<string> FilterByPrefix(IEnumerable<string> candidates, string prefix)
    {
        var results = new List<string>();
        if (candidates == null)
        {
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(prefix) && !candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (seen.Add(candidate))
            {
                results.Add(candidate);
            }
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }
}

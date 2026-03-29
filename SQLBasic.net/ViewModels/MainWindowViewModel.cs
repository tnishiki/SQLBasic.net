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
using SQLBasic_net.Datas;
using SQLBasic_net.Services;
using System.Collections.Generic;
using SQLBasic_net.Views;
using System.Text;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using System.Diagnostics;
using ICSharpCode.AvalonEdit;
using System.Text.RegularExpressions;

namespace SQLBasic_net;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private double _fontSize = 16.0;

    [ObservableProperty]
    private TextDocument _sqlDocument = new TextDocument();

    public string SqlQuery
    {
        get
        {
            return SqlDocument.Text;
        }
    }

    [ObservableProperty]
    private System.Windows.GridLength _editorRowHeight = new System.Windows.GridLength(5, System.Windows.GridUnitType.Star);
    [ObservableProperty]
    private System.Windows.GridLength _resultRowHeight = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star);

    [ObservableProperty]
    private Brush _editorBackground = new SolidColorBrush(Colors.Black);
    [ObservableProperty]
    private IHighlightingDefinition? _syntaxSet;

    [ObservableProperty]
    private ObservableCollection<string> _tableNames = new ObservableCollection<string>();

    [ObservableProperty]
    private string? _selectedTableName;

    partial void OnSelectedTableNameChanged(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        _ = LoadColumnInfosAsync(value);
    }

    private async Task LoadColumnInfosAsync(string tableName)
    {
        var infos = await _coreService.GetColumnInfosAsync(tableName);
        ColumnInfos.Clear();
        foreach (var info in infos)
            ColumnInfos.Add(info);
    }

    [ObservableProperty]
    private ObservableCollection<ColumnInfo> _columnInfos = new ObservableCollection<ColumnInfo>();

    [ObservableProperty]
    private ObservableCollection<ExpandoObject> _gridItems = new ObservableCollection<ExpandoObject>();
    [ObservableProperty]
    private int _dataNum = 0;
    [ObservableProperty]
    private string _sqlMessage = string.Empty;

    private readonly IWindowProvider _windowProvider;
    private readonly ICoreService _coreService;

    public MainWindow? Win = null;

    public TextEditor? SqlEditor = null;

    public List<LanguageItem> LanguageItems { get; } = new()
    {
        new() { Code = "ja", Display = "日本語" },
        new() { Code = "en", Display = "English" },
        new() { Code = "zh-CN", Display = "简体中文" },
        new() { Code = "es", Display = "Español" },
    };

    [ObservableProperty]
    private LanguageItem? _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (value != null)
            LocalizationManager.Instance.SetLanguage(value.Code);
    }

    public MainWindowViewModel(IWindowProvider windowProvider, ICoreService coreService)
    {
        _windowProvider = windowProvider;
        _coreService = coreService;
        _selectedLanguage = LanguageItems[0];
    }

    public async Task GetTableNamesAsync()
    {
        TableNames.Clear();

        if (_coreService != null)
        {

            var list = await _coreService.GetTableNamesAsync();
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

        EditorBackground = _coreService.GetSyntaxColor(0);

        for (int i = 1; i < 11; i++)
        {
            colorList.Add(_coreService.GetStringColorCode(_coreService.GetSyntaxColor(i)));
        }

        var xml = _coreService.GetSyntaxXml(colorList.ToArray());
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

    private string GetCurrentQuery()
    {
        var fullText = SqlDocument.Text;
        if (string.IsNullOrWhiteSpace(fullText)) return string.Empty;

        var caretOffset = Math.Clamp(SqlEditor?.TextArea.Caret.Offset ?? 0, 0, fullText.Length);

        // カーソルより前の最後のセミコロンを探す
        int lastSemi = caretOffset > 0 ? fullText.LastIndexOf(';', caretOffset - 1) : -1;
        int start = lastSemi < 0 ? 0 : lastSemi + 1;

        // カーソル以降の最初のセミコロンを探す
        int nextSemi = fullText.IndexOf(';', caretOffset);
        int end = nextSemi < 0 ? fullText.Length : nextSemi;

        if (start >= end) return string.Empty;
        return fullText.Substring(start, end - start).Trim();
    }

    private static (string ObjectType, string ObjectName) ParseDdlTarget(string sql, bool isCreate)
    {
        // CREATE [TEMP[ORARY]] TABLE|INDEX|VIEW|TRIGGER [IF NOT EXISTS] name
        // DROP TABLE|INDEX|VIEW|TRIGGER [IF EXISTS] name
        var pattern = isCreate
            ? @"\bCREATE\s+(?:TEMP(?:ORARY)?\s+)?(TABLE|UNIQUE\s+INDEX|INDEX|VIEW|TRIGGER)\s+(?:IF\s+NOT\s+EXISTS\s+)?([a-zA-Z0-9_]+)"
            : @"\bDROP\s+(TABLE|INDEX|VIEW|TRIGGER)\s+(?:IF\s+EXISTS\s+)?([a-zA-Z0-9_]+)";

        var match = Regex.Match(sql, pattern, RegexOptions.IgnoreCase);
        if (!match.Success) return (LocalizationManager.Instance["Obj_Default"], "");

        var L = LocalizationManager.Instance;
        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TABLE"]        = L["Obj_Table"],
            ["INDEX"]        = L["Obj_Index"],
            ["UNIQUE INDEX"] = L["Obj_UniqueIndex"],
            ["VIEW"]         = L["Obj_View"],
            ["TRIGGER"]      = L["Obj_Trigger"],
        };

        var rawType = Regex.Replace(match.Groups[1].Value.Trim(), @"\s+", " ");
        var objectType = typeMap.TryGetValue(rawType, out var jp) ? jp : rawType;
        var objectName = match.Groups[2].Value;

        return (objectType, objectName);
    }

    [RelayCommand]
    private async Task Execute()
    {
        if (_coreService == null)
        {
            return;
        }

        var currentQuery = GetCurrentQuery();

        if (string.IsNullOrWhiteSpace(currentQuery))
        {
            return;
        }

        var (p, token) = _coreService.CheckSql(currentQuery);

        SqlMessage = "";
        if (p != "OK")
        {
            SqlMessage = p;
            return;
        }

        if (token == TokenKind.Select)
        {
            var (headers, rows, message) = await _coreService.CallDbQueryAsync(currentQuery);
            if (message != "")
            {
                SqlMessage = message;
                return;
            }
            if (Win != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {

                    Win.BuildColumns(new List<string?>());
                    GridItems.Clear();

                    Win.BuildColumns(headers);
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

                    SqlMessage = string.Format(LocalizationManager.Instance["Msg_SelectHit"], DataNum);
                });
            }
        }
        else if (token == TokenKind.Insert)
        {
            var (affectedRows, mes) = await _coreService.CallDbExecuteAsync(currentQuery);
            if (mes != "")
            {
                SqlMessage = mes;
            }
            else
            {
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_InsertDone"], affectedRows);
            }
        }
        else if (token == TokenKind.Update)
        {
            var (affectedRows, mes) = await _coreService.CallDbExecuteAsync(currentQuery);
            if (mes != "")
            {
                SqlMessage = mes;
            }
            else
            {
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_UpdateDone"], affectedRows);
            }
        }
        else if (token == TokenKind.Delete)
        {
            var (affectedRows, mes) = await _coreService.CallDbExecuteAsync(currentQuery);
            if (mes != "")
            {
                SqlMessage = mes;
            }
            else
            {
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_DeleteDone"], affectedRows);
            }
        }
        else if (token == TokenKind.Create)
        {
            var (_, mes) = await _coreService.CallDbExecuteAsync(currentQuery);
            if (mes != "")
            {
                SqlMessage = mes;
            }
            else
            {
                var (objectType, objectName) = ParseDdlTarget(currentQuery, isCreate: true);
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_CreateDone"], objectType, objectName);
            }
            await GetTableNamesAsync();
        }
        else if (token == TokenKind.Drop)
        {
            var (_, mes) = await _coreService.CallDbExecuteAsync(currentQuery);
            if (mes != "")
            {
                SqlMessage = mes;
            }
            else
            {
                var (objectType, objectName) = ParseDdlTarget(currentQuery, isCreate: false);
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_DropDone"], objectType, objectName);
            }
            await GetTableNamesAsync();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            SqlDocument.Text = "";
        });

        if (Win != null)
        {
            Win.BuildColumns(new List<string?>());
            DataNum = 0;

            if (GridItems != null)
            {
                GridItems.Clear();
            }
        }
    }

    [RelayCommand]
    private async Task SetSyntax()
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _windowProvider.ShowDialog<SetSyntax>(w =>
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
                Title = LocalizationManager.Instance["Dlg_OpenSqlTitle"],
                Filter = LocalizationManager.Instance["Dlg_OpenSqlFilter"],
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
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_OpenError"], ex.Message);
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
                Title = LocalizationManager.Instance["Dlg_SaveSqlTitle"],
                Filter = LocalizationManager.Instance["Dlg_SaveSqlFilter"],
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
                    SqlMessage = string.Format(LocalizationManager.Instance["Msg_SaveDone"], dialog.FileName);
                });
            }
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_SaveError"], ex.Message);
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
                    SqlMessage = LocalizationManager.Instance["Msg_NoExportData"];
                });
                return;
            }

            // ファイル保存ダイアログ
            var dialog = new SaveFileDialog
            {
                Title = LocalizationManager.Instance["Dlg_SaveCsvTitle"],
                Filter = LocalizationManager.Instance["Dlg_SaveCsvFilter"],
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
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_ExportDone"], dialog.FileName);
            });

            if (File.Exists(dialog.FileName))
            {
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SqlMessage = string.Format(LocalizationManager.Instance["Msg_ExportError"], ex.Message);
            });
        }
    }

    public (IEnumerable<string>? Candidates, string Header) GetCandidateDatabaseItem(string documentText, int caretOffset)
    {
        if (_coreService == null || string.IsNullOrEmpty(documentText))
            return (null, "");
        return _coreService.GetCandidateDatabaseItem(documentText, caretOffset);
    }

    [RelayCommand]
    private void SetComment()
    {
        if (SqlEditor == null) return;

        var (text, caretpos) = _coreService.SetSqlComment(SqlDocument.Text,
            SqlEditor.CaretOffset,
            SqlEditor.SelectionStart,
            SqlEditor.SelectionLength);

        if (text != null)
        {
            SqlDocument.Text = text;
        }
        SqlEditor.CaretOffset = caretpos;
    }

    [RelayCommand]
    private void RemoveComment()
    {
        if (SqlEditor == null) return;

        var (text, caretpos) = _coreService.RemoveSqlComment(SqlDocument.Text,
            SqlEditor.CaretOffset,
            SqlEditor.SelectionStart,
            SqlEditor.SelectionLength);

        if (text != null)
        {
            SqlDocument.Text = text;
        }
        SqlEditor.CaretOffset = caretpos;
    }
    [RelayCommand]
    private async Task ConnectDb()
    {
        // ファイルダイアログを表示
        var dialog = new OpenFileDialog
        {
            Title = LocalizationManager.Instance["Dlg_OpenDbTitle"],
            Filter = LocalizationManager.Instance["Dlg_OpenDbFilter"],
            DefaultExt = ".sql",
            AddExtension = true,
        };
        if (dialog.ShowDialog() == true)
        {
            _coreService.ConnectToDb(dialog.FileName);
            await GetTableNamesAsync();
        }
    }
}
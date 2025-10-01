using System.Data;
using System.IO;
using System.Windows.Media;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace SQLBasic_net.Services;
public class CoreService : ICoreService
{
    #region define parameters
    private const string PublisherName = "oecu";
    private const string ProductName = "SQLBasic_net";
    private const string RegistryBasePath = $@"Software\{PublisherName}\{ProductName}";
    private const string RegistryServiceAccountsPath = $@"{RegistryBasePath}\ServiceAccounts";
    private const string RegistrySyntaxPath = $@"{RegistryBasePath}\Syntax";
    private const string RegistryDbPath = $@"{RegistryBasePath}\DB";
    private const string JsonValueName = "json";
    #endregion

    #region Colors
    readonly Brush[] EditColor =
        {
    // 背景 (黒に近いダークグレー)
    new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e)),

    // 数値 (水色系)
    new SolidColorBrush(Color.FromArgb(0xff, 0x4f, 0xc1, 0xff)),

    // コメント (グレー寄り緑)
    new SolidColorBrush(Color.FromArgb(0xff, 0x57, 0xa6, 0x4a)),

    // 句読点 (明るめグレー)
    new SolidColorBrush(Color.FromArgb(0xff, 0xd4, 0xd4, 0xd4)),

    // 文字列 (オレンジ)
    new SolidColorBrush(Color.FromArgb(0xff, 0xd6, 0x9d, 0x85)),

    // ラベル (黄緑)
    new SolidColorBrush(Color.FromArgb(0xff, 0xb5, 0xce, 0xa8)),

    // キーワード (青)
    new SolidColorBrush(Color.FromArgb(0xff, 0x4d, 0x93, 0xf7)),

    // 関数 (青)
    new SolidColorBrush(Color.FromArgb(0xff, 0x2e, 0xc4, 0xb6)),

    // カラム (白)
    new SolidColorBrush(Color.FromArgb(0xff, 0xd4, 0xd4, 0xd4)),

    // コメント内キーワード1 (黄色)
    new SolidColorBrush(Color.FromArgb(0xff, 0xd7, 0xba, 0x7d)),

    // コメント内キーワード2 (シアン)
    new SolidColorBrush(Color.FromArgb(0xff, 0x4e, 0xc9, 0xb0))
    };
    #endregion

    #region Keyword
    private readonly string[] SyntaxKeywords =
    {
        "ABORT","ACTION","ADD","AFTER","ALL","ALTER","ANALYZE","AND","AS","ASC","ATTACH","AUTOINCREMENT",
        "BEFORE","BEGIN","BETWEEN","BY",
        "CASCADE","CASE","CAST","CHECK","COLLATE","COLUMN","COMMIT","CONFLICT","CONSTRAINT","CREATE","CROSS",
        "CURRENT_DATE","CURRENT_TIME","CURRENT_TIMESTAMP","DATABASE","DEFAULT","DEFERRABLE","DEFERRED","DELETE",
        "DESC","DETACH","DISTINCT","DO","DROP",
        "EACH","ELSE","END","ESCAPE","EXCEPT","EXCLUDE","EXCLUSIVE","EXISTS","EXPLAIN",
        "FAIL","FILTER","FIRST","FOLLOWING","FOR","FOREIGN","FROM","FULL",
        "GENERATED","GLOB","GROUP","GROUPS","HAVING",
        "IF","IGNORE","IMMEDIATE","IN","INDEX","INDEXED","INITIALLY","INNER","INSERT","INSTEAD","INTERSECT","INTO",
        "IS","ISNULL","JOIN","KEY","LAST","LEFT","LIKE","LIMIT","MATCH","MATERIALIZED","NATURAL","NO","NOT","NOTHING",
        "NOTNULL","NULL","NULLS","OF","OFFSET","ON","OR","ORDER","OTHERS","OUTER","OVER",
        "PARTITION","PLAN","PRAGMA","PRECEDING","PRIMARY","QUERY","RAISE","RANGE","RECURSIVE","REFERENCES","REGEXP",
        "REINDEX","RELEASE","RENAME","REPLACE","RESTRICT","RETURNING","RIGHT","ROLLBACK","ROW","ROWS",
        "SAVEPOINT","SELECT","SET","TABLE","TEMP","TEMPORARY","THEN","TIES","TO","TRANSACTION","TRIGGER",
        "UNBOUNDED","UNION","UNIQUE","UPDATE","USING","VACUUM","VALUES","VIEW","VIRTUAL","WHEN","WHERE","WINDOW","WITH","WITHOUT"
    };
    #endregion

    private readonly IWindowProvider _windowProvider;
    public string connectionString = string.Empty;
    public CoreService(IWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;
        if (!CheckLocalDB())
        {
        }
    }
    public bool CheckLocalDB()
    {
        bool result = false;
        try
        {
            var dbKey = Registry.CurrentUser.OpenSubKey(RegistryDbPath, writable: true);
            if (dbKey == null)
            {
                dbKey = Registry.CurrentUser.CreateSubKey(RegistryDbPath);
            }
            var folderPath = dbKey?.GetValue("FolderPath") as string;

            //DB ファイルを設置するフォルダを確認する
            if (folderPath == null)
            {
                folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"{PublisherName}",
                $"{ProductName}");
                dbKey?.SetValue("FolderPath", folderPath, RegistryValueKind.String);
            }

            //データベース設定
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string dbPath = Path.Combine(folderPath, "local.db");
            connectionString = $"Data Source={dbPath}";
            if (System.IO.File.Exists(dbPath))
            {
                //DB ファイルはすでに存在している
                result = true;
                return result;
            }
            else
            {
                // データベースとテーブルの作成
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    connectionString = $"Data Source={dbPath}";
                    result = true;
                }
            }
        }
        catch
        {
            throw new Exception("DB ファイルの作成に失敗しました");
        }
        return result;
    }
    public Brush GetSyntaxColor(int SyntaxNo)
    {
        try
        {
            var dbKey = Registry.CurrentUser.OpenSubKey(RegistrySyntaxPath, writable: true);
            if (dbKey == null)
            {
                dbKey = Registry.CurrentUser.CreateSubKey(RegistrySyntaxPath);
            }
            string? col = dbKey.GetValue($"Color{SyntaxNo}") as string;
            if (string.IsNullOrWhiteSpace(col))
            {
                var str = GetStringColorCode(EditColor[SyntaxNo]);
                dbKey.SetValue($"Color{SyntaxNo}",str);
                return EditColor[SyntaxNo];
            }
            else
            {
                var b = new BrushConverter().ConvertFromString(col);
                if (b == null)
                {
                    var str = GetStringColorCode(EditColor[SyntaxNo]);
                    dbKey.SetValue($"Color{SyntaxNo}", str);
                    return EditColor[SyntaxNo];
                }
                return (Brush)b;
            }
        }
        catch
        {
            return EditColor[SyntaxNo];
        }
    }
    public bool SetSyntaxColor(int SyntaxNo, Brush brush)
    {
        try
        {
            if (brush is SolidColorBrush solidBrush)
            {
                Color color = solidBrush.Color;
                string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                var dbKey = Registry.CurrentUser.OpenSubKey(RegistrySyntaxPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RegistrySyntaxPath);
                dbKey?.SetValue($"Color{SyntaxNo}", hex, RegistryValueKind.String);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    public string GetStringColorCode(Brush brush)
    {
        if (brush is SolidColorBrush solidBrush)
        {
            return $"#{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
        }
        else {
            return "#FF000000";
        }
    }

    public string GetSytaxXml(string[]? colors)
    {
        if (colors == null || colors.Length < 10)
        {
            colors = new string[]
            {
                "#FF00008B",//数値
                "#FF008000",//コメント
                "#FFFF0000",//句読点
                "#FF808000",//文字列
                "#FF191970",//ラベル
                "#FF999933",//キーワード
                "#FF3300FF",//関数
                "#FF008080",//
                "#FFFF0000",//
                "#FFE0E000",//
            };
        }

        #region xml
        string xml1 = $@"<SyntaxDefinition name=""SQL"" extensions="".sql"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
  <Color name=""Digits"" foreground=""{colors[0]}"" exampleText=""3.1415f"" />
  <Color name=""Comment"" foreground=""{colors[1]}"" exampleText=""string text = &quot;Hello, World!&quot;"" />
  <Color name=""Punctuation"" foreground=""{colors[2]}"" exampleText=""string text = &quot;Hello, World!&quot;"" />
  <Color name=""String"" foreground=""{colors[3]}"" exampleText=""string text = &quot;Hello, World!&quot;"" />
  <Color name=""Label"" foreground=""{colors[4]}"" exampleText=""string text = &quot;Hello, World!&quot;"" />
  <Color name=""Keyword"" foreground=""{colors[5]}"" fontWeight=""bold"" exampleText=""SELECT"" />
  <Color name=""MethodCall"" foreground=""{colors[6]}"" fontWeight=""bold"" />
  <Color name=""ObjectReference"" foreground=""{colors[7]}"" exampleText=""Customer.Name"" />
  <Color name=""CommentMarkerSetTodo"" foreground=""{colors[8]}"" fontWeight=""bold"" />
  <Color name=""CommentMarkerSetHackUndone"" foreground=""{colors[9]}"" fontWeight=""bold"" />
  <RuleSet name=""CommentMarkerSet"">
    <Keywords color=""CommentMarkerSetTodo"">
      <Word>TODO</Word>
      <Word>FIXME</Word>
    </Keywords>
    <Keywords color=""CommentMarkerSetHackUndone"">
      <Word>HACK</Word>
      <Word>UNDONE</Word>
    </Keywords>
  </RuleSet>
  <RuleSet ignoreCase=""true"">
    <Span color=""String"" multiline=""true"">
      <Begin>'</Begin>
      <End>'</End>
    </Span>
    <Span color=""Label"" multiline=""true"">
      <Begin>""</Begin>
      <End>""</End>
    </Span>
    <Span color=""Comment"" begin=""--"" end=""&#x0A;"" ruleSet=""CommentMarkerSet"" />
    <Span color=""Comment"" ruleSet=""CommentMarkerSet"" multiline=""true"">
      <Begin>/\*</Begin>
      <End>\*/</End>
    </Span>
   <Keywords color=""Keyword"">
        ";
        string xml2 = $@"   </Keywords>
    <Rule color=""ObjectReference"">([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_$]*)</Rule>
    <Rule color=""Punctuation"">
      [,.;()\[\]{{}}]
    </Rule>
    <Rule color=""MethodCall"">[A-Za-z_][A-Za-z0-9_]*(?=\s*\()</Rule>
    <Rule color=""Digits"">
      \b0[xX][0-9a-fA-F]+ 
      |
      (    \b\d+(\.[0-9]+)?
      |    \.[0-9]+
      )
      ([eE][+-]?[0-9]+)?
    </Rule>
  </RuleSet>
</SyntaxDefinition>
        ";
        #endregion

        string xml = xml1;
        foreach (var k in SyntaxKeywords)
        {
            xml += $"      <Word>{k}</Word>\r\n";
        }
        xml += xml2;
        return xml;
    }
    public async Task<List<string>> GetTableNames()
    {
        // データベースとテーブルの作成
        using (var connection = new SqliteConnection(connectionString))
        {
            #region sql001
            string sql001 = @"
SELECT name FROM sqlite_master  WHERE type = 'table'   AND name NOT LIKE 'sqlite_%' ORDER BY name;"
            ;
            #endregion

            await connection.OpenAsync();
            var list = await connection.QueryAsync<string>(sql001);
            List<string> result = new List<string>();
            foreach (var item in list)
            {
                result.Add(item);
            }
            return result;
        }
    }
    public (string, TokenKind tokenKind) CheckSQL(string SQL)
    {
        try
        {
            var p = new Parser(SQL);
            var statement = p.ParseStatement();
            var lex = new Lexer(SQL);
            Token firstToken = lex.NextToken();
            if (firstToken.Kind == TokenKind.Select)
            {
                return ("OK", TokenKind.Select);
            }
            switch (firstToken.Kind)
            {
                case TokenKind.Update:
                case TokenKind.Delete:
                case TokenKind.Insert:
                case TokenKind.Create:
                case TokenKind.Drop:
                    return ("OK", firstToken.Kind);
            }
            return ("OK", TokenKind.False);
        }
        catch (Exception err)
        {
            return (err.Message, TokenKind.EOF);
        }
    }
    public async Task<(List<string?> Headers, List<object[]> Rows, string Message)> CallDBQuery(string SQL)
    {
        string Message = "";
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                List<string?> header = new List<string?>();
                using (var hcom = connection.CreateCommand())
                {
                    hcom.CommandText = SQL;
                    var hreader = await hcom.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
                    var schema = hreader.GetSchemaTable();
                    if (schema != null)
                    {
                        header = schema.Rows.Cast<DataRow>()
                        .Select(r => r["ColumnName"].ToString())
                        .ToList();
                    }
                }
                IEnumerable<dynamic> reader = await connection.QueryAsync(SQL);
                var rows = new List<object[]>();
                foreach (var record in reader)
                {
                    if (record is IDictionary<string, object> rowDict)
                    {
                        rows.Add(rowDict.Values.ToArray());
                    }
                }
                return (header, rows, Message);
            }
        }
        catch (Exception err)
        {
            return (new List<string?>(), new List<object[]>(), err.Message);
        }
    }
    public async Task<string> CallDBExecute(string SQL)
    {
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(SQL);

                return "OK";
            }
        }
        catch (Exception err)
        {
            return err.Message;
        }
    }
}

using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using SQLBasic_net;
using SQLBasic_net.Datas;
using SQLBasic_net.Services;

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

    public string GetRegistryBasePath() => RegistryBasePath;

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
    public bool CheckOtherDB(string dbfilePath)
    {
        bool result = false;
        try
        {
            //データベース設定
            if (!File.Exists(dbfilePath))
            {
                return result;
            }
            connectionString = $"Data Source={dbfilePath}";
            // データベースとテーブルの作成
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                result = true;
            }
        }
        catch
        {
            throw new Exception("DB ファイルに接続できませんでした");
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
                dbKey.SetValue($"Color{SyntaxNo}", str);
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
        else
        {
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
    public async Task<(int AffectedRows, string Message)> CallDBExecute(string SQL)
    {
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var affectedRows = await connection.ExecuteAsync(SQL);
                return (affectedRows, "");
            }
        }
        catch (Exception err)
        {
            return (0, err.Message);
        }
    }
    private IEnumerable<string> GetTableNamesOnEditor()
    {
        var tables = new List<string>();

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        catch
        {
            throw new Exception("DB への接続に失敗しました");
        }
    }
    private IEnumerable<string> GetColumnNamesOnEditor(string tableName)
    {
        var columns = new List<string>();
        if (string.IsNullOrWhiteSpace(tableName))
            return columns;
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = new SqliteCommand($"PRAGMA table_info([{tableName}]);", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                columns.Add(reader.GetString(1)); // name列（1番目のカラム名）
            }
        }
        catch
        {
            throw new Exception("DB への接続に失敗しました");
        }
        return columns;
    }
    public async Task<List<ColumnInfo>> GetColumnInfos(string tableName)
    {
        var result = new List<ColumnInfo>();
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(connectionString))
            return result;
        try
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand($"PRAGMA table_info([{tableName}]);", connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ColumnInfo
                {
                    Name       = reader.GetString(1),
                    DataType   = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    IsNullable = reader.GetInt32(3) == 0 ? "○" : "×",
                });
            }
        }
        catch { }
        return result;
    }

    public (IEnumerable<string>? Candidates, string Header) GetCandicateDatabaseItem(string documentText, int caretOffset)
    {
        if (string.IsNullOrEmpty(documentText))
            return (null, "");

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
                var filteredColumns = FilterByPrefix(GetColumnNamesOnEditor(tableName), columnPrefix);
                return filteredColumns.Count > 0 ? (filteredColumns, LocalizationManager.Instance["Comp_ColumnHeader"]) : (null, "");
            }
            return (null, "");
        }

        // FROM / JOIN / UPDATE / INSERT INTO 直後はテーブル候補
        var tableKeywordMatch = Regex.Match(textBeforeCaret,
            @"\b(?:from|join|update|into)\s+([a-zA-Z0-9_]*)$", RegexOptions.IgnoreCase);
        if (tableKeywordMatch.Success)
        {
            var tablePrefix = tableKeywordMatch.Groups[1].Value;
            var filteredTables = FilterByPrefix(GetTableNamesOnEditor(), tablePrefix);
            return filteredTables.Count > 0 ? (filteredTables, LocalizationManager.Instance["Comp_TableHeader"]) : (null, "");
        }

        // DROP TABLE / ALTER TABLE / CREATE TABLE など TABLE キーワード直後はテーブル候補
        var afterTableMatch = Regex.Match(textBeforeCaret,
            @"\btable\s+(?:if\s+(?:not\s+)?exists\s+)?([a-zA-Z0-9_]*)$", RegexOptions.IgnoreCase);
        if (afterTableMatch.Success)
        {
            var tablePrefix = afterTableMatch.Groups[1].Value;
            var filteredTables = FilterByPrefix(GetTableNamesOnEditor(), tablePrefix);
            return filteredTables.Count > 0 ? (filteredTables, LocalizationManager.Instance["Comp_TableHeader"]) : (null, "");
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

                // SELECT の列リストの次は必ず FROM なので常に候補に含める
                var allColumns = new List<string> { "FROM" };
                foreach (var table in tablesForColumns)
                    allColumns.AddRange(GetColumnNamesOnEditor(table));

                var filteredColumns = FilterByPrefix(allColumns, columnPrefix);
                if (filteredColumns.Count > 0)
                    return (filteredColumns, LocalizationManager.Instance["Comp_ColumnHeader"]);
            }
        }

        // SQL キーワード補完
        var kwPrefix = Regex.Match(textBeforeCaret, @"([a-zA-Z_][a-zA-Z0-9_ ]*)$").Groups[1].Value.TrimEnd();
        var (kwCandidates, kwHeader) = GetSqlKeywordCandidates(textBeforeCaret, kwPrefix);
        if (kwCandidates.Count > 0)
            return (kwCandidates, kwHeader);

        return (null, "");
    }

    private static bool IsKeywordToken(TokenKind kind) => kind >= TokenKind.Select;

    private static List<Token> TokenizeAll(string text)
    {
        var lexer = new Lexer(text);
        var tokens = new List<Token>();
        while (true)
        {
            var tok = lexer.NextToken();
            if (tok.Kind == TokenKind.EOF) break;
            tokens.Add(tok);
        }
        return tokens;
    }

    private (List<string> candidates, string header) GetSqlKeywordCandidates(string textBeforeCaret, string prefix)
    {
        // プレフィックスを除いた部分でコンテキストを判定
        var contextText = !string.IsNullOrEmpty(prefix) && textBeforeCaret.EndsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? textBeforeCaret[..^prefix.Length]
            : textBeforeCaret;

        var allTokens = TokenizeAll(contextText);

        // 最後のセミコロン以降を現在のステートメントとして扱う
        var lastSemiIdx = allTokens.FindLastIndex(t => t.Kind == TokenKind.Semicolon);
        var stmtTokens = (lastSemiIdx >= 0 ? allTokens.Skip(lastSemiIdx + 1) : (IEnumerable<Token>)allTokens).ToList();

        // ステートメントが空 → DML/DDL 先頭キーワードを提案
        if (stmtTokens.Count == 0)
        {
            return (FilterByPrefix(new List<string> {
                "SELECT", "INSERT INTO", "UPDATE", "DELETE FROM",
                "CREATE TABLE", "CREATE INDEX", "CREATE VIEW",
                "DROP TABLE", "DROP INDEX", "DROP VIEW",
                "ALTER TABLE", "BEGIN", "COMMIT", "ROLLBACK", "EXPLAIN", "PRAGMA"
            }, prefix), LocalizationManager.Instance["Comp_KeywordHeader"]);
        }

        // 最後のキーワードトークンのインデックス
        int lastKwIdx = stmtTokens.FindLastIndex(t => IsKeywordToken(t.Kind));
        var lastKw = lastKwIdx >= 0 ? stmtTokens[lastKwIdx].Kind : TokenKind.EOF;
        var lastToken = stmtTokens.Last();
        bool lastIsNonKeyword = !IsKeywordToken(lastToken.Kind);

        // 最後から2番目のキーワード
        int prevKwIdx = -1;
        for (int i = lastKwIdx - 1; i >= 0; i--)
        {
            if (IsKeywordToken(stmtTokens[i].Kind)) { prevKwIdx = i; break; }
        }
        var prevKw = prevKwIdx >= 0 ? stmtTokens[prevKwIdx].Kind : TokenKind.EOF;

        List<string> keywords;

        // テーブル名/カラム名など非キーワードの後 → 直前のキーワードで文脈判断
        if (lastIsNonKeyword)
        {
            keywords = lastKw switch
            {
                // SELECT 列リストの後（カラム名・カンマ・*等の非キーワード）→ FROM
                TokenKind.Select or TokenKind.Distinct or TokenKind.All =>
                    new() { "FROM" },
                TokenKind.From or TokenKind.Join =>
                    new() { "WHERE", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "CROSS JOIN", "NATURAL JOIN",
                            "GROUP BY", "ORDER BY", "HAVING", "LIMIT", "UNION", "EXCEPT", "INTERSECT" },
                TokenKind.On =>
                    new() { "AND", "OR", "WHERE", "GROUP BY", "ORDER BY", "HAVING", "LIMIT" },
                TokenKind.Where or TokenKind.Having or TokenKind.And or TokenKind.Or =>
                    new() { "AND", "OR", "NOT", "ORDER BY", "GROUP BY", "LIMIT" },
                TokenKind.Update => new() { "SET" },
                TokenKind.Into   => new() { "VALUES", "SELECT" },
                TokenKind.Set    => new() { "WHERE" },
                TokenKind.Limit  => new() { "OFFSET" },
                TokenKind.By when prevKw == TokenKind.Order =>
                    new() { "ASC", "DESC", "NULLS FIRST", "NULLS LAST" },
                TokenKind.By when prevKw == TokenKind.Group =>
                    new() { "HAVING", "ORDER BY", "LIMIT" },
                TokenKind.Table when prevKw == TokenKind.Alter =>
                    new() { "ADD COLUMN", "RENAME TO", "RENAME COLUMN" },
                _ => new()
            };
        }
        else
        {
            // キーワード自体が最後のトークン → その後に続くキーワードを提案
            keywords = lastKw switch
            {
                TokenKind.EOF    => new() { "SELECT", "INSERT INTO", "UPDATE", "DELETE FROM",
                                            "CREATE TABLE", "CREATE INDEX", "CREATE VIEW",
                                            "DROP TABLE", "DROP INDEX", "ALTER TABLE",
                                            "BEGIN", "COMMIT", "ROLLBACK", "EXPLAIN", "PRAGMA" },
                TokenKind.Select => new() { "DISTINCT", "ALL" },
                TokenKind.From   => new() { "WHERE", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN",
                                            "CROSS JOIN", "NATURAL JOIN", "GROUP BY", "ORDER BY", "HAVING", "LIMIT" },
                TokenKind.Where or TokenKind.Having =>
                    new() { "AND", "OR", "NOT", "EXISTS", "BETWEEN", "IN", "LIKE", "IS", "NULL" },
                TokenKind.And or TokenKind.Or =>
                    new() { "NOT", "EXISTS", "BETWEEN", "IN", "LIKE", "IS", "NULL" },
                TokenKind.Not    => new() { "EXISTS", "IN", "LIKE", "BETWEEN" },
                TokenKind.Join   => new() { "ON" },
                TokenKind.On     => new() { "AND", "OR", "WHERE", "GROUP BY", "ORDER BY", "HAVING", "LIMIT" },
                TokenKind.Inner => new() { "JOIN" },
                TokenKind.Left or TokenKind.Right or TokenKind.Full => new() { "JOIN", "OUTER JOIN" },
                TokenKind.Outer  => new() { "JOIN" },
                TokenKind.Group  => new() { "BY" },
                TokenKind.Order  => new() { "BY" },
                TokenKind.By when prevKw == TokenKind.Order => new() { "ASC", "DESC", "NULLS FIRST", "NULLS LAST" },
                TokenKind.By when prevKw == TokenKind.Group => new() { "HAVING", "ORDER BY", "LIMIT" },
                TokenKind.Asc or TokenKind.Desc => new() { "NULLS FIRST", "NULLS LAST" },
                TokenKind.Limit  => new() { "OFFSET" },
                TokenKind.Insert => new() { "INTO" },
                TokenKind.Into   => new() { "VALUES", "SELECT" },
                TokenKind.Update => new() { "SET" },
                TokenKind.Set    => new() { "WHERE" },
                TokenKind.Delete => new() { "FROM" },
                TokenKind.Create => new() { "TABLE", "UNIQUE INDEX", "INDEX", "VIEW", "TRIGGER",
                                            "TEMP TABLE", "TEMPORARY TABLE" },
                TokenKind.Drop   => new() { "TABLE", "INDEX", "VIEW", "TRIGGER" },
                TokenKind.Alter  => new() { "TABLE" },
                TokenKind.Add    => new() { "COLUMN" },
                TokenKind.Table when prevKw == TokenKind.Alter =>
                    new() { "ADD COLUMN", "RENAME TO", "RENAME COLUMN" },
                _ => new()
            };
        }

        var filtered = FilterByPrefix(keywords, prefix);
        return (filtered, LocalizationManager.Instance["Comp_KeywordHeader"]);
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
    public (string newText, int newCaretOffset) SetSqlComment(
        string documentText, int caretOffset, int selectionStart, int selectionLength)
    {
        try
        {
            if (string.IsNullOrEmpty(documentText))
                return (documentText, caretOffset);

            var lines = documentText;
            if (lines == null)
                return (documentText, caretOffset);

            //現在の行の行頭位置を調べる
            int startpos = 0;
            if (selectionLength == 0)
            {
                startpos = caretOffset;
            }
            else
            {
                startpos = selectionStart;
            }

            if (startpos == lines.Length)
            {
                startpos = startpos - 2;
            }

            if (startpos < 2)
            {
                startpos = 0;
            }
            else
            {
                for (; 2 <= startpos; startpos--)
                {
                    if (lines[startpos - 2] == '\r' && lines[startpos - 1] == '\n')
                    {
                        break;
                    }
                }
                if (startpos < 2) startpos = 0;
            }

            if (selectionLength == 0)
            {
                var aftertarget = lines.Insert(startpos, "-- ");
                return (aftertarget, caretOffset + 3);
            }
            else
            {
                var target = lines.Substring(startpos, selectionLength + selectionStart - startpos);
                var aftertarget =
                    lines.Substring(0, startpos) +
                    Regex.Replace(target, "^", "-- ", RegexOptions.Multiline) +
                    lines.Substring(selectionLength + selectionStart, lines.Length - selectionLength - selectionStart);
                if (caretOffset == selectionStart)
                {
                    return (aftertarget, caretOffset + 3);
                }
                else
                {
                    return (aftertarget, caretOffset + aftertarget.Length - documentText.Length);
                }
            }
        }
        catch
        {
            return (documentText, caretOffset);
        }
    }

    public (string newText, int newCaretOffset) RemoveSqlComment(
        string documentText, int caretOffset, int selectionStart, int selectionLength)
    {
        try
        {
            if (string.IsNullOrEmpty(documentText))
                return (documentText, caretOffset);

            // 行頭位置を調べる (SetSqlComment と同じロジック)
            int startpos = selectionLength == 0 ? caretOffset : selectionStart;

            if (startpos == documentText.Length)
                startpos = startpos - 2;

            if (startpos < 2)
            {
                startpos = 0;
            }
            else
            {
                for (; 2 <= startpos; startpos--)
                {
                    if (documentText[startpos - 2] == '\r' && documentText[startpos - 1] == '\n')
                        break;
                }
                if (startpos < 2) startpos = 0;
            }

            if (selectionLength == 0)
            {
                // 単一行: 行頭の "-- " または "--" を削除
                var rest = documentText.Substring(startpos);
                var newRest = Regex.Replace(rest, @"^-- ?", "");
                var removed = rest.Length - newRest.Length;
                var newText = documentText.Substring(0, startpos) + newRest;
                var newCaret = Math.Max(startpos, caretOffset - removed);
                return (newText, newCaret);
            }
            else
            {
                // 複数行: 選択範囲内の各行頭から "-- " または "--" を削除
                int endpos = selectionStart + selectionLength;
                var target = documentText.Substring(startpos, endpos - startpos);
                var newTarget = Regex.Replace(target, @"^-- ?", "", RegexOptions.Multiline);
                var newText = documentText.Substring(0, startpos) + newTarget + documentText.Substring(endpos);
                int lengthDiff = documentText.Length - newText.Length;

                int newCaret;
                if (caretOffset == selectionStart)
                    newCaret = Math.Max(startpos, caretOffset - Math.Min(3, lengthDiff));
                else
                    newCaret = Math.Max(0, caretOffset - lengthDiff);

                return (newText, newCaret);
            }
        }
        catch
        {
            return (documentText, caretOffset);
        }
    }
}

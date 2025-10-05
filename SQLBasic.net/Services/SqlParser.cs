using System.Globalization;
using System.Text;
using System.Linq;

namespace SQLBasic_net.Services;

// SqlParser.cs
// .NET 6+ / C# 10 で動作する、最小限の SQL パーサ（方言非依存のサブセット）
// 対応: SELECT [DISTINCT|ALL] ... FROM ... [JOIN ... ON ...]* [WHERE expr] [GROUP BY ...] [HAVING expr] [ORDER BY ...] [LIMIT n [OFFSET m]|LIMIT m, n] [;]
// 追加対応: JOIN（INNER/LEFT/RIGHT/FULL）、LIKE / NOT LIKE、IN / NOT IN、IS [NOT] NULL
// 目的: 簡易SQLエディタ内での構文チェック、AST生成のたたき台
// 免責: SQL 方言（T-SQL、PostgreSQL、SQLite、MySQL など）固有構文は未対応
// 拡張しやすいよう最小限の設計にしています
// -------------------------------------------------------------

#region トークンの種類(TokenKind)
public enum TokenKind
{
    EOF,
    Identifier,
    Number,
    String,
    Comma, Dot, Star,
    LParen, RParen,
    Equal, NotEqual, Less, LessEqual, Greater, GreaterEqual,
    Plus, Minus, Asterisk, Slash, Percent,
    Semicolon,
    // Keywords (case-insensitive)
    Select, Distinct, All, From, Where, Group, By, Having, Order, Asc, Desc, Limit, Offset, As,
    And, Or, Not,
    True, False, Null,
    // New for JOIN / predicates
    Join, Inner, Left, Right, Full, Outer, On,
    Like, In, Is,
    // DML
    Insert, Into, Values,
    Update, Set,
    Delete,
    Create, Table, Index,
    Drop,
    Alter, Add, Column
}
#endregion

#region トークン構造体(struct)
public readonly struct Token
{
    public TokenKind Kind { get; }
    public string Text { get; }
    public int Position { get; }
    public Token(TokenKind kind, string text, int pos)
    {
        Kind = kind; Text = text; Position = pos;
    }
    public override string ToString() => $"{Kind}('{Text}')@{Position}";
}
#endregion

#region Lexer クラス
public sealed class Lexer
{
    private readonly string _text;
    private int _pos;
    private readonly int _len;

    private static readonly Dictionary<string, TokenKind> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SELECT"] = TokenKind.Select,
        ["DISTINCT"] = TokenKind.Distinct,
        ["ALL"] = TokenKind.All,
        ["FROM"] = TokenKind.From,
        ["WHERE"] = TokenKind.Where,
        ["GROUP"] = TokenKind.Group,
        ["ORDER"] = TokenKind.Order,
        ["BY"] = TokenKind.By,
        ["HAVING"] = TokenKind.Having,
        ["ASC"] = TokenKind.Asc,
        ["DESC"] = TokenKind.Desc,
        ["LIMIT"] = TokenKind.Limit,
        ["OFFSET"] = TokenKind.Offset,
        ["AS"] = TokenKind.As,
        ["AND"] = TokenKind.And,
        ["OR"] = TokenKind.Or,
        ["NOT"] = TokenKind.Not,
        ["TRUE"] = TokenKind.True,
        ["FALSE"] = TokenKind.False,
        ["NULL"] = TokenKind.Null,
        ["JOIN"] = TokenKind.Join,
        ["INNER"] = TokenKind.Inner,
        ["LEFT"] = TokenKind.Left,
        ["RIGHT"] = TokenKind.Right,
        ["FULL"] = TokenKind.Full,
        ["OUTER"] = TokenKind.Outer,
        ["ON"] = TokenKind.On,
        ["LIKE"] = TokenKind.Like,
        ["IN"] = TokenKind.In,
        ["IS"] = TokenKind.Is,
        ["INSERT"] = TokenKind.Insert,
        ["INTO"] = TokenKind.Into,
        ["VALUES"] = TokenKind.Values,
        ["UPDATE"] = TokenKind.Update,
        ["SET"] = TokenKind.Set,
        ["DELETE"] = TokenKind.Delete,
        ["CREATE"] = TokenKind.Create,
        ["TABLE"] = TokenKind.Table,
        ["INDEX"] = TokenKind.Index,
        ["DROP"] = TokenKind.Drop,
        ["ALTER"] = TokenKind.Alter,
        ["ADD"] = TokenKind.Add,
        ["COLUMN"] = TokenKind.Column,
    };

    public Lexer(string text)
    {
        _text = text ?? string.Empty;
        _len = _text.Length;
        _pos = 0;
    }

    private char Current => _pos < _len ? _text[_pos] : ' ';
    private char Peek(int offset) => (_pos + offset) < _len ? _text[_pos + offset] : ' ';
    private void Advance(int n = 1) => _pos = Math.Min(_pos + n, _len);

    private void SkipWhiteAndComments()
    {
        while (true)
        {
            int before = _pos;

            // whitespace
            while (_pos < _len && char.IsWhiteSpace(Current)) Advance();
            // line comment -- ...

            if (Current == '-' && Peek(1) == '-')
            {
                Advance(2);
                while (_pos < _len && Current is not ('\n' or '\r'))
                    Advance();
            }
            // block comment /* ... */
            if (Current == '/' && Peek(1) == '*')
            {
                Advance(2);
                while (_pos < _len && !(Current == '*' && Peek(1) == '/'))
                    Advance();
                if (_pos < _len) Advance(2); // consume closing */            }
            }
            if (_pos == before)
                break;
        }
    }

    public Token NextToken()
    {
        SkipWhiteAndComments();
        int start = _pos;
        char c = Current;
        if (c == ' ') return new Token(TokenKind.EOF, string.Empty, _pos);

        // punctuation & operators
        switch (c)
        {
            case ',': Advance(); return new Token(TokenKind.Comma, ",", start);
            case '.': Advance(); return new Token(TokenKind.Dot, ".", start);
            case '*': Advance(); return new Token(TokenKind.Star, "*", start);
            case '(': Advance(); return new Token(TokenKind.LParen, "(", start);
            case ')': Advance(); return new Token(TokenKind.RParen, ")", start);
            case ';': Advance(); return new Token(TokenKind.Semicolon, ";", start);
            case '+': Advance(); return new Token(TokenKind.Plus, "+", start);
            case '-': Advance(); return new Token(TokenKind.Minus, "-", start);
            case '/': Advance(); return new Token(TokenKind.Slash, "/", start);
            case '%': Advance(); return new Token(TokenKind.Percent, "%", start);
            case '=': Advance(); return new Token(TokenKind.Equal, "=", start);
            case '<':
                if (Peek(1) == '=') { Advance(2); return new Token(TokenKind.LessEqual, "<=", start); }
                if (Peek(1) == '>') { Advance(2); return new Token(TokenKind.NotEqual, "<>", start); }
                Advance(); return new Token(TokenKind.Less, "<", start);
            case '>':
                if (Peek(1) == '=') { Advance(2); return new Token(TokenKind.GreaterEqual, ">=", start); }
                Advance(); return new Token(TokenKind.Greater, ">", start);
        }

        // string literal '...'
        if (c == '\'')
        {
            Advance();
            var sb = new StringBuilder();
            while (true)
            {
                if (Current == ' ') throw new Exception($"Unterminated string starting at {start}");
                if (Current == '\'')
                {
                    if (Peek(1) == '\'') { sb.Append('\''); Advance(2); continue; } // escaped ''
                    Advance(); break; // end
                }
                sb.Append(Current);
                Advance();
            }
            return new Token(TokenKind.String, sb.ToString(), start);
        }

        // number (int or decimal)
        if (char.IsDigit(c))
        {
            int p = _pos;
            while (char.IsDigit(Current)) Advance();
            if (Current == '.') { Advance(); while (char.IsDigit(Current)) Advance(); }
            string num = _text.Substring(p, _pos - p);
            return new Token(TokenKind.Number, num, p);
        }

        // identifier or keyword [A-Za-z_][A-Za-z0-9$_]*
        if (char.IsLetter(c) || c == '_')
        {
            int p = _pos; Advance();
            while (char.IsLetterOrDigit(Current) || Current == '_' || Current == '$') Advance();
            string ident = _text.Substring(p, _pos - p);
            if (_keywords.TryGetValue(ident, out var kw))
                return new Token(kw, ident, p);
            return new Token(TokenKind.Identifier, ident, p);
        }

        throw new Exception($"Unexpected character '{c}' at position {start}");
    }
}
#endregion

#region AST群
 public abstract record SqlStatement;

    public abstract record Expr;
    public sealed record LiteralExpr(object? Value) : Expr;
    public sealed record IdentifierExpr(IReadOnlyList<string> Parts) : Expr
    {
        public override string ToString() => string.Join('.', Parts);
    }
    public sealed record UnaryExpr(string Op, Expr Operand) : Expr;
    public sealed record BinaryExpr(string Op, Expr Left, Expr Right) : Expr;
    public sealed record CallExpr(string Name, IReadOnlyList<Expr> Args) : Expr;
    public sealed record InExpr(Expr Target, IReadOnlyList<Expr> List, bool Negated) : Expr;
    public sealed record AllColumnsExpr(IReadOnlyList<string>? Qualifier) : Expr
    {
        public override string ToString()
        {
            if (Qualifier is null || Qualifier.Count == 0)
                return "*";
            return string.Join('.', Qualifier) + ".*";
        }
    }

    public sealed record SelectItem(Expr Expression, string? Alias);
    public abstract record FromItem;
    public sealed record NamedTable(IdentifierExpr Name, string? Alias) : FromItem;
    public sealed record DerivedTable(SelectStatement Subquery, string Alias) : FromItem;
    public sealed record OrderByItem(Expr Expression, bool Descending);
    public enum JoinKind { Inner, Left, Right, Full }
    public sealed record JoinClause(JoinKind Kind, FromItem Right, Expr On);
    public sealed record FromClause(FromItem Base, IReadOnlyList<JoinClause> Joins);

public enum SelectResultModifier
    {
        All,
        Distinct
    }

    public sealed record SelectStatement(
        SelectResultModifier Modifier,
        IReadOnlyList<SelectItem> Items,
        FromClause? From,
        Expr? Where,
        IReadOnlyList<Expr>? GroupBy,
        Expr? Having,
        IReadOnlyList<OrderByItem>? OrderBy,
        int? Limit,
        int? Offset
    ) : SqlStatement;


// DML AST
public sealed record InsertStatement(
    NamedTable Into,
    IReadOnlyList<string>? Columns,
    IReadOnlyList<IReadOnlyList<Expr>>? ValuesRows,
    SelectStatement? SelectSource
) : SqlStatement;

public sealed record Assignment(IdentifierExpr Column, Expr Value);
public sealed record UpdateStatement(
    NamedTable Target,
    IReadOnlyList<Assignment> SetList,
    Expr? Where
) : SqlStatement;

public sealed record DeleteStatement(
    NamedTable From,
    Expr? Where
) : SqlStatement;

// DDL AST
public sealed record ColumnDefinition(string Name, string Type);
public sealed record CreateTableStatement(
    IdentifierExpr Name,
    IReadOnlyList<ColumnDefinition> Columns
) : SqlStatement;

public sealed record CreateIndexStatement(
    IdentifierExpr Name,
    IdentifierExpr Table,
    IReadOnlyList<string> Columns
) : SqlStatement;

public sealed record DropTableStatement(
    IdentifierExpr Name
) : SqlStatement;
public sealed record DropIndexStatement(
    IdentifierExpr Name,
    IdentifierExpr Table
) : SqlStatement;

public sealed record AlterTableAddColumnStatement(
    IdentifierExpr Table,
    ColumnDefinition Column
) : SqlStatement;
#endregion


#region Parserクラス
public sealed class Parser
{
    private readonly Lexer _lexer;
    private Token _cur;
    private Token _peek;
    private bool _allowQualifiedStar;

    public Parser(string text)
    {
        _lexer = new Lexer(text);
        _cur = _lexer.NextToken();
        _peek = _lexer.NextToken();
    }

    private void Next() { _cur = _peek; _peek = _lexer.NextToken(); }
    private bool Match(TokenKind kind) { if (_cur.Kind == kind) { Next(); return true; } return false; }
    private Token Expect(TokenKind kind, string? msg = null)
    {
        if (_cur.Kind != kind) throw Error(msg ?? $"Expected {kind}, got {_cur.Kind} at pos {_cur.Position}");
        var t = _cur; Next(); return t;
    }
    private Exception Error(string msg) => new($"Parse error: {msg}");

    // 汎用入口: 先頭トークンで分岐
    public SqlStatement ParseStatement()
    {
        return _cur.Kind switch
        {
            TokenKind.Select => ParseSelectLike(endWithEof: true),
            TokenKind.Insert => ParseInsertStatement(endWithEof: true),
            TokenKind.Update => ParseUpdateStatement(endWithEof: true),
            TokenKind.Delete => ParseDeleteStatement(endWithEof: true),
            TokenKind.Create => ParseCreateStatement(endWithEof: true),
            TokenKind.Drop => ParseDropStatement(endWithEof: true),
            TokenKind.Alter => ParseAlterStatement(endWithEof: true),
            _ => throw Error($"Unsupported statement start: {_cur.Kind}")
        };
    }

    // 既存互換: SELECT 専用
    public SelectStatement ParseSelectStatement()
        => ParseSelectLike(endWithEof: true);

    private SelectStatement ParseSelectLike(bool endWithEof)
    {
        Expect(TokenKind.Select, "SELECT expected");
        var modifier = SelectResultModifier.All;
        if (Match(TokenKind.Distinct)) modifier = SelectResultModifier.Distinct;
        else if (Match(TokenKind.All)) { /* explicit ALL - no-op */ }

        var items = ParseSelectList();
        FromClause? from = null;
        Expr? where = null;
        List<Expr>? groupBy = null;
        Expr? having = null;
        List<OrderByItem>? orderBy = null;
        int? limit = null; int? offset = null;

        if (Match(TokenKind.From))
            from = ParseFromClause();
        if (Match(TokenKind.Where))
            where = ParseExpr();
        if (Match(TokenKind.Group))
        {
            Expect(TokenKind.By, "BY expected after GROUP");
            groupBy = new List<Expr>();
            do
            {
                groupBy.Add(ParseExpr());
            } while (Match(TokenKind.Comma));
        }
        if (Match(TokenKind.Having))
            having = ParseExpr();
        if (Match(TokenKind.Order))
        {
            Expect(TokenKind.By);
            orderBy = new List<OrderByItem>();
            do
            {
                var e = ParseExpr();
                bool desc = false;
                if (Match(TokenKind.Asc)) desc = false;
                else if (Match(TokenKind.Desc)) desc = true;
                orderBy.Add(new OrderByItem(e, desc));
            } while (Match(TokenKind.Comma));
        }
        if (Match(TokenKind.Limit))
        {
            var first = ParseRequiredInt64("LIMIT requires a number");
            if (Match(TokenKind.Comma))
            {
                offset = (int)first;
                limit = (int)ParseRequiredInt64("LIMIT requires a number");
            }
            else
            {
                limit = (int)first;
                if (Match(TokenKind.Offset))
                    offset = (int)ParseRequiredInt64("OFFSET requires a number");
            }
        }
        Match(TokenKind.Semicolon); // optional
        if (endWithEof) Expect(TokenKind.EOF, "Unexpected tokens after statement");

        return new SelectStatement(modifier, items, from, where, groupBy, having, orderBy, limit, offset);
    }

    private InsertStatement ParseInsertStatement(bool endWithEof)
    {
        Expect(TokenKind.Insert, "INSERT expected");
        // Most dialects require INTO
        if (!Match(TokenKind.Into)) Expect(TokenKind.Into, "INTO expected");

        var table = ParseNamedTable();

        // optional column list
        List<string>? columns = null;
        if (Match(TokenKind.LParen))
        {
            columns = new List<string>();
            do
            {
                columns.Add(ParseIdentifierSingle());
            } while (Match(TokenKind.Comma));
            Expect(TokenKind.RParen);
        }

        List<IReadOnlyList<Expr>>? rows = null;
        SelectStatement? selectSource = null;

        if (Match(TokenKind.Values))
        {
            rows = new List<IReadOnlyList<Expr>>();
            do
            {
                Expect(TokenKind.LParen, "VALUES requires (...) list");
                var list = new List<Expr>();
                if (_cur.Kind != TokenKind.RParen)
                {
                    do { list.Add(ParseExpr()); } while (Match(TokenKind.Comma));
                }
                Expect(TokenKind.RParen);
                rows.Add(list);
            } while (Match(TokenKind.Comma));
        }
        else if (_cur.Kind == TokenKind.Select)
        {
            selectSource = ParseSelectLike(endWithEof: false);
        }
        else
        {
            throw Error("Expected VALUES or SELECT after INSERT target");
        }

        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new InsertStatement(table, columns, rows, selectSource);
    }

    private UpdateStatement ParseUpdateStatement(bool endWithEof)
    {
        Expect(TokenKind.Update, "UPDATE expected");
        var target = ParseNamedTable();
        Expect(TokenKind.Set, "SET expected");

        var sets = new List<Assignment>();
        do
        {
            var col = ParseIdentifier();
            Expect(TokenKind.Equal, "= expected in assignment");
            var val = ParseExpr();
            sets.Add(new Assignment(col, val));
        } while (Match(TokenKind.Comma));

        Expr? where = null;
        if (Match(TokenKind.Where))
            where = ParseExpr();

        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new UpdateStatement(target, sets, where);
    }

    private DeleteStatement ParseDeleteStatement(bool endWithEof)
    {
        Expect(TokenKind.Delete, "DELETE expected");
        // Optional FROM (we accept both: DELETE FROM t ... or DELETE t ...)
        if (!Match(TokenKind.From)) { /* tolerate absence */ }
        var tr = ParseNamedTable();

        Expr? where = null;
        if (Match(TokenKind.Where))
            where = ParseExpr();

        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new DeleteStatement(tr, where);
    }

    private SqlStatement ParseCreateStatement(bool endWithEof)
    {
        Expect(TokenKind.Create, "CREATE expected");
        if (_cur.Kind == TokenKind.Table)
            return ParseCreateTableStatement(endWithEof);
        if (_cur.Kind == TokenKind.Index)
            return ParseCreateIndexStatement(endWithEof);
        throw Error("Expected TABLE or INDEX after CREATE");
    }

    private CreateTableStatement ParseCreateTableStatement(bool endWithEof)
    {
        Expect(TokenKind.Table, "TABLE expected");
        var name = ParseIdentifier();
        Expect(TokenKind.LParen, "( expected");
        var columns = new List<ColumnDefinition>();
        while (_cur.Kind != TokenKind.RParen)
        {
            var colName = ParseIdentifierSingle();
            var sb = new StringBuilder();
            // Track nested parentheses to handle types like VARCHAR(128)
            var parenDepth = 0;
            var needsSpace = false;
            while (
                !(_cur.Kind == TokenKind.Comma && parenDepth == 0) &&
                !(_cur.Kind == TokenKind.RParen && parenDepth == 0))
            {
                if (needsSpace && _cur.Kind != TokenKind.Comma && _cur.Kind != TokenKind.RParen && _cur.Kind != TokenKind.LParen)
                    sb.Append(' ');

                sb.Append(_cur.Text);

                if (_cur.Kind == TokenKind.LParen)
                {
                    parenDepth++;
                    needsSpace = false;
                }
                else if (_cur.Kind == TokenKind.RParen)
                {
                    parenDepth--;
                    needsSpace = true;
                }
                else
                {
                    needsSpace = true;
                }

                Next();
            }
            columns.Add(new ColumnDefinition(colName, sb.ToString().Trim()));
            if (!Match(TokenKind.Comma))
                break;
        }
        Expect(TokenKind.RParen, ") expected");
        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new CreateTableStatement(name, columns);
    }

    private CreateIndexStatement ParseCreateIndexStatement(bool endWithEof)
    {
        Expect(TokenKind.Index, "INDEX expected");
        var idxName = ParseIdentifier();
        Expect(TokenKind.On, "ON expected");
        var table = ParseIdentifier();
        Expect(TokenKind.LParen, "( expected");
        var cols = new List<string>();
        do
        {
            cols.Add(ParseIdentifierSingle());
        } while (Match(TokenKind.Comma));
        Expect(TokenKind.RParen, ") expected");
        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new CreateIndexStatement(idxName, table, cols);
    }

    private SqlStatement ParseDropStatement(bool endWithEof)
    {
        Expect(TokenKind.Drop, "DROP expected");
        if (_cur.Kind == TokenKind.Table)
            return ParseDropTableStatement(endWithEof);
        if (_cur.Kind == TokenKind.Index)
            return ParseDropIndexStatement(endWithEof);
        throw Error("Expected TABLE or INDEX after DROP");
    }

    private DropTableStatement ParseDropTableStatement(bool endWithEof)
    {
        Expect(TokenKind.Table, "TABLE expected");
        var name = ParseIdentifier();
        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new DropTableStatement(name);
    }

    private DropIndexStatement ParseDropIndexStatement(bool endWithEof)
    {
        Expect(TokenKind.Index, "INDEX expected");
        var name = ParseIdentifier();
        IdentifierExpr? table = null;
        if (Match(TokenKind.On))
        {
            table = ParseIdentifier();
        }
        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);
        return new DropIndexStatement(name, table!);
    }

    private AlterTableAddColumnStatement ParseAlterStatement(bool endWithEof)
    {
        Expect(TokenKind.Alter, "ALTER expected");
        Expect(TokenKind.Table, "TABLE expected");
        var table = ParseIdentifier();
        Expect(TokenKind.Add, "ADD expected");
        Match(TokenKind.Column);
        var columnName = ParseIdentifierSingle();

        var typeBuilder = new StringBuilder();
        var parenDepth = 0;
        var needsSpace = false;

        while (_cur.Kind != TokenKind.Semicolon && _cur.Kind != TokenKind.EOF)
        {
            if (_cur.Kind == TokenKind.Comma && parenDepth == 0)
                break;

            if (needsSpace && _cur.Kind != TokenKind.Comma && _cur.Kind != TokenKind.RParen && _cur.Kind != TokenKind.LParen)
                typeBuilder.Append(' ');

            typeBuilder.Append(_cur.Text);

            if (_cur.Kind == TokenKind.LParen)
            {
                parenDepth++;
                needsSpace = false;
            }
            else if (_cur.Kind == TokenKind.RParen)
            {
                parenDepth = Math.Max(0, parenDepth - 1);
                needsSpace = true;
            }
            else
            {
                needsSpace = true;
            }

            Next();
        }

        var type = typeBuilder.ToString().Trim();

        Match(TokenKind.Semicolon);
        if (endWithEof) Expect(TokenKind.EOF);

        return new AlterTableAddColumnStatement(table, new ColumnDefinition(columnName, type));
    }

    private List<SelectItem> ParseSelectList()
    {
        var list = new List<SelectItem>();
        do
        {
            if (Match(TokenKind.Star))
            {
                list.Add(new SelectItem(new AllColumnsExpr(null), null));
            }
            else
            {
                var prevAllow = _allowQualifiedStar;
                _allowQualifiedStar = true;
                var expr = ParseExpr();
                _allowQualifiedStar = prevAllow;

                if (expr is IdentifierExpr id && id.Parts.Count > 0 && id.Parts[id.Parts.Count - 1] == "*")
                {
                    var qualifiers = id.Parts.Take(id.Parts.Count - 1).ToList();
                    expr = new AllColumnsExpr(qualifiers.Count == 0 ? null : qualifiers);
                }

                string? alias = null;
                if (Match(TokenKind.As))
                {
                    alias = ParseIdentifierSingle();
                }
                else if (_cur.Kind == TokenKind.Identifier)
                {
                    // support implicit alias: expr alias
                    alias = _cur.Text; Next();
                }
                list.Add(new SelectItem(expr, alias));
            }
        } while (Match(TokenKind.Comma));
        return list;
    }

    private FromClause ParseFromClause()
    {
        var baseTable = ParseFromItem();
        var joins = new List<JoinClause>();
        while (IsJoinStart(_cur.Kind))
        {
            var kind = ParseJoinKind();
            var right = ParseFromItem();
            Expect(TokenKind.On, "JOIN requires ON <condition>");
            var on = ParseExpr();
            joins.Add(new JoinClause(kind, right, on));
        }
        return new FromClause(baseTable, joins);
    }
    private NamedTable ParseNamedTable()
    {
        var name = ParseIdentifier();
        string? alias = null;
        if (Match(TokenKind.As)) alias = ParseIdentifierSingle();
        else if (_cur.Kind == TokenKind.Identifier) { alias = _cur.Text; Next(); }
        return new NamedTable(name, alias);
    }

    private static bool IsJoinStart(TokenKind k)
        => k == TokenKind.Join || k == TokenKind.Inner || k == TokenKind.Left || k == TokenKind.Right || k == TokenKind.Full;

    private JoinKind ParseJoinKind()
    {
        if (Match(TokenKind.Join)) return JoinKind.Inner; // plain JOIN = INNER JOIN
        if (Match(TokenKind.Inner)) { Expect(TokenKind.Join); return JoinKind.Inner; }
        if (Match(TokenKind.Left)) { Match(TokenKind.Outer); Expect(TokenKind.Join); return JoinKind.Left; }
        if (Match(TokenKind.Right)) { Match(TokenKind.Outer); Expect(TokenKind.Join); return JoinKind.Right; }
        if (Match(TokenKind.Full)) { Match(TokenKind.Outer); Expect(TokenKind.Join); return JoinKind.Full; }
        throw Error($"JOIN keyword expected at {_cur.Position}");
    }

    private FromItem ParseFromItem()
    {
        // ( SELECT ... ) [AS] alias
        if (_cur.Kind == TokenKind.LParen)
        {
            Next();
            if (_cur.Kind != TokenKind.Select)
                throw Error("Subquery in FROM must start with SELECT");
            var sub = ParseSelectLike(endWithEof: false);
            Expect(TokenKind.RParen, ") expected after subquery");

            // 別名は必須（SQLiteもFROMサブクエリはエイリアス必須）
            if (Match(TokenKind.As))
            {
                return new DerivedTable(sub, ParseIdentifierSingle());
            }
            else if (_cur.Kind == TokenKind.Identifier)
            {
                return new DerivedTable(sub, ParseIdentifierSingle());
            }
            else
            {
                throw Error("Alias is required for subquery in FROM");
            }
        }

        // 通常のテーブル参照: ident[.ident[...]] [AS] alias?
        var name = ParseIdentifier();
        string? alias = null;
        if (Match(TokenKind.As)) alias = ParseIdentifierSingle();
        else if (_cur.Kind == TokenKind.Identifier) { alias = _cur.Text; Next(); }

        return new NamedTable(name, alias);
    }

    // --- Expressions ---
    // precedence: OR < AND < NOT < cmp (including LIKE/IN/IS) < add < mul < unary < primary
    private Expr ParseExpr() => ParseOr();
    private Expr ParseOr()
    {
        var left = ParseAnd();
        while (_cur.Kind == TokenKind.Or)
        {
            Next();
            var right = ParseAnd();
            left = new BinaryExpr("OR", left, right);
        }
        return left;
    }
    private Expr ParseAnd()
    {
        var left = ParseNot();
        while (_cur.Kind == TokenKind.And)
        {
            Next();
            var right = ParseNot();
            left = new BinaryExpr("AND", left, right);
        }
        return left;
    }
    private Expr ParseNot()
    {
        if (_cur.Kind == TokenKind.Not)
        {
            Next();
            var inner = ParseCompare();
            return new UnaryExpr("NOT", inner);
        }
        return ParseCompare();
    }
    private Expr ParseCompare()
    {
        var left = ParseAdd();
        while (true)
        {
            // standard binary comparisons
            switch (_cur.Kind)
            {
                case TokenKind.Equal: Next(); left = new BinaryExpr("=", left, ParseAdd()); continue;
                case TokenKind.NotEqual: Next(); left = new BinaryExpr("<>", left, ParseAdd()); continue;
                case TokenKind.Less: Next(); left = new BinaryExpr("<", left, ParseAdd()); continue;
                case TokenKind.LessEqual: Next(); left = new BinaryExpr("<=", left, ParseAdd()); continue;
                case TokenKind.Greater: Next(); left = new BinaryExpr(">", left, ParseAdd()); continue;
                case TokenKind.GreaterEqual: Next(); left = new BinaryExpr(">=", left, ParseAdd()); continue;
            }

            // LIKE / NOT LIKE
            if (_cur.Kind == TokenKind.Like || (_cur.Kind == TokenKind.Not && _peek.Kind == TokenKind.Like))
            {
                bool neg = false;
                if (_cur.Kind == TokenKind.Not) { Next(); neg = true; }
                Expect(TokenKind.Like);
                left = new BinaryExpr(neg ? "NOT LIKE" : "LIKE", left, ParseAdd());
                continue;
            }

            // IN / NOT IN (expr, ...)
            if (_cur.Kind == TokenKind.In || (_cur.Kind == TokenKind.Not && _peek.Kind == TokenKind.In))
            {
                bool neg = false;
                if (_cur.Kind == TokenKind.Not) { Next(); neg = true; }
                Expect(TokenKind.In);
                Expect(TokenKind.LParen, "IN requires a parenthesized list");
                var list = new List<Expr>();
                if (_cur.Kind != TokenKind.RParen)
                {
                    do { list.Add(ParseExpr()); } while (Match(TokenKind.Comma));
                }
                Expect(TokenKind.RParen);
                left = new InExpr(left, list, neg);
                continue;
            }

            // IS [NOT] NULL
            if (_cur.Kind == TokenKind.Is)
            {
                Next();
                bool not = Match(TokenKind.Not);
                Expect(TokenKind.Null, "NULL expected after IS [NOT]");
                left = new BinaryExpr(not ? "IS NOT" : "IS", left, new LiteralExpr(null));
                continue;
            }

            return left;
        }
    }
    private Expr ParseAdd()
    {
        var left = ParseMul();
        while (true)
        {
            if (Match(TokenKind.Plus)) left = new BinaryExpr("+", left, ParseMul());
            else if (Match(TokenKind.Minus)) left = new BinaryExpr("-", left, ParseMul());
            else return left;
        }
    }
    private Expr ParseMul()
    {
        var left = ParseUnary();
        while (true)
        {
            if (Match(TokenKind.Asterisk) || Match(TokenKind.Star)) left = new BinaryExpr("*", left, ParseUnary());
            else if (Match(TokenKind.Slash)) left = new BinaryExpr("/", left, ParseUnary());
            else if (Match(TokenKind.Percent)) left = new BinaryExpr("%", left, ParseUnary());
            else return left;
        }
    }
    private Expr ParseUnary()
    {
        if (Match(TokenKind.Minus)) return new UnaryExpr("-", ParseUnary());
        if (Match(TokenKind.Plus)) return new UnaryExpr("+", ParseUnary());
        return ParsePrimary();
    }
    private Expr ParsePrimary()
    {
        if (Match(TokenKind.LParen))
        {
            var e = ParseExpr();
            Expect(TokenKind.RParen, ") expected");
            return e;
        }
        switch (_cur.Kind)
        {
            case TokenKind.Number:
                var numText = _cur.Text; Next();
                if (numText.Contains('.'))
                    return new LiteralExpr(double.Parse(numText, CultureInfo.InvariantCulture));
                else
                    return new LiteralExpr(long.Parse(numText, CultureInfo.InvariantCulture));
            case TokenKind.String:
                var s = _cur.Text; Next();
                return new LiteralExpr(s);
            case TokenKind.True: Next(); return new LiteralExpr(true);
            case TokenKind.False: Next(); return new LiteralExpr(false);
            case TokenKind.Null: Next(); return new LiteralExpr(null);
            case TokenKind.Identifier:
                // function call or identifier chain
                var name = ParseIdentifier();
                if (Match(TokenKind.LParen))
                {
                    var args = new List<Expr>();
                    if (_cur.Kind == TokenKind.Star)
                    {
                        Next();
                        args.Add(new AllColumnsExpr(null));
                    }
                    else if (_cur.Kind != TokenKind.RParen)
                    {
                        do { args.Add(ParseExpr()); } while (Match(TokenKind.Comma));
                    }
                    Expect(TokenKind.RParen);
                    return new CallExpr(name.ToString(), args);
                }
                return name;
            default:
                throw Error($"Unexpected token {_cur.Kind} at {_cur.Position}");
        }
    }

    private IdentifierExpr ParseIdentifier()
    {
        var parts = new List<string> { ParseIdentifierSingle() };
        while (true)
        {
            if (!Match(TokenKind.Dot))
                break;

            if (_allowQualifiedStar && _cur.Kind == TokenKind.Star)
            {
                Next();
                parts.Add("*");
                break;
            }

            parts.Add(ParseIdentifierSingle());
        }
        return new IdentifierExpr(parts);
    }
    private string ParseIdentifierSingle()
    {
        if (_cur.Kind != TokenKind.Identifier)
            throw Error($"Identifier expected at {_cur.Position}");
        var name = _cur.Text; Next(); return name;
    }
    private long ParseRequiredInt64(string err)
    {
        if (_cur.Kind != TokenKind.Number)
            throw Error(err);
        if (!long.TryParse(_cur.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v))
            throw Error("Invalid integer");
        Next();
        return v;
    }
}
#endregion

#region Simple Dumper (for debugging)
public static class AstDumper
{
    public static string Dump(SqlStatement stmt) => stmt switch
    {
        SelectStatement s => Dump(s),
        InsertStatement i => Dump(i),
        UpdateStatement u => Dump(u),
        DeleteStatement d => Dump(d),
        CreateTableStatement ct => Dump(ct),
        CreateIndexStatement ci => Dump(ci),
        DropTableStatement dt => Dump(dt),
        DropIndexStatement di => Dump(di),
        AlterTableAddColumnStatement at => Dump(at),
        _ => stmt.ToString() ?? stmt.GetType().Name
    };

    public static string Dump(SelectStatement stmt)
    {
        var sb = new StringBuilder();
        sb.Append("SELECT");
        if (stmt.Modifier == SelectResultModifier.Distinct) sb.Append(" DISTINCT");
        sb.AppendLine();
        for (int i = 0; i < stmt.Items.Count; i++)
        {
            var it = stmt.Items[i];
            sb.Append("  ").Append(i == 0 ? " " : ",").Append(Dump(it.Expression));
            if (!string.IsNullOrEmpty(it.Alias)) sb.Append(" AS ").Append(it.Alias);
            sb.AppendLine();
        }

        if (stmt.From is not null)
        {
            sb.Append("FROM ").Append(DumpFromItem(stmt.From.Base)).AppendLine();
            foreach (var j in stmt.From.Joins)
            {
                sb.Append(JoinKindToSql(j.Kind))
                  .Append(" JOIN ")
                  .Append(DumpFromItem(j.Right))
                  .Append(" ON ").Append(Dump(j.On)).AppendLine();
            }
        }
        if (stmt.Where is not null)
        {
            sb.Append("WHERE ").Append(Dump(stmt.Where)).AppendLine();
        }
        if (stmt.GroupBy is not null && stmt.GroupBy.Count > 0)
        {
            sb.Append("GROUP BY ");
            for (int i = 0; i < stmt.GroupBy.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Dump(stmt.GroupBy[i]));
            }
            sb.AppendLine();
        }
        if (stmt.Having is not null)
        {
            sb.Append("HAVING ").Append(Dump(stmt.Having)).AppendLine();
        }
        if (stmt.OrderBy is not null && stmt.OrderBy.Count > 0)
        {
            sb.Append("ORDER BY ");
            for (int i = 0; i < stmt.OrderBy.Count; i++)
            {
                var o = stmt.OrderBy[i];
                if (i > 0) sb.Append(", ");
                sb.Append(Dump(o.Expression)).Append(o.Descending ? " DESC" : " ASC");
            }
            sb.AppendLine();
        }
        if (stmt.Limit is not null)
        {
            sb.Append("LIMIT ").Append(stmt.Limit);
            if (stmt.Offset is not null) sb.Append(" OFFSET ").Append(stmt.Offset);
            sb.AppendLine();
        }
        return sb.ToString();
    }
    private static string DumpFromItem(FromItem item)
    {
        return item switch
        {
            NamedTable nt => nt.Alias is null
                ? nt.Name.ToString()
                : $"{nt.Name} AS {nt.Alias}",
            DerivedTable dt =>
                "(" + Dump(dt.Subquery).TrimEnd() + ") AS " + dt.Alias,
            _ => item.ToString() ?? item.GetType().Name
        };
    }

    public static string Dump(InsertStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("INSERT INTO ").Append(s.Into.Name);
        if (s.Columns is not null && s.Columns.Count > 0)
            sb.Append(" (").Append(string.Join(", ", s.Columns)).Append(")");
        if (s.ValuesRows is not null)
        {
            sb.Append(" VALUES ");
            for (int r = 0; r < s.ValuesRows.Count; r++)
            {
                if (r > 0) sb.Append(", ");
                sb.Append('(').Append(string.Join(", ", s.ValuesRows[r].Select(Dump))).Append(')');
            }
        }
        else if (s.SelectSource is not null)
        {
            sb.AppendLine();
            sb.Append(Dump(s.SelectSource).TrimEnd());
        }
        return sb.ToString();
    }

    public static string Dump(UpdateStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("UPDATE ").Append(s.Target.Name);
        if (!string.IsNullOrEmpty(s.Target.Alias)) sb.Append(" AS ").Append(s.Target.Alias);
        sb.Append(" SET ");
        for (int i = 0; i < s.SetList.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var a = s.SetList[i];
            sb.Append(Dump(a.Column)).Append(" = ").Append(Dump(a.Value));
        }
        if (s.Where is not null)
            sb.Append(" WHERE ").Append(Dump(s.Where));
        return sb.ToString();
    }

    public static string Dump(DeleteStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("DELETE FROM ").Append(s.From.Name);
        if (!string.IsNullOrEmpty(s.From.Alias)) sb.Append(" AS ").Append(s.From.Alias);
        if (s.Where is not null)
            sb.Append(" WHERE ").Append(Dump(s.Where));
        return sb.ToString();
    }

    public static string Dump(CreateTableStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("CREATE TABLE ").Append(s.Name);
        sb.Append(" (");
        for (int i = 0; i < s.Columns.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var c = s.Columns[i];
            sb.Append(c.Name).Append(' ').Append(c.Type);
        }
        sb.Append(")");
        return sb.ToString();
    }

    public static string Dump(CreateIndexStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("CREATE INDEX ").Append(s.Name);
        sb.Append(" ON ").Append(s.Table);
        sb.Append(" (").Append(string.Join(", ", s.Columns)).Append(")");
        return sb.ToString();
    }

    public static string Dump(DropTableStatement s)
        => $"DROP TABLE {s.Name}";

    public static string Dump(DropIndexStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("DROP INDEX ").Append(s.Name);
        if (s.Table is not null)
            sb.Append(" ON ").Append(s.Table);
        return sb.ToString();
    }

    public static string Dump(AlterTableAddColumnStatement s)
    {
        var sb = new StringBuilder();
        sb.Append("ALTER TABLE ").Append(s.Table);
        sb.Append(" ADD COLUMN ").Append(s.Column.Name);
        if (!string.IsNullOrWhiteSpace(s.Column.Type))
            sb.Append(' ').Append(s.Column.Type);
        return sb.ToString();
    }

    private static string Dump(Expr e)
    {
        return e switch
        {
            LiteralExpr lit => lit.Value is null ? "NULL" : lit.Value is string s ? "'" + s.Replace("'", "''") + "'" : Convert.ToString(lit.Value, CultureInfo.InvariantCulture)!,
            IdentifierExpr id => id.ToString(),
            AllColumnsExpr all => all.ToString(),
            UnaryExpr u => $"({u.Op} {Dump(u.Operand)})",
            BinaryExpr b => $"({Dump(b.Left)} {b.Op} {Dump(b.Right)})",
            InExpr i => $"({Dump(i.Target)} " + (i.Negated ? "NOT IN" : "IN") + " (" + string.Join(", ", i.List.Select(Dump)) + "))",
            CallExpr c => $"{c.Name}(" + string.Join(", ", c.Args.Select(Dump)) + ")",
            _ => e.ToString() ?? e.GetType().Name,
        };
    }

    private static string JoinKindToSql(JoinKind kind) => kind switch
    {
        JoinKind.Inner => "INNER",
        JoinKind.Left => "LEFT",
        JoinKind.Right => "RIGHT",
        JoinKind.Full => "FULL",
        _ => "INNER"
    };
}
#endregion




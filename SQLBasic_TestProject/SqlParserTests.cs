using Xunit;
using Xunit.Abstractions;
using SQLBasic_net.Services;

namespace SQLBasic_TestProject;

public class SqlParserTests
{
    private readonly ITestOutputHelper _output;

    public SqlParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // -------------------------------------------------------------------------
    // SELECT
    // -------------------------------------------------------------------------

    /*
     * シンプルな SELECT 文のパーステスト
     * 列数・FROM テーブル名を確認する
     */
    [Fact]
    public void SqlParserTest_Select_Simple()
    {
        var sql = "SELECT id, name FROM users";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.Equal(2, sel.Items.Count);
        Assert.NotNull(sel.From);
        Assert.IsType<NamedTable>(sel.From!.Base);
        Assert.Equal("users", ((NamedTable)sel.From.Base).Name.ToString());
    }

    /*
     * WHERE 句を含む SELECT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Select_WithWhere()
    {
        var sql = "SELECT id, name FROM users WHERE id = 1";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.Equal(2, sel.Items.Count);
        Assert.NotNull(sel.Where);
    }

    /*
     * LEFT JOIN を含む SELECT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Select_WithLeftJoin()
    {
        var sql = "SELECT a.id, b.name FROM orders AS a LEFT JOIN users AS b ON a.user_id = b.id";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.NotNull(sel.From);
        var join = Assert.Single(sel.From!.Joins);
        Assert.Equal(JoinKind.Left, join.Kind);
    }

    /*
     * ORDER BY と LIMIT を含む SELECT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Select_WithOrderByAndLimit()
    {
        var sql = "SELECT id, name FROM users ORDER BY id DESC LIMIT 10";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.NotNull(sel.OrderBy);
        var orderItem = Assert.Single(sel.OrderBy!);
        Assert.True(orderItem.Descending);
        Assert.Equal(10, sel.Limit);
    }

    /*
     * GROUP BY と HAVING を含む SELECT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Select_WithGroupByHaving()
    {
        var sql = "SELECT category, COUNT(id) FROM items GROUP BY category HAVING COUNT(id) > 3";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.NotNull(sel.GroupBy);
        Assert.Single(sel.GroupBy!);
        Assert.NotNull(sel.Having);
    }

    /*
     * DISTINCT を含む SELECT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Select_Distinct()
    {
        var sql = "SELECT DISTINCT category FROM items";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<SelectStatement>(stmt);
        var sel = (SelectStatement)stmt;
        Assert.Equal(SelectResultModifier.Distinct, sel.Modifier);
    }

    // -------------------------------------------------------------------------
    // INSERT
    // -------------------------------------------------------------------------

    /*
     * カラムリスト付き INSERT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Insert_WithColumns()
    {
        var sql = "INSERT INTO items (id, name) VALUES (1, 'Apple')";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<InsertStatement>(stmt);
        var ins = (InsertStatement)stmt;
        Assert.Equal("items", ins.Into.Name.ToString());
        Assert.NotNull(ins.Columns);
        Assert.Equal(2, ins.Columns!.Count);
        Assert.Equal("id", ins.Columns[0]);
        Assert.Equal("name", ins.Columns[1]);
        Assert.NotNull(ins.ValuesRows);
        var row = Assert.Single(ins.ValuesRows!);
        Assert.Equal(2, row.Count);
    }

    /*
     * カラムリストなしの INSERT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Insert_WithoutColumns()
    {
        var sql = "INSERT INTO logs VALUES (100, 'info', '2025-01-01')";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<InsertStatement>(stmt);
        var ins = (InsertStatement)stmt;
        Assert.Equal("logs", ins.Into.Name.ToString());
        Assert.Null(ins.Columns);
        Assert.NotNull(ins.ValuesRows);
        var row = Assert.Single(ins.ValuesRows!);
        Assert.Equal(3, row.Count);
    }

    /*
     * 複数行 VALUES を持つ INSERT 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Insert_MultipleValues()
    {
        var sql = "INSERT INTO items (id, name) VALUES (1, 'Apple'), (2, 'Banana')";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<InsertStatement>(stmt);
        var ins = (InsertStatement)stmt;
        Assert.NotNull(ins.ValuesRows);
        Assert.Equal(2, ins.ValuesRows!.Count);
    }

    // -------------------------------------------------------------------------
    // UPDATE
    // -------------------------------------------------------------------------

    /*
     * WHERE 句付き UPDATE 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Update_WithWhere()
    {
        var sql = "UPDATE items SET name = 'Banana' WHERE id = 1";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<UpdateStatement>(stmt);
        var upd = (UpdateStatement)stmt;
        Assert.Equal("items", upd.Target.Name.ToString());
        var assignment = Assert.Single(upd.SetList);
        Assert.Equal("name", assignment.Column.ToString());
        Assert.NotNull(upd.Where);
    }

    /*
     * 複数カラムを SET する UPDATE 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Update_MultipleSet()
    {
        var sql = "UPDATE items SET name = 'Cherry', price = 200 WHERE id = 3";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<UpdateStatement>(stmt);
        var upd = (UpdateStatement)stmt;
        Assert.Equal(2, upd.SetList.Count);
        Assert.NotNull(upd.Where);
    }

    /*
     * WHERE 句なしの UPDATE 文のパーステスト
     */
    [Fact]
    public void SqlParserTest_Update_WithoutWhere()
    {
        var sql = "UPDATE items SET price = 0";
        var stmt = new Parser(sql).ParseStatement();

        _output.WriteLine($"SQL: {sql}");

        Assert.IsType<UpdateStatement>(stmt);
        var upd = (UpdateStatement)stmt;
        Assert.Single(upd.SetList);
        Assert.Null(upd.Where);
    }

    // -------------------------------------------------------------------------
    // 異常系
    // -------------------------------------------------------------------------

    /*
     * 不正な SQL を渡した場合に例外がスローされることを確認する
     */
    [Fact]
    public void SqlParserTest_InvalidSql_ThrowsException()
    {
        var sql = "INVALID QUERY HERE";
        _output.WriteLine($"SQL: {sql}");

        Assert.Throws<Exception>(() => new Parser(sql).ParseStatement());
    }

    /*
     * 閉じカッコが欠落した INSERT で例外がスローされることを確認する
     */
    [Fact]
    public void SqlParserTest_Insert_MissingParen_ThrowsException()
    {
        var sql = "INSERT INTO items (id, name VALUES (1, 'Apple')";
        _output.WriteLine($"SQL: {sql}");

        Assert.Throws<Exception>(() => new Parser(sql).ParseStatement());
    }
}

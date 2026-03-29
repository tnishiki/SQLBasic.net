using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SQLBasic_net.Services;
using Xunit.Abstractions;

namespace SQLBasic_TestProject;

public class SqlFormatterTests
{
    private readonly ICoreService _coreService;
    private readonly ITestOutputHelper _output;

    public SqlFormatterTests(ITestOutputHelper output)
    {
        _output = output;

        // テスト用のDIコンテナを構築
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IWindowProvider, WindowProvider>();
                services.AddSingleton<ICoreService, CoreService>();
            })
            .Build();

        _coreService = host.Services.GetRequiredService<ICoreService>();
    }

    /*
     * シンプルな SELECT 文のテスト
     */
    [Fact]
    public void SQLFormatterTest01()
    {
        #region テストコード01
        var sql = "SELECT 1 AS Value from a";
        var expected =
@"SELECT
  1 AS Value
from
  a";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * WHERE、ORDER BY 句のテスト
     */
    [Fact]
    public void SQLFormatterTest02()
    {
        #region テストコード02
        var sql = "SELECT a.item as T from aaa as a where a.id = 3 order by a.id";
        var expected =
@"SELECT
  a.item as T
from
  aaa as a
where
  a.id = 3
order by
  a.id";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * JOIN 句のテスト
     */
    [Fact]
    public void SQLFormatterTest03()
    {
        #region テストコード03
        var sql = "SELECT a.item as T from aaa as a left join bbb as b on a.id = b.a_id where a.id = 3 order by a.id";
        var expected =
@"SELECT
  a.item as T
from
  aaa as a
  left join bbb as b on a.id = b.a_id
where
  a.id = 3
order by
  a.id";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * サブクエリを含む JOIN のテスト
     */
    [Fact]
    public void SQLFormatterTest04()
    {
        #region テストコード04
        var sql = "SELECT a.item as T from aaa as a left join (select b.id, b.name from bbb as b where b.id = 8) as b on a.id = b.id where a.id = 3 order by a.id";
        var expected =
@"SELECT
  a.item as T
from
  aaa as a
  left join (
  select
    b.id
    , b.name
  from
    bbb as b
  where
    b.id = 8) as b on a.id = b.id
where
  a.id = 3
order by
  a.id";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * INSERT クエリのテスト
     * カラムリスト・VALUES の整形を確認する
     */
    [Fact]
    public void SQLFormatterTest05()
    {
        #region テストコード05
        var sql = "INSERT INTO items (id, name) VALUES (1, 'Apple')";
        var expected =
@"  INSERT INTO items (id,
    name) VALUES (1,
    'Apple')";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * UPDATE クエリのテスト
     * SET 句と WHERE 句の整形を確認する
     */
    [Fact]
    public void SQLFormatterTest06()
    {
        #region テストコード06
        var sql = "UPDATE items SET name = 'Banana' WHERE id = 1";
        var expected =
@"  UPDATE items SET name = 'Banana'
WHERE
  id = 1";
        #endregion

        // 整形されたテキストを反映
        var result = SimpleSqlFormatter.Format(sql);

        // Debug出力
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }
}

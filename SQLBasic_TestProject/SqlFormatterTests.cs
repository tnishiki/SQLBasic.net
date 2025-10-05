using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SQLBasic_net.Services;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace SQLBasic_TestProject;

public class SqlFormatterTests
{
    private readonly ICoreService _coreService;
    private readonly ITestOutputHelper _output;

    public SqlFormatterTests(ITestOutputHelper output)
    {
        _output = output;

        // �e�X�g�p��DI�R���e�i���\�z
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
     * �V���v����SELECT���̃e�X�g
     */
    [Fact]
    public void QueryTest01()
    {
        #region �e�X�g�R�[�h01
        var sql = "SELECT 1 AS Value from a";
        var expected =
@"SELECT
  1 AS Value
from
  a";
        #endregion

        // ���`�����e�L�X�g�𔽉f
        var result = SimpleSqlFormatter.Format(sql);

        // Debug�o��
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }
    /*
     * WHERE�AORDER BY ��̃e�X�g
     */
    [Fact]
    public void QueryTest02()
    {
        #region �e�X�g�R�[�h02
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

        // ���`�����e�L�X�g�𔽉f
        var result = SimpleSqlFormatter.Format(sql);

        // Debug�o��
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    /*
     * JOIN ��̃e�X�g
     */
    [Fact]
    public void QueryTest03()
    {
        #region �e�X�g�R�[�h03
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

        // ���`�����e�L�X�g�𔽉f
        var result = SimpleSqlFormatter.Format(sql);

        // Debug�o��
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void QueryTest04()
    {
        #region �e�X�g�R�[�h04
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
      b.id = 8
  ) as b on a.id = b.id
where
  a.id = 3
order by
  a.id";
        #endregion

        // ���`�����e�L�X�g�𔽉f
        var result = SimpleSqlFormatter.Format(sql);

        // Debug�o��
        _output.WriteLine($"Expected:{expected}");
        _output.WriteLine($"Result:{result}");

        Assert.Equal(expected, result);
    }


}
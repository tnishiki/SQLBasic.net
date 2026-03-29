using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SQLBasic_net.Services;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;
using Microsoft.Win32;

namespace SQLBasic_TestProject;

public class RegistryTests
{
    private readonly ICoreService _coreService;
    private readonly ITestOutputHelper _output;

    public RegistryTests(ITestOutputHelper output)
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

    private void DropRegistryKey()
    {
        try
        {
            string RegistryPath = _coreService.GetRegistryBasePath();
            Registry.CurrentUser.DeleteSubKeyTree(RegistryPath);
        }
        catch (Exception err)
        {
            Assert.Fail($"レジストリの削除に失敗しました。 {err.Message}");
        }
    }

#if DEBUG
    [Fact]
    public void RegistryTest01()
    {
        DropRegistryKey();

        string RegistryPath = _coreService.GetRegistryBasePath();

        _coreService.CheckLocalDB();

        string RegistryDbPath = $@"{RegistryPath}\DB";

        var dbKey = Registry.CurrentUser.OpenSubKey(RegistryDbPath, writable: true);
        if (dbKey == null)
        {
            Assert.Fail("レジストリのキーが存在しません。");
        }
        var folderPath = dbKey?.GetValue("FolderPath") as string;

        if (!Directory.Exists(folderPath))
        {
            Assert.Fail("DBフォルダが存在しません。");
        }

        string dbPath = Path.Combine(folderPath, "local.db");

        if (!File.Exists(dbPath))
        {
            Assert.Fail("DBファイルが存在しません。");
        }
    }
    [Fact]
    public void RegistryTest02()
    {
        DropRegistryKey();

        string RegistryPath = _coreService.GetRegistryBasePath();
        string RegistrySyntaxPath = $@"{RegistryPath}\Syntax";

        for (int i = 0; i < 11; i++)
        {
            _coreService.GetSyntaxColor(i);
        }

        var dbKey = Registry.CurrentUser.OpenSubKey(RegistrySyntaxPath, writable: false);
        if (dbKey == null)
        {
            Assert.Fail("レジストリのキーが存在しません。");
        }

        for (int i = 0; i < 11; i++)
        {
            string? col = dbKey.GetValue($"Color{i}") as string;

            if (col == null)
            {
                Assert.Fail($"フォントカラー No.{i}の情報が登録されていません。");
            }
        }
    }

    /*
     * SetSyntaxColor でカラーを書き込み、レジストリに正しく保存されることを確認する
     */
    [Fact]
    public void RegistryTest03()
    {
        string RegistryPath = _coreService.GetRegistryBasePath();
        string RegistrySyntaxPath = $@"{RegistryPath}\Syntax";

        var brush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x12, 0x34, 0x56));

        bool result = _coreService.SetSyntaxColor(0, brush);
        Assert.True(result, "SetSyntaxColor が false を返しました。");

        var dbKey = Registry.CurrentUser.OpenSubKey(RegistrySyntaxPath, writable: false);
        if (dbKey == null)
        {
            Assert.Fail("レジストリのキーが存在しません。");
        }

        string? col = dbKey!.GetValue("Color0") as string;
        Assert.Equal("#123456", col);
    }

    /*
     * SetSyntaxColor で書き込んだカラーを GetSyntaxColor で読み戻せることを確認する
     */
    [Fact]
    public void RegistryTest04()
    {
        var expected = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xAA, 0xBB, 0xCC));

        _coreService.SetSyntaxColor(1, expected);

        var actual = _coreService.GetSyntaxColor(1) as System.Windows.Media.SolidColorBrush;
        Assert.NotNull(actual);
        Assert.Equal(expected.Color.R, actual!.Color.R);
        Assert.Equal(expected.Color.G, actual.Color.G);
        Assert.Equal(expected.Color.B, actual.Color.B);
    }

    /*
     * GetRegistryBasePath が期待するパスを返すことを確認する
     */
    [Fact]
    public void RegistryTest05()
    {
        string path = _coreService.GetRegistryBasePath();
        Assert.Equal(@"Software\oecu\SQLBasic_net", path);
    }

#endif
}

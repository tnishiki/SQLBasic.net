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

#endif
}

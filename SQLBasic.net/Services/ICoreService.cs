using System.Windows.Media;

namespace SQLBasic_net.Services;

public interface ICoreService
{
    string GetRegistryBasePath();

#if DEBUG
    public bool CheckLocalDB();
#endif

        Task<List<string>> GetTableNames();
    Task<(List<string?> Headers, List<object[]> Rows, string Message)> CallDBQuery(string SQL);
    (string, TokenKind tokenKind) CheckSQL(string SQL);
    Task<string> CallDBExecute(string SQL);

    string GetStringColorCode(Brush brush);
    Brush GetSyntaxColor(int SyntaxNo);
    bool SetSyntaxColor(int SyntaxNo, Brush brush);
    string GetSytaxXml(string[]? colors);
    IEnumerable<string>? GetCandicateDatabaseItem(string documentText, int caretOffset);
}

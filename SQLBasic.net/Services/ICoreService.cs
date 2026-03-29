using System.Windows.Media;
using SQLBasic_net.Datas;

namespace SQLBasic_net.Services;

public interface ICoreService
{
    string GetRegistryBasePath();

#if DEBUG
    bool InitializeLocalDB();
#endif
    bool ConnectToDb(string dbfilePath);

    Task<List<string>> GetTableNamesAsync();
    Task<List<ColumnInfo>> GetColumnInfosAsync(string tableName);
    Task<(List<string?> Headers, List<object[]> Rows, string Message)> CallDbQueryAsync(string sql);
    (string, TokenKind tokenKind) CheckSql(string sql);
    Task<(int AffectedRows, string Message)> CallDbExecuteAsync(string sql);

    string GetStringColorCode(Brush brush);
    Brush GetSyntaxColor(int SyntaxNo);
    bool SetSyntaxColor(int SyntaxNo, Brush brush);
    string GetSyntaxXml(string[]? colors);
    (IEnumerable<string>? Candidates, string Header) GetCandidateDatabaseItem(string documentText, int caretOffset);
    (string newText, int newCaretOffset) SetSqlComment(string documentText, int caretOffset, int selectionStart, int selectionLength);
    (string newText, int newCaretOffset) RemoveSqlComment(string documentText, int caretOffset, int selectionStart, int selectionLength);
}

using System.Text;
using System.Text.RegularExpressions;

namespace SQLBasic_net.Services;

public static class SimpleSqlFormatter
{
    public static int IndentSpaceNum = 2;

    public static string Format(string sql)
    {
        var sb = new StringBuilder();

        // 1)主要キーワードの前で改行
        sql = Regex.Replace(sql, @"\b(select|from|where|order\s+by|group\s+by|having)\b",
            m => "\n" + m.Value + "\n", RegexOptions.IgnoreCase);

        // 2)JOIN 句の直前で改行（LEFT/RIGHT/INNER [+ OUTER] JOIN をひとかたまりで扱う）
        sql = Regex.Replace(sql, @"\b((left|right|inner)\s+(?:outer\s+)?join)\b",
            m => "\n" + m.Value, RegexOptions.IgnoreCase);

        // 3)カンマの後で改行
        sql = Regex.Replace(sql, @",", ",\n");

        // 4)ブロックコメントを独立行に整形
        sql = Regex.Replace(sql, @"/\*([\s\S]*?)\*/",
            m =>
            {
                var inner = m.Groups[1].Value.Trim();
                var lines = inner.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                 .Select(l => " " + l.TrimEnd());
                return "\n/*\n" + string.Join("\n", lines) + "\n*/\n";
            });

        // 5)行ごとのインデント調整
        var lines2 = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        bool inSelect = false;
        bool inWhere = false;
        int colIndex = 0;

        foreach (var raw in lines2)
        {
            var org = raw.Trim();
            var line = org.ToLower();

            if (line.Length == 0) continue;

            if (Regex.IsMatch(line, @"^select$", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(org);
                inSelect = true;
                inWhere = false;
                colIndex = 0;
            }
            else if (Regex.IsMatch(line, @"^where$", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(org);
                inSelect = false;
                inWhere = true;
                colIndex = 0;
            }
            else if (Regex.IsMatch(line, @"^(from|order by|group by|having|join)", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(org);
                inSelect = false;
                inWhere = false;
            }
            else if (line.StartsWith("/*"))
            {
                // コメントはそのまま出力
                sb.AppendLine(org);
            }
            else if (line.StartsWith("*/"))
            {
                // コメントはそのまま出力
                sb.AppendLine(org);
            }
            else if (inSelect)
            {
                if (colIndex == 0)
                    sb.AppendLine(new string(' ', IndentSpaceNum) + org.Trim(','));
                else
                    sb.AppendLine(new string(' ', IndentSpaceNum) + ", " + org.Trim(','));
                colIndex++;
            }
            else if (inWhere)
            {
                var split = Regex.Replace(
                    org,
                    @"\s+(?i:and)\s+", "\n" + new string(' ', IndentSpaceNum) + "and ", RegexOptions.IgnoreCase);
                sb.AppendLine(new string(' ', IndentSpaceNum) + split);
                inWhere = false;
            }
            else
            {
                sb.AppendLine(new string(' ', IndentSpaceNum) + org);
            }
        }

        return sb.ToString().TrimEnd();
    }
}

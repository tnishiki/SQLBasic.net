using System.Text;
using System.Text.RegularExpressions;

namespace SQLBasic_net.Services;

public static class SimpleSqlFormatter
{
    public static int IndentSpaceNum = 2;

    public static string Format(string sql)
    {
        var sb = new StringBuilder();

        // 1) 主要キーワードの前後で改行（ケースは保持）
        sql = Regex.Replace(sql,
            @"\b(select|from|where|order\s+by|group\s+by|having)\b",
            m => "\n" + m.Value + "\n",
            RegexOptions.IgnoreCase);

        // 2) JOIN 句の直前で改行（LEFT/RIGHT/INNER [+ OUTER] JOIN をひとかたまり）
        sql = Regex.Replace(sql,
            @"\b((left|right|inner)\s+(?:outer\s+)?join)\b",
            m => "\n" + m.Value,
            RegexOptions.IgnoreCase);

        // 2.5) 「(select|from|where...)」を「(\n<keyword>」に分割してサブクエリを次行から開始
        sql = Regex.Replace(sql,
            @"\(\s*(?=(select|from|where|order\s+by|group\s+by|having)\b)",
            "(\n",
            RegexOptions.IgnoreCase);

        // 3) カンマの後で改行（列の先頭カンマ表記は下の inSelect ロジックで作る）
        sql = Regex.Replace(sql, @",", ",\n");

        // 4) ブロックコメントを独立行に整形
        sql = Regex.Replace(sql, @"/\*([\s\S]*?)\*/",
            m =>
            {
                var inner = m.Groups[1].Value.Trim();
                var lines = inner.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                 .Select(l => " " + l.TrimEnd());
                return "\n/*\n" + string.Join("\n", lines) + "\n*/\n";
            });

        // 5) 行ごとのインデント調整（入れ子に応じて増減）
        var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        bool inSelect = false;
        bool inWhere = false;
        int colIndex = 0;
        int indentLevel = 0; // '(' で +1, ')' で -1

        foreach (var raw in lines)
        {
            var org = raw.Trim();
            if (org.Length == 0) continue;

            var lower = org.ToLower();

            // --- 出力前: 先頭の連続 ')' をカウントして、その数だけ戻す
            int leadingCloses = 0;
            while (leadingCloses < org.Length && org[leadingCloses] == ')')
                leadingCloses++;

            if (leadingCloses > 0)
                indentLevel = Math.Max(0, indentLevel - leadingCloses);

            // インデント幅の計算
            int baseIndent = IndentSpaceNum * indentLevel;
            int bodyIndent = IndentSpaceNum * (indentLevel + 1);

            // キーワード行の判定（出力はオリジナルのケースで）
            if (Regex.IsMatch(lower, @"^select$", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(new string(' ', baseIndent) + org);
                inSelect = true; inWhere = false; colIndex = 0;
            }
            else if (Regex.IsMatch(lower, @"^where$", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(new string(' ', baseIndent) + org);
                inSelect = false; inWhere = true; colIndex = 0;
            }
            else if (Regex.IsMatch(lower, @"^(from|order by|group by|having)\b", RegexOptions.IgnoreCase))
            {
                sb.AppendLine(new string(' ', baseIndent) + org);
                inSelect = false; inWhere = false;
            }
            else if (lower.StartsWith("/*") || lower.StartsWith("*/"))
            {
                // コメントはそのまま（現在レベルに合わせて字下げ）
                sb.AppendLine(new string(' ', baseIndent) + org);
            }
            else if (inSelect)
            {
                // SELECT 句の列。1行目は先頭カンマなし、2行目以降は先頭に ", "
                if (colIndex == 0)
                    sb.AppendLine(new string(' ', bodyIndent) + org.Trim(','));
                else
                    sb.AppendLine(new string(' ', bodyIndent) + ", " + org.Trim(','));
                colIndex++;
            }
            else if (inWhere)
            {
                // WHERE 句：AND の前で改行（AND のケースは保持）
                var split = Regex.Replace(
                    org,
                    @"\s+(and)\s+",
                    m => "\n" + new string(' ', bodyIndent) + m.Groups[1].Value + " ",
                    RegexOptions.IgnoreCase);
                sb.AppendLine(new string(' ', bodyIndent) + split);
                inWhere = false; // 単純化（必要に応じて継続ロジックに）
            }
            else
            {
                // 通常行（テーブル、JOIN 行、ON 条件行、閉じカッコ行など）
                sb.AppendLine(new string(' ', bodyIndent) + org);
            }

            // 行内の '(' で次行から一段深くする（カッコ内開始）
            // ※ ')' は先頭チェックで既に反映済み。行内に複数の '(' があっても対応。
            int opens = org.Count(ch => ch == '(');
            int closes = org.Count(ch => ch == ')');
            closes = Math.Max(0, closes - leadingCloses);

            indentLevel = Math.Max(0, indentLevel + opens - closes);
        }

        return sb.ToString().TrimEnd();
    }
}
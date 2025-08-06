using System.Text.RegularExpressions;

namespace FreeSql.Various.Utilitys;

internal class DatabaseNameTemplateReplacer
{
    public static string ReplaceTemplate(string template, Dictionary<string, string> variables)
    {
        // 匹配{占位符}格式的正则表达式
        return Regex.Replace(template, @"\{(\w+)\}", match =>
        {
            // 尝试从字典中获取对应的值
            if (variables.TryGetValue(match.Groups[1].Value, out var value))
            {
                return value;
            }

            // 如果找不到对应的值，保留原始占位符
            return match.Value;
        });
    }
}
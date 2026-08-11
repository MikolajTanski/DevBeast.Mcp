using System.Text.RegularExpressions;

namespace DevBeast.Mcp.Server.Security;

public static class SqlQueryValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER",
        "TRUNCATE", "CREATE", "EXEC", "EXECUTE", "MERGE"
    ];

    public static void EnsureReadOnly(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new InvalidOperationException("SQL query cannot be empty.");
        }

        var normalized = sqlQuery.Trim();

        if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only SELECT queries (including CTEs starting with WITH) are allowed.");
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (ForbiddenKeywordPattern(keyword).IsMatch(normalized))
            {
                throw new InvalidOperationException($"Query rejected: forbidden keyword '{keyword}' detected.");
            }
        }
    }

    private static Regex ForbiddenKeywordPattern(string keyword) =>
        new($@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}

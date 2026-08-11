using DevBeast.Mcp.Server.Models;

namespace DevBeast.Mcp.Server.Services;

public interface IDatabaseService
{
    Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken cancellationToken = default);
    Task<DatabaseSchemaResult> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default);
    Task<QueryResult> ExecuteReadQueryAsync(string sqlQuery, CancellationToken cancellationToken = default);
}

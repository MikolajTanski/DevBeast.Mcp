using System.Text.Json;
using System.Text.RegularExpressions;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public sealed partial class FileLogService(IOptions<DevBeastOptions> options) : ILogService
{
    public async Task<IReadOnlyList<AggregatedError>> GetRecentErrorsAsync(
        int timeWindowMinutes,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        if (timeWindowMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeWindowMinutes), "Time window must be greater than zero.");
        }

        var logDirectory = options.Value.Logs.Directory;
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new InvalidOperationException("Log directory is not configured.");
        }

        if (!Directory.Exists(logDirectory))
        {
            throw new DirectoryNotFoundException($"Log directory not found: {logDirectory}");
        }

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-timeWindowMinutes);
        var filePattern = options.Value.Logs.FilePattern;
        var files = Directory.GetFiles(logDirectory, filePattern, SearchOption.AllDirectories);

        var rawErrors = new List<ParsedLogError>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!MatchesEnvironment(file, environment))
            {
                continue;
            }

            await foreach (var entry in ParseLogFileAsync(file, cancellationToken))
            {
                if (entry.Timestamp >= cutoff)
                {
                    rawErrors.Add(entry);
                }
            }
        }

        return AggregateErrors(rawErrors);
    }

    private static bool MatchesEnvironment(string filePath, string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            return true;
        }

        return filePath.Contains(environment, StringComparison.OrdinalIgnoreCase);
    }

    private static async IAsyncEnumerable<ParsedLogError> ParseLogFileAsync(
        string filePath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            var parsed = TryParseLine(line, filePath);
            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    private static ParsedLogError? TryParseLine(string line, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var jsonParsed = TryParseJsonLog(line, sourceFile);
        if (jsonParsed is not null)
        {
            return jsonParsed;
        }

        return TryParseSerilogTextLog(line, sourceFile);
    }

    private static ParsedLogError? TryParseJsonLog(string line, string sourceFile)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (!root.TryGetProperty("@l", out var levelProp)
                && !root.TryGetProperty("Level", out levelProp))
            {
                return null;
            }

            var level = levelProp.GetString();
            if (level is null || !level.Contains("Error", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var timestamp = ParseTimestamp(root);
            var message = root.TryGetProperty("@m", out var msgProp)
                ? msgProp.GetString()
                : root.TryGetProperty("Message", out msgProp) ? msgProp.GetString() : null;

            var exceptionType = root.TryGetProperty("ExceptionType", out var typeProp)
                ? typeProp.GetString()
                : ExtractExceptionType(message);

            var stackTrace = root.TryGetProperty("Exception", out var exProp)
                ? exProp.GetString()
                : root.TryGetProperty("StackTrace", out var stProp) ? stProp.GetString() : null;

            var environment = root.TryGetProperty("Environment", out var envProp)
                ? envProp.GetString()
                : null;

            return new ParsedLogError(
                timestamp,
                exceptionType ?? "UnknownException",
                message ?? "Unknown error",
                stackTrace,
                environment,
                sourceFile);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ParsedLogError? TryParseSerilogTextLog(string line, string sourceFile)
    {
        var match = SerilogErrorPattern().Match(line);
        if (!match.Success)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var timestamp))
        {
            timestamp = DateTimeOffset.UtcNow;
        }

        var message = match.Groups["message"].Value.Trim();
        var exceptionType = ExtractExceptionType(message) ?? "ApplicationException";

        return new ParsedLogError(timestamp, exceptionType, message, null, null, sourceFile);
    }

    private static DateTimeOffset ParseTimestamp(JsonElement root)
    {
        if (root.TryGetProperty("@t", out var timestampProp)
            && DateTimeOffset.TryParse(timestampProp.GetString(), out var timestamp))
        {
            return timestamp;
        }

        if (root.TryGetProperty("Timestamp", out timestampProp)
            && DateTimeOffset.TryParse(timestampProp.GetString(), out timestamp))
        {
            return timestamp;
        }

        return DateTimeOffset.UtcNow;
    }

    private static string? ExtractExceptionType(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = ExceptionTypePattern().Match(message);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static IReadOnlyList<AggregatedError> AggregateErrors(IReadOnlyList<ParsedLogError> errors)
    {
        return errors
            .GroupBy(e => (e.ExceptionType, NormalizeMessage(e.Message), e.StackTrace ?? string.Empty))
            .Select(group =>
            {
                var ordered = group.OrderBy(e => e.Timestamp).ToList();
                return new AggregatedError(
                    group.Key.ExceptionType,
                    ordered[0].Message,
                    string.IsNullOrWhiteSpace(group.Key.Item3) ? null : group.Key.Item3,
                    group.Count(),
                    ordered[0].Timestamp,
                    ordered[^1].Timestamp,
                    ordered.Select(e => e.Environment).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                    group.Select(e => e.SourceFile).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            })
            .OrderByDescending(e => e.LastSeen)
            .ThenByDescending(e => e.OccurrenceCount)
            .ToList();
    }

    private static string NormalizeMessage(string message) =>
        WhitespacePattern().Replace(message.Trim(), " ");

    [GeneratedRegex(@"\[(?<timestamp>[^\]]+)\]\s+\[[^\]]+\]\s+(?<message>.+)", RegexOptions.CultureInvariant)]
    private static partial Regex SerilogErrorPattern();

    [GeneratedRegex(@"(\w+(?:\.\w+)+Exception)", RegexOptions.CultureInvariant)]
    private static partial Regex ExceptionTypePattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();

    private sealed record ParsedLogError(
        DateTimeOffset Timestamp,
        string ExceptionType,
        string Message,
        string? StackTrace,
        string? Environment,
        string SourceFile);
}

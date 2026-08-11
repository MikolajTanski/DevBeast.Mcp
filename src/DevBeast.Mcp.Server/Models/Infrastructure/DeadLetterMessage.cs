namespace DevBeast.Mcp.Server.Models;

public sealed record DeadLetterMessage(
    string MessageId,
    string Queue,
    string Reason,
    string Payload,
    string Error,
    DateTimeOffset FailedAt,
    int RetryCount);

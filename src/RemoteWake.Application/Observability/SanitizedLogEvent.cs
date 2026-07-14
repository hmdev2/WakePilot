using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Application.Observability;

public enum LogEventLevel
{
    Information,
    Warning,
    Error,
}

public sealed class SanitizedLogEvent
{
    internal SanitizedLogEvent(
        DateTimeOffset utcTime,
        LogEventLevel level,
        string eventName,
        ErrorCode? code,
        string component,
        CorrelationId correlationId,
        string outcome,
        IReadOnlyDictionary<string, string> properties)
    {
        UtcTime = utcTime;
        Level = level;
        EventName = eventName;
        Code = code;
        Component = component;
        CorrelationId = correlationId;
        Outcome = outcome;
        Properties = properties;
    }

    public DateTimeOffset UtcTime { get; }

    public LogEventLevel Level { get; }

    public string EventName { get; }

    public ErrorCode? Code { get; }

    public string Component { get; }

    public CorrelationId CorrelationId { get; }

    public string Outcome { get; }

    public IReadOnlyDictionary<string, string> Properties { get; }
}

public static class LogSanitizer
{
    private const int MaximumValueLength = 128;

    private static readonly HashSet<string> AllowedPropertyNames =
        new(StringComparer.Ordinal)
        {
            "attempt",
            "nextState",
            "outcome",
            "phase",
            "previousState",
            "retryable",
            "state",
        };

    private static readonly string[] SensitiveMarkers =
    [
        "auth key",
        "password",
        "private key",
        "secret",
        "token",
    ];

    public static IReadOnlyDictionary<string, string> SanitizeProperties(
        IEnumerable<KeyValuePair<string, string>> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return properties
            .Where(property => AllowedPropertyNames.Contains(property.Key))
            .ToDictionary(
                property => property.Key,
                property => SanitizeValue(property.Value),
                StringComparer.Ordinal);
    }

    public static SanitizedLogEvent CreateTransitionEvent(
        DateTimeOffset utcTime,
        CorrelationId correlationId,
        WakeTransition transition)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["previousState"] = transition.Previous.ToString(),
            ["nextState"] = transition.Current.ToString(),
        };

        return new SanitizedLogEvent(
            utcTime,
            LogEventLevel.Information,
            "wake.state_changed",
            null,
            "WakeOrchestrator",
            correlationId,
            "transitioned",
            SanitizeProperties(properties));
    }

    public static SanitizedLogEvent CreateFailureEvent(
        DateTimeOffset utcTime,
        CorrelationId correlationId,
        DomainError error,
        WakeState state)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["retryable"] = error.IsRetryable.ToString(),
            ["state"] = state.ToString(),
        };

        return new SanitizedLogEvent(
            utcTime,
            LogEventLevel.Error,
            "wake.failed",
            error.Code,
            "WakeOrchestrator",
            correlationId,
            "failed",
            SanitizeProperties(properties));
    }

    private static string SanitizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || ContainsSensitiveMarker(value))
        {
            return "[REDACTED]";
        }

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ');
        return normalized.Length <= MaximumValueLength
            ? normalized
            : normalized[..MaximumValueLength];
    }

    private static bool ContainsSensitiveMarker(string value) =>
        SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}

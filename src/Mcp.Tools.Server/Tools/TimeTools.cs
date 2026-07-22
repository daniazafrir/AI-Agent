using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class TimeTools
{
    [McpServerTool(Name = "get_current_time")]
    [Description(
        "Gets the real current date and time for an IANA time zone. " +
        "Use Asia/Jerusalem for Israeli local time.")]
    public static object GetCurrentTime(
        [Description(
            "IANA time-zone identifier, for example Asia/Jerusalem.")]
        string timeZone,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new ArgumentException(
                "timeZone is required.",
                nameof(timeZone));
        }

        TimeZoneInfo resolvedTimeZone;

        try
        {
            resolvedTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
            when (
                OperatingSystem.IsWindows()
                && timeZone.Equals(
                    "Asia/Jerusalem",
                    StringComparison.OrdinalIgnoreCase))
        {
            resolvedTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Israel Standard Time");
        }

        var utcNow = DateTimeOffset.UtcNow;

        var localNow =
            TimeZoneInfo.ConvertTime(
                utcNow,
                resolvedTimeZone);

        return new
        {
            requestedTimeZone = timeZone,
            resolvedTimeZone = resolvedTimeZone.Id,
            date = localNow.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            time = localNow.ToString(
                "HH:mm:ss",
                CultureInfo.InvariantCulture),
            dayOfWeek = localNow.DayOfWeek.ToString(),
            localDateTime = localNow.ToString(
                "O",
                CultureInfo.InvariantCulture),
            utcDateTime = utcNow.ToString(
                "O",
                CultureInfo.InvariantCulture)
        };
    }
}
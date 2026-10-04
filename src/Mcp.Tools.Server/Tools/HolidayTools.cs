using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class HolidayTools(HttpClient http)
{
    [McpServerTool(Name = "get_jewish_holidays")]
    [Description("Verified Jewish holiday dates from Hebcal for a Gregorian year. Omit year for current year. region: israel, diaspora, or both when unspecified. Dates only, no local entry/exit times. Israel combines Shmini Atzeret and Simchat Torah.")]
    public async Task<object> GetHolidays(int? year = null, string region = "both", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var today = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem"));
        var requestedYear = year ?? today.Year;
        if (requestedYear < 1900 || requestedYear > 2100)
            return new { success = false, error = "unsupported_year", message = "Specify a Gregorian year from 1900 through 2100; not a Hebrew year." };
        if (region is not ("israel" or "diaspora" or "both"))
            return new { success = false, error = "invalid_region" };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            var calendars = new List<object>();
            foreach (var schedule in region == "both" ? new[] { "israel", "diaspora" } : new[] { region })
            {
                var url = $"https://www.hebcal.com/hebcal?v=1&cfg=json&year={requestedYear}&yt=G&maj=on&min=on&mf=on&mod=on&i={(schedule == "israel" ? "on" : "off")}";
                using var response = await http.GetAsync(url, timeout.Token);
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                var items = json.RootElement.GetProperty("items");
                var events = new List<object>();
                foreach (var item in items.EnumerateArray())
                {
                    if (item.GetProperty("category").GetString() != "holiday") continue;
                    var date = DateOnly.ParseExact(item.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (date.Year != requestedYear) throw new JsonException("Unexpected year.");
                    var yomTov = item.TryGetProperty("yomtov", out var value) && value.ValueKind == JsonValueKind.True;
                    events.Add(new {
                        title = item.GetProperty("title").GetString(), date = date.ToString("yyyy-MM-dd"), dayOfWeek = date.DayOfWeek.ToString(),
                        hebrew = item.TryGetProperty("hebrew", out var hebrew) ? hebrew.GetString() : null,
                        hebrewDate = item.TryGetProperty("hdate", out var hdate) ? hdate.GetString() : null,
                        beginsEveningOn = yomTov ? date.AddDays(-1).ToString("yyyy-MM-dd") : null,
                        isYomTov = yomTov
                    });
                }
                if (events.Count == 0) throw new JsonException("No holiday events.");
                calendars.Add(new { region = schedule, sourceUrl = url, events });
            }
            return new { success = true, source = "Hebcal", retrievedAtUtc = now,
                year = requestedYear, currentDate = today.ToString("yyyy-MM-dd"), referenceTimezone = "Asia/Jerusalem", calendars,
                note = "In Israel Shmini Atzeret is also Simchat Torah. beginsEveningOn is the preceding civil date for Yom Tov, not an exact start time. Do not apply this rule to other events without that field. Local entry/exit hours are not supplied." };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            return new { success = false, error = "calendar_unavailable", message = "Holiday dates could not be verified. Do not invent dates or fall back to memory." };
        }
    }
}

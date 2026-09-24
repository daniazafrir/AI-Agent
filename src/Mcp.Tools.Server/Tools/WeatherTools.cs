using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class WeatherTools(HttpClient http)
{
    [McpServerTool(Name = "get_weather")]
    [Description("Gets current weather and a 3-day forecast from Open-Meteo. Supply a city, preferably its English name (Eilat for אילת), and optional ISO country code. Never invent a location. Ambiguous locations require clarification.")]
    public async Task<object> GetWeather(
        [Description("City name, e.g. Eilat. Empty if the user did not specify a location.")] string city,
        [Description("Optional two-letter country code, e.g. IL.")] string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(city))
            return new { success = false, error = "missing_location", message = "Ask the user which city." };
        if (city.Length > 150 || (countryCode is not null &&
            (countryCode.Length != 2 || !countryCode.All(char.IsAsciiLetter))))
            return new { success = false, error = "invalid_location", message = "Supply a city and optional two-letter country code." };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            var geoUrl = "https://geocoding-api.open-meteo.com/v1/search?count=5&language=en&format=json&name=" +
                Uri.EscapeDataString(city.Trim()) +
                (countryCode is null ? "" : "&countryCode=" + countryCode.ToUpperInvariant());
            using var geo = await ReadJson(geoUrl, timeout.Token);
            if (!geo.RootElement.TryGetProperty("results", out var locations) || locations.GetArrayLength() == 0)
                return new { success = false, error = "location_not_found", message = "Ask for another city name or country; no weather was retrieved." };
            var candidates = locations.EnumerateArray().ToArray();
            var exact = candidates.Where(x => string.Equals(
                x.GetProperty("name").GetString(), city.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            // A fuzzy search may return other places alongside the exact city.
            if (exact.Length == 1) candidates = exact;
            if (candidates.Length > 1)
                return new
                {
                    success = false, error = "ambiguous_location",
                    message = "Ask the user to specify the country or region. Retry with City, Region and countryCode.",
                    candidates = candidates.Select(x => new
                    {
                        name = x.GetProperty("name").GetString(),
                        country = x.TryGetProperty("country", out var c) ? c.GetString() : null,
                        region = x.TryGetProperty("admin1", out var a) ? a.GetString() : null
                    }).ToArray()
                };

            var location = candidates[0];
            var latitude = location.GetProperty("latitude").GetDouble().ToString(CultureInfo.InvariantCulture);
            var longitude = location.GetProperty("longitude").GetDouble().ToString(CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}" +
                "&timezone=auto&forecast_days=3&temperature_unit=celsius&wind_speed_unit=kmh" +
                "&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m" +
                "&daily=temperature_2m_max,temperature_2m_min,weather_code,precipitation_probability_max";
            using var forecast = await ReadJson(url, timeout.Token);
            var data = forecast.RootElement;
            return new
            {
                success = true,
                source = "Open-Meteo", sourceUrl = "https://open-meteo.com/",
                retrievedAtUtc = DateTimeOffset.UtcNow,
                location = location.Clone(),
                timezone = data.GetProperty("timezone").GetString(),
                current = data.GetProperty("current").Clone(),
                currentUnits = data.GetProperty("current_units").Clone(),
                daily = data.GetProperty("daily").Clone(),
                dailyUnits = data.GetProperty("daily_units").Clone(),
                note = "Current conditions are weather model estimates, not a live station observation. Times are local to the returned timezone. Weather codes use WMO definitions."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException
            or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return new { success = false, error = "weather_unavailable",
                message = "The weather service is unavailable. Tell the user current weather could not be retrieved; do not guess." };
        }
    }

    private async Task<JsonDocument> ReadJson(string url, CancellationToken token)
    {
        using var response = await http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
    }
}

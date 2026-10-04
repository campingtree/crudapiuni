using System.Globalization;
using System.Text.Json;

namespace CrudApp.Services;

public sealed class OpenMeteoForecastClient(HttpClient client)
{
    public async Task<ForecastSnapshot> GetTomorrowAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var url = $"v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max,weather_code" +
                  "&timezone=auto&forecast_days=2";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var daily = document.RootElement.GetProperty("daily");
        var date = DateOnly.Parse(daily.GetProperty("time")[1].GetString()!, CultureInfo.InvariantCulture);
        return new ForecastSnapshot(
            date,
            GetDoubleOrNull(daily, "temperature_2m_min"),
            GetDoubleOrNull(daily, "temperature_2m_max"),
            GetIntOrNull(daily, "precipitation_probability_max"),
            GetIntOrNull(daily, "weather_code"));
    }

    private static double? GetDoubleOrNull(JsonElement daily, string name)
    {
        var value = daily.GetProperty(name)[1];
        return value.ValueKind == JsonValueKind.Null ? null : value.GetDouble();
    }

    private static int? GetIntOrNull(JsonElement daily, string name)
    {
        var value = daily.GetProperty(name)[1];
        return value.ValueKind == JsonValueKind.Null ? null : value.GetInt32();
    }
}

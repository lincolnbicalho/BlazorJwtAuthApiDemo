namespace BlazorJwtAuthApiDemo.Shared.Models;

/// <summary>
/// Weather forecast data model
/// Shared between API and Client projects
/// </summary>
public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}


using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WeatherFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("WeatherFrontend");

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. Home endpoint
app.MapGet("/", () => Results.Ok(new
{
    application = "DemoApi",
    message = "Weather API is running successfully!",
    version = "2.0",
    endpoints = new[]
    {
        "/weatherforecast",
        "/api/weatherforecast?city=Noida&days=5",
        "/health"
    }
}));

// 2. Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    application = "DemoApi",
    timestamp = DateTimeOffset.UtcNow
}));

// 3. Real weather forecast endpoint using Open-Meteo
app.MapGet("/api/weatherforecast",
    async Task<IResult> (
        HttpClient httpClient,
        string? city,
        int days = 5) =>
{
    if (string.IsNullOrWhiteSpace(city))
    {
        return Results.BadRequest(new
        {
            error = "City is required.",
            example = "/api/weatherforecast?city=Noida&days=5"
        });
    }

    if (days < 1 || days > 7)
    {
        return Results.BadRequest(new
        {
            error = "Days must be between 1 and 7."
        });
    }

    try
    {
        // Find the city's coordinates
        var cityName = Uri.EscapeDataString(city.Trim());

        var geoUrl =
            $"https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={cityName}&count=1&language=en&format=json";

        using var geoResponse =
            await httpClient.GetAsync(geoUrl);

        if (!geoResponse.IsSuccessStatusCode)
        {
            return Results.StatusCode(
                StatusCodes.Status502BadGateway);
        }

        using var geoJson =
            await geoResponse.Content.ReadFromJsonAsync<JsonDocument>();

        if (geoJson is null ||
            !geoJson.RootElement.TryGetProperty(
                "results", out var results) ||
            results.GetArrayLength() == 0)
        {
            return Results.NotFound(new
            {
                error = $"City '{city}' was not found."
            });
        }

        var location = results[0];

        var latitude =
            location.GetProperty("latitude").GetDouble();

        var longitude =
            location.GetProperty("longitude").GetDouble();

        var resolvedCity =
            location.GetProperty("name").GetString();

        // Request the actual weather forecast
        var weatherUrl =
            "https://api.open-meteo.com/v1/forecast" +
            $"?latitude={latitude}&longitude={longitude}" +
            "&daily=temperature_2m_max,temperature_2m_min," +
            "precipitation_probability_max,weather_code" +
            $"&timezone=auto&forecast_days={days}";

        using var weatherResponse =
            await httpClient.GetAsync(weatherUrl);

        if (!weatherResponse.IsSuccessStatusCode)
        {
            return Results.StatusCode(
                StatusCodes.Status502BadGateway);
        }

        using var weatherJson =
            await weatherResponse.Content.ReadFromJsonAsync<JsonDocument>();

        if (weatherJson is null ||
            !weatherJson.RootElement.TryGetProperty(
                "daily", out var daily))
        {
            return Results.StatusCode(
                StatusCodes.Status502BadGateway);
        }

        var dates = daily.GetProperty("time");
        var maxTemps = daily.GetProperty("temperature_2m_max");
        var minTemps = daily.GetProperty("temperature_2m_min");
        var rain = daily.GetProperty(
            "precipitation_probability_max");
        var codes = daily.GetProperty("weather_code");

        var forecast = Enumerable.Range(
                0, dates.GetArrayLength())
            .Select(i => new
            {
                date = dates[i].GetString(),

                temperatureMaxC =
                    maxTemps[i].GetDouble(),

                temperatureMinC =
                    minTemps[i].GetDouble(),

                precipitationProbabilityPercent =
                    rain[i].ValueKind == JsonValueKind.Number
                        ? rain[i].GetInt32()
                        : (int?)null,

                weatherCode = codes[i].GetInt32()
            })
            .ToArray();

        return Results.Ok(new
        {
            city = resolvedCity,
            latitude,
            longitude,
            days,
            source = "Open-Meteo",
            forecast
        });
    }
    catch (HttpRequestException)
    {
        return Results.StatusCode(
            StatusCodes.Status502BadGateway);
    }
    catch (JsonException)
    {
        return Results.StatusCode(
            StatusCodes.Status502BadGateway);
    }
})
.WithName("GetCityWeatherForecast")
.WithOpenApi();

// 4. Original simulated weather forecast endpoint
var originalSummaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    return Enumerable.Range(1, 5)
        .Select(index => new WeatherForecast(
            DateOnly.FromDateTime(
                DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            originalSummaries[
                Random.Shared.Next(originalSummaries.Length)]
        ))
        .ToArray();
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Start application
app.Run();

// Weather forecast model for the original endpoint
record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}

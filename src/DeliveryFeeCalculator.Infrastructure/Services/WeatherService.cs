using System.Xml.Linq;
using DeliveryFeeCalculator.Core.Constants;
using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using DeliveryFeeCalculator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeliveryFeeCalculator.Infrastructure.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _dbContext;
        private readonly ILogger<WeatherService> _logger;
        private readonly HttpClient _httpClient;
        private const string WeatherDataUrl = "https://www.ilmateenistus.ee/ilma_andmed/xml/observations.php";

        public WeatherService(WeatherDbContext dbContext, ILogger<WeatherService> logger, HttpClient httpClient)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<WeatherData?> GetLatestWeatherDataForCityAsync(City city)
        {
            if (!WeatherStations.CityToStation.Map.TryGetValue(city, out string stationName))
            {
                throw new ArgumentException($"Invalid city: {city}");
            }
            
            _logger.LogInformation("Getting latest weather data for city {City}, station {Station}", city, stationName);

            return await _dbContext.WeatherData
                .Where(w => w.StationName == stationName)
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task ImportWeatherDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting weather data import from {Url}", WeatherDataUrl);

                var response = await _httpClient.GetStringAsync(WeatherDataUrl);
                var xmlDoc = XDocument.Parse(response);

                _logger.LogInformation("Successfully retrieved XML data, parsing stations...");
                
                // Log available station names to debug
                var allStationNames = xmlDoc.Descendants("station").Select(s => s.Element("name")?.Value).ToList();
                _logger.LogInformation("Available stations: {Stations}", string.Join(", ", allStationNames));
                
                var stations = xmlDoc.Descendants("station")
                    .Where(s => 
                        s.Element("name")?.Value == WeatherStations.TallinnHarku || 
                        s.Element("name")?.Value == WeatherStations.TartuToravere || 
                        s.Element("name")?.Value == WeatherStations.Parnu)
                    .ToList();
                
                _logger.LogInformation("Found {Count} matching stations", stations.Count);

                // The timestamp is provided as a Unix timestamp
                var observationTimeStr = xmlDoc.Root?.Attribute("timestamp")?.Value;
                _logger.LogInformation("Raw timestamp value: {Timestamp}", observationTimeStr);
                
                DateTime timestamp;
                
                if (!string.IsNullOrEmpty(observationTimeStr) && long.TryParse(observationTimeStr, out long unixTime))
                {
                    // Convert Unix timestamp to DateTime
                    // Note: Unix timestamp is usually in seconds since Jan 1, 1970
                    // If it's in milliseconds, divide by 1000
                    try 
                    {
                        // Try as seconds
                        timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                        _logger.LogInformation("Parsed timestamp (as seconds): {Timestamp}", timestamp);
                    }
                    catch 
                    {
                        // If that fails, try as milliseconds
                        timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).UtcDateTime;
                        _logger.LogInformation("Parsed timestamp (as milliseconds): {Timestamp}", timestamp);
                    }
                }
                else
                {
                    // Fallback to current time if parsing fails
                    timestamp = DateTime.UtcNow;
                    _logger.LogWarning("Could not parse timestamp, using current time: {Timestamp}", timestamp);
                }
                
                List<WeatherData> weatherDataToAdd = new();

                foreach (var station in stations)
                {
                    var stationName = station.Element("name")?.Value;
                    var wmoCode = station.Element("wmocode")?.Value;
                    
                    if (string.IsNullOrEmpty(stationName) || string.IsNullOrEmpty(wmoCode))
                    {
                        _logger.LogWarning("Skipping station with missing name or WMO code");
                        continue;
                    }
                    
                    _logger.LogInformation("Processing station: {StationName}, WMO: {WmoCode}", stationName, wmoCode);

                    var airTemperatureElement = station.Element("airtemperature");
                    var windSpeedElement = station.Element("windspeed");
                    var phenomenonElement = station.Element("phenomenon");
                    
                    _logger.LogInformation("Raw values - Temperature: {Temp}, Wind: {Wind}, Phenomenon: {Phenomenon}", 
                        airTemperatureElement?.Value, 
                        windSpeedElement?.Value, 
                        phenomenonElement?.Value);

                    decimal airTemperature = 0;
                    var airTempStr = airTemperatureElement?.Value?.Replace(',', '.');
                    if (!string.IsNullOrEmpty(airTempStr) && decimal.TryParse(airTempStr, 
                        System.Globalization.NumberStyles.Any, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        out var tempValue))
                    {
                        airTemperature = tempValue;
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse air temperature: {Value}", airTemperatureElement?.Value);
                    }

                    decimal windSpeed = 0;
                    var windSpeedStr = windSpeedElement?.Value?.Replace(',', '.');
                    if (!string.IsNullOrEmpty(windSpeedStr) && decimal.TryParse(windSpeedStr, 
                        System.Globalization.NumberStyles.Any, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        out var windValue))
                    {
                        windSpeed = windValue;
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse wind speed: {Value}", windSpeedElement?.Value);
                    }

                    var phenomenon = phenomenonElement?.Value ?? string.Empty;

                    var weatherData = new WeatherData
                    {
                        StationName = stationName,
                        WmoCode = wmoCode,
                        AirTemperature = airTemperature,
                        WindSpeed = windSpeed,
                        WeatherPhenomenon = phenomenon,
                        Timestamp = timestamp
                    };

                    weatherDataToAdd.Add(weatherData);
                }

                await _dbContext.WeatherData.AddRangeAsync(weatherDataToAdd);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully imported {Count} weather observations", weatherDataToAdd.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing weather data");
                throw;
            }
        }
    }
}
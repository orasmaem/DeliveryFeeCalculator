using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using DeliveryFeeCalculator.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace DeliveryFeeCalculator.Infrastructure.Services
{
    /// <summary>
    /// Service for calculating delivery fees based on city, vehicle type, and weather conditions
    /// </summary>
    public class DeliveryFeeCalculationService : IDeliveryFeeCalculationService
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<DeliveryFeeCalculationService> _logger;

        public DeliveryFeeCalculationService(IWeatherService weatherService, ILogger<DeliveryFeeCalculationService> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DeliveryFeeResponse> CalculateDeliveryFeeAsync(DeliveryFeeRequest request, bool useTestData = false)
        {
            try
            {
                // Get the base fee for the city and vehicle type
                decimal regionalBaseFee = RegionalBaseFeeData.GetBaseFee(request.City, request.VehicleType);
                
                // Get the latest weather data for the city
                var weatherData = await _weatherService.GetLatestWeatherDataForCityAsync(request.City, useTestData);
                
                if (weatherData == null)
                {
                    _logger.LogWarning("No weather data available for {City}", request.City);
                    return new DeliveryFeeResponse 
                    { 
                        Fee = regionalBaseFee,
                        RegionalBaseFee = regionalBaseFee,
                        ErrorMessage = "Warning: Using base fee only as no weather data is available."
                    };
                }

                // Calculate extra fees based on weather conditions
                decimal extraFeeAirTemperature = CalculateAirTemperatureExtraFee(weatherData.AirTemperature, request.VehicleType);
                decimal extraFeeWindSpeed = CalculateWindSpeedExtraFee(weatherData.WindSpeed, request.VehicleType);
                decimal extraFeeWeatherPhenomenon = CalculateWeatherPhenomenonExtraFee(weatherData.WeatherPhenomenon, request.VehicleType);

                // Check for usage prohibitions
                if (IsVehicleUsageForbidden(weatherData, request.VehicleType, out string reason))
                {
                    return new DeliveryFeeResponse
                    {
                        ErrorMessage = $"Usage of selected vehicle type is forbidden: {reason}"
                    };
                }

                // Calculate total fee
                decimal totalFee = regionalBaseFee + extraFeeAirTemperature + extraFeeWindSpeed + extraFeeWeatherPhenomenon;

                // Analyze weather conditions for UI feedback
                var weatherDetails = new WeatherDetails
                {
                    StationName = weatherData.StationName,
                    Temperature = weatherData.AirTemperature,
                    WindSpeed = weatherData.WindSpeed,
                    Phenomenon = weatherData.WeatherPhenomenon,
                    Timestamp = weatherData.Timestamp,
                    
                    // Flag conditions based on thresholds
                    IsLowTemperature = weatherData.AirTemperature < 0 && weatherData.AirTemperature >= -10,
                    IsVeryLowTemperature = weatherData.AirTemperature < -10,
                    IsHighWindSpeed = weatherData.WindSpeed >= 10 && weatherData.WindSpeed <= 20,
                    IsExtremeWindSpeed = weatherData.WindSpeed > 20,
                    HasSnowOrSleet = weatherData.WeatherPhenomenon.ToLower().Contains("snow") || weatherData.WeatherPhenomenon.ToLower().Contains("sleet"),
                    HasRain = weatherData.WeatherPhenomenon.ToLower().Contains("rain"),
                    HasDangerousPhenomenon = weatherData.WeatherPhenomenon.ToLower().Contains("glaze") || 
                                           weatherData.WeatherPhenomenon.ToLower().Contains("hail") || 
                                           weatherData.WeatherPhenomenon.ToLower().Contains("thunder")
                };

                return new DeliveryFeeResponse
                {
                    Fee = totalFee,
                    RegionalBaseFee = regionalBaseFee,
                    ExtraFeeTemperature = extraFeeAirTemperature,
                    ExtraFeeWindSpeed = extraFeeWindSpeed,
                    ExtraFeeWeatherPhenomenon = extraFeeWeatherPhenomenon,
                    WeatherDetails = weatherDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery fee for {City} and {VehicleType}", 
                    request.City, request.VehicleType);
                throw;
            }
        }

        private decimal CalculateAirTemperatureExtraFee(decimal temperature, VehicleType vehicleType)
        {
            _logger.LogInformation("Calculating temperature extra fee for {Temperature}°C and {VehicleType}", temperature, vehicleType);
            
            // Only bikes and scooters get extra fee for low temperature
            if (vehicleType == VehicleType.Car)
                return 0;

            if (temperature < -10)
            {
                _logger.LogInformation("Applied temperature rule: Very Cold (below -10°C), ExtraFee=1.0€");
                return 1.0m;
            }
            
            if (temperature < 0)
            {
                _logger.LogInformation("Applied temperature rule: Cold (0°C to -10°C), ExtraFee=0.5€");
                return 0.5m;
            }

            _logger.LogInformation("No temperature rules applied");
            return 0;
        }

        private decimal CalculateWindSpeedExtraFee(decimal windSpeed, VehicleType vehicleType)
        {
            // Only bikes get extra fee for high wind speed
            if (vehicleType != VehicleType.Bike)
                return 0;

            if (windSpeed >= 10 && windSpeed <= 20)
                return 0.5m;

            return 0;
        }

        private decimal CalculateWeatherPhenomenonExtraFee(string phenomenon, VehicleType vehicleType)
        {
            // Only bikes and scooters get extra fee for weather phenomena
            if (vehicleType == VehicleType.Car)
                return 0;

            // Convert to lower case for case-insensitive comparison
            string phenomenonLower = phenomenon.ToLower();

            // Check for snow or sleet related phenomena
            if (phenomenonLower.Contains("snow") || phenomenonLower.Contains("sleet"))
                return 1.0m;

            // Check for rain related phenomena
            if (phenomenonLower.Contains("rain"))
                return 0.5m;

            return 0;
        }

        private bool IsVehicleUsageForbidden(WeatherData weatherData, VehicleType vehicleType, out string reason)
        {
            reason = string.Empty;

            // Only apply restrictions to bikes and scooters
            if (vehicleType == VehicleType.Car)
                return false;

            // Check wind speed for bikes
            if (vehicleType == VehicleType.Bike && weatherData.WindSpeed > 20)
            {
                reason = "Wind speed is too high";
                return true;
            }

            // Check for forbidden weather phenomena for bikes and scooters
            string phenomenonLower = weatherData.WeatherPhenomenon.ToLower();
            if (phenomenonLower.Contains("glaze") || 
                phenomenonLower.Contains("hail") || 
                phenomenonLower.Contains("thunder"))
            {
                reason = $"Dangerous weather phenomenon: {weatherData.WeatherPhenomenon}";
                return true;
            }

            return false;
        }
    }
}
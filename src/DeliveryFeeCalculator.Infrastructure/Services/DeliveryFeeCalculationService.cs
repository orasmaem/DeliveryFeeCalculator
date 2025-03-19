using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using DeliveryFeeCalculator.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace DeliveryFeeCalculator.Infrastructure.Services
{
    public class DeliveryFeeCalculationService : IDeliveryFeeCalculationService
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<DeliveryFeeCalculationService> _logger;

        public DeliveryFeeCalculationService(IWeatherService weatherService, ILogger<DeliveryFeeCalculationService> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<DeliveryFeeResponse> CalculateDeliveryFeeAsync(DeliveryFeeRequest request)
        {
            try
            {
                // Get the base fee for the city and vehicle type
                decimal regionalBaseFee = RegionalBaseFeeData.GetBaseFee(request.City, request.VehicleType);
                
                // Get the latest weather data for the city
                var weatherData = await _weatherService.GetLatestWeatherDataForCityAsync(request.City);
                
                if (weatherData == null)
                {
                    _logger.LogWarning("No weather data available for {City}", request.City);
                    return new DeliveryFeeResponse 
                    { 
                        Fee = regionalBaseFee,
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

                return new DeliveryFeeResponse
                {
                    Fee = totalFee
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
            // Only bikes and scooters get extra fee for low temperature
            if (vehicleType == VehicleType.Car)
                return 0;

            if (temperature < -10)
                return 1.0m;
            if (temperature < 0)
                return 0.5m;

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
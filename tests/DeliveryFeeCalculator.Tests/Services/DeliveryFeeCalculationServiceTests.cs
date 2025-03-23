using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Core.Models;
using DeliveryFeeCalculator.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeliveryFeeCalculator.Tests.Services
{
    public class DeliveryFeeCalculationServiceTests
    {
        private readonly Mock<IWeatherService> _weatherServiceMock;
        private readonly Mock<ILogger<DeliveryFeeCalculationService>> _loggerMock;
        private readonly DeliveryFeeCalculationService _service;

        public DeliveryFeeCalculationServiceTests()
        {
            _weatherServiceMock = new Mock<IWeatherService>();
            _loggerMock = new Mock<ILogger<DeliveryFeeCalculationService>>();
            _service = new DeliveryFeeCalculationService(_weatherServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_NoWeatherData_ReturnsBaseFeeOnly()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Car };
            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync((WeatherData?)null);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4.0m, result.Fee); // Base fee for Tallinn car
            Assert.Equal(4.0m, result.RegionalBaseFee);
            Assert.Equal(0.0m, result.ExtraFeeTemperature);
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(0.0m, result.ExtraFeeWeatherPhenomenon);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Warning", result.ErrorMessage);
            Assert.Null(result.WeatherDetails);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_LowTemperature_AppliesExtraFeeForBike()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Bike };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = -5.0m,
                WindSpeed = 5.0m,
                WeatherPhenomenon = "Clear",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3.0m, result.RegionalBaseFee); // Base fee for Tallinn bike
            Assert.Equal(0.5m, result.ExtraFeeTemperature); // Extra fee for temperature
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(0.0m, result.ExtraFeeWeatherPhenomenon);
            Assert.Equal(3.5m, result.Fee); // Total fee
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.WeatherDetails);
            Assert.True(result.WeatherDetails.IsLowTemperature);
            Assert.False(result.WeatherDetails.IsVeryLowTemperature);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_VeryLowTemperature_AppliesHigherExtraFeeForBike()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Bike };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = -15.0m,
                WindSpeed = 5.0m,
                WeatherPhenomenon = "Clear",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3.0m, result.RegionalBaseFee); // Base fee for Tallinn bike
            Assert.Equal(1.0m, result.ExtraFeeTemperature); // Higher extra fee for very low temperature
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(0.0m, result.ExtraFeeWeatherPhenomenon);
            Assert.Equal(4.0m, result.Fee); // Total fee
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.WeatherDetails);
            Assert.False(result.WeatherDetails.IsLowTemperature);
            Assert.True(result.WeatherDetails.IsVeryLowTemperature);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_HighWindSpeed_AppliesExtraFeeForBike()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Bike };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = 5.0m,
                WindSpeed = 15.0m,
                WeatherPhenomenon = "Clear",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3.0m, result.RegionalBaseFee); // Base fee for Tallinn bike
            Assert.Equal(0.0m, result.ExtraFeeTemperature);
            Assert.Equal(0.5m, result.ExtraFeeWindSpeed); // Extra fee for high wind speed
            Assert.Equal(0.0m, result.ExtraFeeWeatherPhenomenon);
            Assert.Equal(3.5m, result.Fee); // Total fee
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.WeatherDetails);
            Assert.True(result.WeatherDetails.IsHighWindSpeed);
            Assert.False(result.WeatherDetails.IsExtremeWindSpeed);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_ExtremeWindSpeed_ForbidsBikeUsage()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Bike };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = 5.0m,
                WindSpeed = 25.0m,
                WeatherPhenomenon = "Clear",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.0m, result.Fee); // No fee when forbidden
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("forbidden", result.ErrorMessage);
            Assert.Contains("Wind speed is too high", result.ErrorMessage);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_SnowWeather_AppliesExtraFeeForBikeAndScooter()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Scooter };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = 1.0m,
                WindSpeed = 5.0m,
                WeatherPhenomenon = "Light snow shower",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3.5m, result.RegionalBaseFee); // Base fee for Tallinn scooter
            Assert.Equal(0.0m, result.ExtraFeeTemperature);
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(1.0m, result.ExtraFeeWeatherPhenomenon); // Extra fee for snow
            Assert.Equal(4.5m, result.Fee); // Total fee
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.WeatherDetails);
            Assert.True(result.WeatherDetails.HasSnowOrSleet);
            Assert.False(result.WeatherDetails.HasRain);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_RainWeather_AppliesExtraFeeForBikeAndScooter()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Scooter };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = 10.0m,
                WindSpeed = 5.0m,
                WeatherPhenomenon = "Light rain",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3.5m, result.RegionalBaseFee); // Base fee for Tallinn scooter
            Assert.Equal(0.0m, result.ExtraFeeTemperature);
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(0.5m, result.ExtraFeeWeatherPhenomenon); // Extra fee for rain
            Assert.Equal(4.0m, result.Fee); // Total fee
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.WeatherDetails);
            Assert.False(result.WeatherDetails.HasSnowOrSleet);
            Assert.True(result.WeatherDetails.HasRain);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_DangerousWeatherPhenomenon_ForbidsBikeAndScooterUsage()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Scooter };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = 5.0m,
                WindSpeed = 5.0m,
                WeatherPhenomenon = "Thunder",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.0m, result.Fee); // No fee when forbidden
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("forbidden", result.ErrorMessage);
            Assert.Contains("Thunder", result.ErrorMessage);
        }

        [Fact]
        public async Task CalculateDeliveryFeeAsync_CarUnaffectedByWeather()
        {
            // Arrange
            var request = new DeliveryFeeRequest { City = City.Tallinn, VehicleType = VehicleType.Car };
            var weatherData = new WeatherData
            {
                StationName = "Tallinn-Harku",
                AirTemperature = -20.0m,
                WindSpeed = 30.0m,
                WeatherPhenomenon = "Thunder with heavy hail and snow",
                Timestamp = DateTime.Now
            };

            _weatherServiceMock.Setup(x => x.GetLatestWeatherDataForCityAsync(City.Tallinn, false))
                .ReturnsAsync(weatherData);

            // Act
            var result = await _service.CalculateDeliveryFeeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4.0m, result.RegionalBaseFee); // Base fee for Tallinn car
            Assert.Equal(0.0m, result.ExtraFeeTemperature); // No extra fees for car
            Assert.Equal(0.0m, result.ExtraFeeWindSpeed);
            Assert.Equal(0.0m, result.ExtraFeeWeatherPhenomenon);
            Assert.Equal(4.0m, result.Fee); // Only base fee for car
            Assert.Null(result.ErrorMessage); // No restrictions for car
        }
    }
}
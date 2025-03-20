using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Models;

namespace DeliveryFeeCalculator.Core.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherData?> GetLatestWeatherDataForCityAsync(City city, bool useTestData = false);
        Task ImportWeatherDataAsync();
    }
}
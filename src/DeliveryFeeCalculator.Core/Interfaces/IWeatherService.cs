using DeliveryFeeCalculator.Core.Enums;
using DeliveryFeeCalculator.Core.Models;

namespace DeliveryFeeCalculator.Core.Interfaces
{
    /// <summary>
    /// Service for retrieving and importing weather data from external sources
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Gets the latest weather data for a specified city
        /// </summary>
        /// <param name="city">The city to get weather data for</param>
        /// <param name="useTestData">If true, returns test data with extreme weather conditions</param>
        /// <returns>The latest weather data for the city, or null if no data is available</returns>
        Task<WeatherData?> GetLatestWeatherDataForCityAsync(City city, bool useTestData = false);
        
        /// <summary>
        /// Imports weather data from the Estonian Environment Agency XML feed
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ImportWeatherDataAsync();
    }
}
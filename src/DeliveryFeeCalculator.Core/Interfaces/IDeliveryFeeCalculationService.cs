using DeliveryFeeCalculator.Core.Models;

namespace DeliveryFeeCalculator.Core.Interfaces
{
    /// <summary>
    /// Service for calculating delivery fees based on city, vehicle type, and weather conditions
    /// </summary>
    public interface IDeliveryFeeCalculationService
    {
        /// <summary>
        /// Calculates the delivery fee based on the provided request and current weather conditions
        /// </summary>
        /// <param name="request">The delivery fee request containing city and vehicle type</param>
        /// <param name="useTestData">If true, uses test weather data with extreme conditions</param>
        /// <returns>
        /// A response containing the calculated fee, fee breakdown, and weather details,
        /// or an error message if calculation failed or vehicle usage is prohibited
        /// </returns>
        Task<DeliveryFeeResponse> CalculateDeliveryFeeAsync(DeliveryFeeRequest request, bool useTestData = false);
    }
}
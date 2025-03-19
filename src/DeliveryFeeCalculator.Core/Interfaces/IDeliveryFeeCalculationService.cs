using DeliveryFeeCalculator.Core.Models;

namespace DeliveryFeeCalculator.Core.Interfaces
{
    public interface IDeliveryFeeCalculationService
    {
        Task<DeliveryFeeResponse> CalculateDeliveryFeeAsync(DeliveryFeeRequest request);
    }
}
using DeliveryFeeCalculator.Core.Enums;

namespace DeliveryFeeCalculator.Core.Models
{
    public class DeliveryFeeRequest
    {
        public City City { get; set; }
        public VehicleType VehicleType { get; set; }
    }
}
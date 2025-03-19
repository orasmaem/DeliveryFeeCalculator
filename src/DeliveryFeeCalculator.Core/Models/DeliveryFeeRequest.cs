using DeliveryFeeCalculator.Core.Enums;

namespace DeliveryFeeCalculator.Core.Models
{
    public class DeliveryFeeRequest
    {
        // Default parameterless constructor required for model binding
        public DeliveryFeeRequest()
        {
        }
        
        public DeliveryFeeRequest(City city, VehicleType vehicleType)
        {
            City = city;
            VehicleType = vehicleType;
        }

        public City City { get; set; }
        public VehicleType VehicleType { get; set; }
    }
}